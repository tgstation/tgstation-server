using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Factory for creating <see cref="IServerClient"/>s
	/// </summary>
	public interface IServerClientFactory
	{
		/// <summary>
		/// Create a <see cref="IServerClient"/>
		/// </summary>
		/// <param name="host">The URL to access TGS</param>
		/// <param name="username">The username to for the <see cref="IServerClient"/></param>
		/// <param name="password">The password for the <see cref="IServerClient"/></param>
		/// <param name="timeout">The <see cref="TimeSpan"/> representing timeout for the connection</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IServerClient"/></returns>
		Task<IServerClient> CreateServerClient(Uri host, string username, string password, TimeSpan timeout = default, CancellationToken cancellationToken = default);

		/// <summary>
		/// Create a <see cref="IServerClient"/>
		/// </summary>
		/// <param name="host">The URL to access TGS</param>
		/// <param name="token">The <see cref="Token"/> to access the API with</param>
		/// <param name="timeout">The <see cref="TimeSpan"/> representing timeout for the connection</param>
		/// <returns>A new <see cref="IServerClient"/></returns>
		IServerClient CreateServerClient(Uri host, Token token, TimeSpan timeout = default);
	}
}
