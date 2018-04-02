using System.Threading.Tasks;

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
		/// <param name="hostname">The URL to access tgstation-server at</param>
		/// <param name="username">The username to for the <see cref="IServerClient"/></param>
		/// <param name="password">The password for the <see cref="IServerClient"/></param>
		/// <param name="timeout">The initial timeout for the connection</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IServerClient"/></returns>
		/// <exception cref="System.UnauthorizedAccessException">If the <paramref name="username"/> and/or <paramref name="password"/> is invalid</exception>
		Task<IServerClient> CreateServerClient(string hostname, string username, string password, int timeout = 10000);

		/// <summary>
		/// Create a <see cref="IServerClient"/>
		/// </summary>
		/// <param name="hostname">The URL to access tgstation-server at</param>
		/// <param name="token">The <see cref="Api.Models.Token.Value"/> to access the API with</param>
		/// <param name="timeout">The initial timeout for the connection</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IServerClient"/></returns>
		/// <exception cref="System.UnauthorizedAccessException">If the <paramref name="token"/> invalid</exception>
		Task<IServerClient> CreateServerClient(string hostname, string token, int timeout = 10000);
	}
}
