using System;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut;
using GreenDonut.Data;

using Tgstation.Server.Host.Authority.Core;

namespace Tgstation.Server.Host.Extensions
{
	static class DataLoaderExtensions
	{
		public static async ValueTask<TResult?> LoadAuthorityResponse<TResult, TID>(
			this IDataLoader<TID, AuthorityResponse<TResult>> dataLoader,
			QueryContext<TResult>? queryContext,
			TID id,
			CancellationToken cancellationToken)
			where TResult : class
			where TID : notnull
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
