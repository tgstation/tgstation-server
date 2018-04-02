using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	public sealed class ServerClientFactory : IServerClientFactory
	{
		/// <inheritdoc />
		public Task<IServerClient> CreateServerClient(string hostname, string username, string password, int timeout = 10000) => throw new NotImplementedException();

		/// <inheritdoc />
		public Task<IServerClient> CreateServerClient(string hostname, string token, int timeout = 10000) => throw new NotImplementedException();
	}
}
