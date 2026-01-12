using System;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut;
using GreenDonut.Data;

using Tgstation.Server.Host.Authority.Core;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="IDataLoader{TKey, TValue}"/>.
	/// </summary>
	static class DataLoaderExtensions
	{
		/// <summary>
		/// Convert a request for a single <typeparamref name="TResult"/> into a data-loader invocation for the matching <see cref="AuthorityResponse{TResult}"/>.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> of the underlying DTO being loaded.</typeparam>
		/// <param name="dataLoader">The <see cref="IDataLoader{TKey, TValue}"/> for <typeparamref name="TResult"/> <see cref="AuthorityResponse{TResult}"/>s.</param>
		/// <param name="queryContext">The active <see cref="QueryContext{TEntity}"/>, if any.</param>
		/// <param name="id">The <see cref="Api.Models.EntityId.Id"/> of <typeparamref name="TResult"/> to load.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the loaded <typeparamref name="TResult"/> if it exists, <see langword="null"/> otherwise.</returns>
		public static async ValueTask<TResult?> LoadAuthorityResponse<TResult>(
			this IDataLoader<long, AuthorityResponse<TResult>> dataLoader,
			QueryContext<TResult>? queryContext,
			long id,
			CancellationToken cancellationToken)
			where TResult : class
		{
			ArgumentNullException.ThrowIfNull(dataLoader);

			var wrappedQueryContext = queryContext?.AuthorityResponseWrap();

			var branchedDataLoader = dataLoader
				.With(wrappedQueryContext);

			var authorityResponse = await branchedDataLoader
				.LoadAsync(id, cancellationToken);

			if (authorityResponse == null)
				return null;

			GraphQLAuthorityInvoker<IAuthority>.ThrowGraphQLErrorIfNecessary(authorityResponse, false);
			return authorityResponse.Result;
		}
	}
}
