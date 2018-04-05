using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	sealed class Server : IServer
    {
        /// <inheritdoc />
        public string UpdatePath => null;

        /// <inheritdoc />
        public void Dispose() { }

		/// <inheritdoc />
		public Task RunAsync(string[] args, CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
