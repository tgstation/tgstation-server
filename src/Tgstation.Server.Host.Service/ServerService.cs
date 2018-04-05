using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Represents a <see cref="IServer"/> as a <see cref="ServiceBase"/>
	/// </summary>
	sealed class ServerService : ServiceBase
	{
		/// <summary>
		/// The <see cref="IServer"/> for the <see cref="ServerService"/>
		/// </summary>
		IServer server;

		/// <summary>
		/// The <see cref="Task"/> recieved from <see cref="IServer.RunAsync(string[], CancellationToken)"/> of <see cref="server"/>
		/// </summary>
		Task serverTask;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="ServerService"/>
		/// </summary>
		CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// Construct a <see cref="ServerService"/>
		/// </summary>
		/// <param name="serverFactory">The <see cref="IServerFactory"/> to create <see cref="server"/> with</param>
		public ServerService(IServerFactory serverFactory)
		{
			ServiceName = "tgstation-server";
			server = serverFactory.CreateServer();
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			server.Dispose();
			cancellationTokenSource.Dispose();
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		protected override void OnStart(string[] args)
		{
			cancellationTokenSource?.Dispose();
			cancellationTokenSource = new CancellationTokenSource();
			serverTask = server.RunAsync(args, cancellationTokenSource.Token);
		}

		/// <inheritdoc />
		protected override void OnStop()
		{
			cancellationTokenSource.Cancel();
			serverTask.GetAwaiter().GetResult();
		}
	}
}