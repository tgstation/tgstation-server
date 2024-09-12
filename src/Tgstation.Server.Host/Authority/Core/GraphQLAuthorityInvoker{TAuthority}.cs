using System;
using System.Threading.Tasks;

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
		/// <param name="authorityResponse">The potentially errored <paramref name="authorityResponse"/>.</param>
		static void ThrowGraphQLErrorIfNecessary(AuthorityResponse authorityResponse)
		{
			if (authorityResponse.Success
				|| authorityResponse.FailureResponse.Value == HttpFailureResponse.NotFound
				|| authorityResponse.FailureResponse.Value == HttpFailureResponse.Gone)
				return;

			var fallbackString = authorityResponse.FailureResponse.ToString()!;
			throw new ErrorMessageException(authorityResponse.ErrorMessage, fallbackString);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GraphQLAuthorityInvoker{TAuthority}"/> class.
		/// </summary>
		/// <param name="authority">The <typeparamref name="TAuthority"/>.</param>
		public GraphQLAuthorityInvoker(TAuthority authority)
			: base(authority)
		{
		}

		/// <inheritdoc />
		async ValueTask IGraphQLAuthorityInvoker<TAuthority>.Invoke(Func<TAuthority, ValueTask<AuthorityResponse>> authorityInvoker)
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var authorityResponse = await authorityInvoker(Authority);
			ThrowGraphQLErrorIfNecessary(authorityResponse);
		}

		/// <inheritdoc />
		async ValueTask<TApiModel?> IGraphQLAuthorityInvoker<TAuthority>.Invoke<TResult, TApiModel>(Func<TAuthority, ValueTask<AuthorityResponse<TResult>>> authorityInvoker)
			where TApiModel : default
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var authorityResponse = await authorityInvoker(Authority);
			ThrowGraphQLErrorIfNecessary(authorityResponse);
			return authorityResponse.Result;
		}

		/// <inheritdoc />
		async ValueTask<TApiModel?> IGraphQLAuthorityInvoker<TAuthority>.InvokeTransformable<TResult, TApiModel, TTransformer>(Func<TAuthority, ValueTask<AuthorityResponse<TResult>>> authorityInvoker)
			where TApiModel : default
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var authorityResponse = await authorityInvoker(Authority);
			ThrowGraphQLErrorIfNecessary(authorityResponse);
			var result = authorityResponse.Result;
			if (result == null)
				return default;

			return result.ToApi();
		}
	}
}
