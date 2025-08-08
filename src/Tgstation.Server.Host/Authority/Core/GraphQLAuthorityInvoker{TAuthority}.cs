using System;
using System.Linq;
using System.Threading.Tasks;

using HotChocolate;

using Microsoft.AspNetCore.Authorization;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.GraphQL;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <inheritdoc cref="IGraphQLAuthorityInvoker{TAuthority}" />
	sealed class GraphQLAuthorityInvoker<TAuthority> : AuthorityInvokerBase<TAuthority>, IGraphQLAuthorityInvoker<TAuthority>
		where TAuthority : IAuthority
	{
		/// <summary>
		/// Throws a <see cref="ErrorMessageException"/> for errored <paramref name="authorityResponse"/>s.
		/// </summary>
		/// <typeparam name="TAuthorityResponse">The <see cref="AuthorityResponse"/> <see cref="Type"/> being checked.</typeparam>
		/// <param name="authorityResponse">The potentially errored <paramref name="authorityResponse"/> or <see langword="null"/> if requirements evaluation failed.</param>
		/// <param name="errorOnMissing">If an error should be raised for <see cref="HttpFailureResponse.NotFound"/> and <see cref="HttpFailureResponse.Gone"/> failures.</param>
		/// <returns><paramref name="authorityResponse"/> if an <see cref="ErrorMessageException"/> wasn't thrown.</returns>
		static TAuthorityResponse ThrowGraphQLErrorIfNecessary<TAuthorityResponse>(TAuthorityResponse authorityResponse, bool errorOnMissing)
			where TAuthorityResponse : AuthorityResponse
		{
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
		/// <param name="authorizationService">the authorization service to use.</param>
		public GraphQLAuthorityInvoker(TAuthority authority, Security.IAuthorizationService authorizationService)
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
			ThrowGraphQLErrorIfNecessary(authorityResponse, false);

			return authorityResponse.Result;
		}

		/// <inheritdoc />
		async ValueTask<TApiModel?> IGraphQLAuthorityInvoker<TAuthority>.InvokeTransformableAllowMissing<TResult, TApiModel, TTransformer>(Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
			where TApiModel : default
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var requirementsGate = authorityInvoker(Authority);
			var authorityResponse = await ExecuteIfRequirementsSatisfied(requirementsGate);
			ThrowGraphQLErrorIfNecessary(authorityResponse, false);
			var result = authorityResponse.Result;
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
			var queryable = await ExecuteIfRequirementsSatisfied(requirementsGate);

			if (preTransformer != null)
				queryable = preTransformer(queryable);

			if (typeof(EntityId).IsAssignableFrom(typeof(TResult)))
				queryable = queryable.OrderBy(item => ((EntityId)(object)item).Id!.Value); // order by ID to fix an EFCore warning

			var expression = new TTransformer().Expression;
			return queryable
				.Select(expression);
		}

		/// <inheritdoc />
		async ValueTask<TApiModel> IGraphQLAuthorityInvoker<TAuthority>.Invoke<TResult, TApiModel>(Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
			=> await ((IGraphQLAuthorityInvoker<TAuthority>)this).InvokeAllowMissing<TResult, TApiModel>(authorityInvoker)
				?? throw new InvalidOperationException("Authority invocation should have returned a non-nullable result!");

		/// <inheritdoc />
		async ValueTask<TApiModel> IGraphQLAuthorityInvoker<TAuthority>.InvokeTransformable<TResult, TApiModel, TTransformer>(Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
			=> await ((IGraphQLAuthorityInvoker<TAuthority>)this).InvokeTransformableAllowMissing<TResult, TApiModel, TTransformer>(authorityInvoker)
				?? throw new InvalidOperationException("Authority invocation should have returned a non-nullable result!");

		/// <inheritdoc />
		protected override void OnRequirementsFailure(AuthorizationFailure authFailure)
			=> throw authFailure.ForbiddenGraphQLException();

		/// <summary>
		/// Unwrap a <see cref="RequirementsGated{TResult}"/> result, throwing a <see cref="GraphQLException"/> if they weren't met.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> contained by the <paramref name="requirementsGate"/>.</typeparam>
		/// <param name="requirementsGate">The <see cref="RequirementsGated{TResult}"/> result.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TResult"/> if the requirements were met.</returns>
		/// <exception cref="GraphQLException">Throw when requirements were not met.</exception>
		new async ValueTask<TResult> ExecuteIfRequirementsSatisfied<TResult>(RequirementsGated<TResult> requirementsGate)
			where TResult : class
			=> (await base.ExecuteIfRequirementsSatisfied(requirementsGate))!; // base class throws if requirements evaluation fails
	}
}
