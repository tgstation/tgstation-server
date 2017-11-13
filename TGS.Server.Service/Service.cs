using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace TGS.Server.Service
{
	/// <summary>
	/// Windows <see cref="ServiceBase"/> adapter for <see cref="Server"/>
	/// </summary>
	public sealed class Service : ServiceBase, ILogger
	{
		/// <summary>
		/// The entry point for the program. Calls <see cref="ServiceBase.Run(ServiceBase)"/> with a new <see cref="Service"/> as a parameter
		/// </summary>
		public static void Main() => Run(new Service());

		/// <summary>
		/// The <see cref="IServer"/> the <see cref="Service"/> manages
		/// </summary>
		readonly IServer activeServer;

		/// <summary>
		/// Construct a <see cref="Service"/>
		/// </summary>
		Service()
		{
			activeServer = new Server(this, ServerConfig.LoadServerConfig());
		}

		/// <inheritdoc />
		public void WriteAccess(string username, bool authSuccess, byte loggingID)
		{
			EventLog.WriteEntry(String.Format("Access from: {0}", username), authSuccess ? EventLogEntryType.SuccessAudit : EventLogEntryType.FailureAudit , (int)EventID.Authentication + loggingID);
		}

		/// <inheritdoc />
		public void WriteError(string message, EventID id, byte loggingID)
		{
			EventLog.WriteEntry(message, EventLogEntryType.Error, (int)id + loggingID);
		}

		/// <inheritdoc />
		public void WriteInfo(string message, EventID id, byte loggingID)
		{
			EventLog.WriteEntry(message, EventLogEntryType.Information, (int)id + loggingID);
		}

		/// <inheritdoc />
		public void WriteWarning(string message, EventID id, byte loggingID)
		{
			EventLog.WriteEntry(message, EventLogEntryType.Warning, (int)id + loggingID);
		}

		/// <summary>
		/// Called when the <see cref="Service"/> is started. Creates a new <see cref="activeServer"/>
		/// </summary>
		/// <param name="args">The service start arguments</param>
		protected override void OnStart(string[] args)
		{
			activeServer.Start(args);
		}

		/// <summary>
		/// Called when the <see cref="Service"/> is stopped. Calls <see cref="IDisposable.Dispose"/> on <see cref="activeServer"/>
		/// </summary>
		protected override void OnStop()
		{
			activeServer.Stop();
		}

		/// <summary>
		/// Cleans up <see cref="activeServer"/>
		/// </summary>
		/// <param name="disposing"><see langword="true"/> if <see cref="Dispose()"/> was called manually, <see langword="false"/> if it was from the finalizer</param>
		protected override void Dispose(bool disposing)
		{
			activeServer.Dispose();
			base.Dispose(disposing);
		}
	}
}
