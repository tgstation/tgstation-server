using System;
using System.Linq;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.GraphQL;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <inheritdoc cref="IGraphQLAuthorityInvoker{TAuthority}" />
	sealed class GraphQLAuthorityInvoker<TAuthority> : AuthorityInvokerBase<TAuthority>, IGraphQLAuthorityInvoker<TAuthority>
		where TAuthority : IAuthority
	{
		/// <summary>
		/// Create a new <see cref="ErrorMessageException"/> to be thrown when a forbidden error occurs.
		/// </summary>
		/// <returns>A new <see cref="ErrorMessageException"/>.</returns>
		static ErrorMessageException ForbiddenGraphQLError()
			=> new(new ErrorMessageResponse(), HttpFailureResponse.Forbidden.ToString());

		/// <summary>
		/// Throws a <see cref="ErrorMessageException"/> for errored <paramref name="authorityResponse"/>s.
		/// </summary>
		/// <typeparam name="TAuthorityResponse">The <see cref="AuthorityResponse"/> <see cref="Type"/> being checked.</typeparam>
		/// <param name="authorityResponse">The potentially errored <paramref name="authorityResponse"/> or <see langword="null"/> if requirements evaluation failed.</param>
		/// <param name="errorOnMissing">If an error should be raised for <see cref="HttpFailureResponse.NotFound"/> and <see cref="HttpFailureResponse.Gone"/> failures.</param>
		/// <returns><paramref name="authorityResponse"/> if an <see cref="ErrorMessageException"/> wasn't thrown.</returns>
		static TAuthorityResponse ThrowGraphQLErrorIfNecessary<TAuthorityResponse>(TAuthorityResponse? authorityResponse, bool errorOnMissing)
			where TAuthorityResponse : AuthorityResponse
		{
			if (authorityResponse == null)
				throw ForbiddenGraphQLError();

			if (authorityResponse.Success
				|| ((authorityResponse.FailureResponse.Value == HttpFailureResponse.NotFound
				|| authorityResponse.FailureResponse.Value == HttpFailureResponse.Gone) && !errorOnMissing))
				return authorityResponse;

			var fallbackString = authorityResponse.FailureResponse.ToString()!;
			throw new ErrorMessageException(authorityResponse.ErrorMessage, fallbackString);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GraphQLAuthorityInvoker{TAuthority}"/> class.
		/// </summary>
		/// <param name="authority">The <typeparamref name="TAuthority"/>.</param>
		/// <param name="authorizationService">the <see cref="IAuthorizationService"/> to use.</param>
		public GraphQLAuthorityInvoker(TAuthority authority, IAuthorizationService authorizationService)
			: base(authority, authorizationService)
		{
		}

		/// <inheritdoc />
		async ValueTask IGraphQLAuthorityInvoker<TAuthority>.Invoke(Func<TAuthority, RequirementsGated<AuthorityResponse>> authorityInvoker)
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var requirementsGate = authorityInvoker(Authority);
			var authorityResponse = await ExecuteIfRequirementsSatisfied(requirementsGate);
			ThrowGraphQLErrorIfNecessary(authorityResponse, true);
		}

		/// <inheritdoc />
		async ValueTask<TApiModel?> IGraphQLAuthorityInvoker<TAuthority>.InvokeAllowMissing<TResult, TApiModel>(Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
			where TApiModel : default
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var requirementsGate = authorityInvoker(Authority);
			var authorityResponse = await ExecuteIfRequirementsSatisfied(requirementsGate);
			return ThrowGraphQLErrorIfNecessary(authorityResponse, false).Result;
		}

		/// <inheritdoc />
		async ValueTask<TApiModel?> IGraphQLAuthorityInvoker<TAuthority>.InvokeTransformableAllowMissing<TResult, TApiModel, TTransformer>(Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
			where TApiModel : default
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var requirementsGate = authorityInvoker(Authority);
			var authorityResponse = await ExecuteIfRequirementsSatisfied(requirementsGate);
			var result = ThrowGraphQLErrorIfNecessary(authorityResponse, false).Result;
			if (result == null)
				return default;

			return result.ToApi();
		}

		/// <inheritdoc />
		async ValueTask<IQueryable<TApiModel>> IGraphQLAuthorityInvoker<TAuthority>.InvokeTransformableQueryable<TResult, TApiModel, TTransformer>(
			Func<TAuthority, RequirementsGated<IQueryable<TResult>>> authorityInvoker,
			Func<IQueryable<TResult>, IQueryable<TResult>>? preTransformer)
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var requirementsGate = authorityInvoker(Authority);
			var queryable = await ExecuteIfRequirementsSatisfied(requirementsGate)
				?? throw ForbiddenGraphQLError();

			if (preTransformer != null)
				queryable = preTransformer(queryable);

			if (typeof(EntityId).IsAssignableFrom(typeof(TResult)))
				queryable = queryable.OrderBy(item => ((EntityId)(object)item).Id!.Value); // order by ID to fix an EFCore warning

			var expression = new TTransformer().Expression;
			return queryable
				.Select(expression);
		}

		/// <inheritdoc />
		ValueTask<TApiModel> IGraphQLAuthorityInvoker<TAuthority>.Invoke<TResult, TApiModel>(Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
			=> ((IGraphQLAuthorityInvoker<TAuthority>)this).InvokeAllowMissing<TResult, TApiModel>(authorityInvoker)!;

		/// <inheritdoc />
		ValueTask<TApiModel> IGraphQLAuthorityInvoker<TAuthority>.InvokeTransformable<TResult, TApiModel, TTransformer>(Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
			=> ((IGraphQLAuthorityInvoker<TAuthority>)this).InvokeTransformableAllowMissing<TResult, TApiModel, TTransformer>(authorityInvoker)!;
	}
}
