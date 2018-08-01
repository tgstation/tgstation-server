using Microsoft.AspNetCore;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	sealed class ServerFactory : IServerFactory
	{
		/// <inheritdoc />
		public IServer CreateServer(string[] args, string updatePath) => new Server(WebHost.CreateDefaultBuilder(args), updatePath);
	}
}
