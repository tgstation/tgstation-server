using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	sealed class Server : IServer
    {
        /// <inheritdoc />
        public string UpdatePath => "Tgstation.Server.Host.New.dll";

        /// <inheritdoc />
        public void Dispose() { }

		/// <inheritdoc />
		public Task RunAsync(string[] args, CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
