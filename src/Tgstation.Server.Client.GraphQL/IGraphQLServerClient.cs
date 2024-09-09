using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Client.GraphQL
{
	/// <summary>
	/// Wrapper for using a TGS <see cref="IGraphQLClient"/>.
	/// </summary>
	public interface IGraphQLServerClient : IAsyncDisposable
	{
		/// <summary>
		/// Runs a given <paramref name="queryExector"/>. It may be invoked multiple times depending on the behavior of the <see cref="IGraphQLServerClient"/>.
		/// </summary>
		/// <param name="queryExector">A <see cref="Func{T, TResult}"/> which executes a single query on a given <see cref="IGraphQLClient"/> and returns a <see cref="ValueTask"/> representing the running operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask RunQuery(Func<IGraphQLClient, ValueTask> queryExector);
	}
}
