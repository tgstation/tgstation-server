using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace TGS.Server.Service
{
	/// <summary>
	/// Windows <see cref="ServiceBase"/> adapter for <see cref="Server"/>
	/// </summary>
	sealed class Service : ServiceBase, ILogger
	{
		/// <summary>
		/// The <see cref="IServer"/> for the <see cref="Service"/>
		/// </summary>
		readonly IServer Server;

		/// <summary>
		/// Construct a <see cref="Service"/>
		/// </summary>
		/// <param name="serverFactory">The <see cref="IServerFactory"/> for creating <see cref="Server"/></param>
		public Service(IServerFactory serverFactory)
		{
			ServiceName = "TG Station Server";
			Server = serverFactory.CreateServer(this);
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
			Server.Start(args);
		}

		/// <summary>
		/// Called when the <see cref="Service"/> is stopped. Calls <see cref="IDisposable.Dispose"/> on <see cref="activeServer"/>
		/// </summary>
		protected override void OnStop()
		{
			Server.Stop();
		}

		/// <summary>
		/// Cleans up <see cref="activeServer"/>
		/// </summary>
		/// <param name="disposing"><see langword="true"/> if <see cref="Dispose()"/> was called manually, <see langword="false"/> if it was from the finalizer</param>
		protected override void Dispose(bool disposing)
		{
			Server.Dispose();
			base.Dispose(disposing);
		}
	}
}
