using Microsoft.AspNetCore;
using System;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	public sealed class ServerFactory : IServerFactory
	{
		/// <inheritdoc />
		public IServer CreateServer(string[] args, string updatePath) => new Server(WebHost.CreateDefaultBuilder(args ?? throw new ArgumentNullException(nameof(args))), updatePath);
	}
}
