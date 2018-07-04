using Byond.TopicSender;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class SessionManager : ISessionManager, IInteropConsumer
	{
		/// <summary>
		/// Generic OK response
		/// </summary>
		const string DMResponseOKGeneric = "OK";

		/// <summary>
		/// The server is requesting to know what port to open on
		/// </summary>
		const string DMQueryPortsClosed = "its_dark";

		const string DMParameterAccessIdentifier = "access";
		const string DMParameterCommand = "command";
		const string DMParameterNewPort = "new_port";
		const string DMParameterNewRebootMode = "new_reboot_mode";

		const string DMCommandChangePort = "change_port";
		const string DMCommandChangeReboot = "change_reboot";

		/// <inheritdoc />
		public bool IsPrimary
		{
			get
			{
				CheckDisposed();
				return reattachInformation.IsPrimary;
			}
		}

		/// <inheritdoc />
		public IDmbProvider Dmb
		{
			get
			{
				CheckDisposed();
				return reattachInformation.Dmb;
			}
		}

		/// <inheritdoc />
		public ushort? Port
		{
			get
			{
				CheckDisposed();
				if (portClosed)
					return null;
				return reattachInformation.Port;
			}
		}

		/// <inheritdoc />
		public RebootState RebootState
		{
			get
			{
				CheckDisposed();
				return reattachInformation.RebootState;
			}
		}

		/// <summary>
		/// The up to date <see cref="ReattachInformation"/>
		/// </summary>
		readonly ReattachInformation reattachInformation;

		/// <summary>
		/// The <see cref="IByondTopicSender"/> for the <see cref="SessionManager"/>
		/// </summary>
		readonly IByondTopicSender byondTopicSender;

		/// <summary>
		/// The <see cref="IInteropContext"/> for the <see cref="SessionManager"/>
		/// </summary>
		readonly IInteropContext interopContext;

		/// <summary>
		/// The <see cref="ISession"/> for the <see cref="SessionManager"/>
		/// </summary>
		readonly ISession session;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> <see cref="SetPortImpl(ushort, CancellationToken)"/> waits on when DreamDaemon currently has it's ports closed
		/// </summary>
		TaskCompletionSource<bool> portAssignmentTcs;
		/// <summary>
		/// The port to assign DreamDaemon when it queries for it
		/// </summary>
		ushort? nextPort;

		/// <summary>
		/// If we know DreamDaemon currently has it's port closed
		/// </summary>
		bool portClosed;
		/// <summary>
		/// If the <see cref="SessionManager"/> has been disposed
		/// </summary>
		bool disposed;

		/// <summary>
		/// Construct a <see cref="SessionManager"/>
		/// </summary>
		/// <param name="reattachInformation">The value of <see cref="reattachInformation"/></param>
		/// <param name="session">The value of <see cref="session"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="interopRegistrar">The <see cref="IInteropRegistrar"/> used to construct <see cref="interopContext"/></param>
		public SessionManager(ReattachInformation reattachInformation, ISession session, IByondTopicSender byondTopicSender, IInteropRegistrar interopRegistrar)
		{
			this.reattachInformation = reattachInformation ?? throw new ArgumentNullException(nameof(reattachInformation));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.session = session ?? throw new ArgumentNullException(nameof(session));
			if (interopRegistrar == null)
				throw new ArgumentNullException(nameof(interopRegistrar));

			interopContext = interopRegistrar.Register(reattachInformation.AccessIdentifier, this);

			portClosed = false;
			disposed = false;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (this)
			{
				if (disposed)
					return;
				session.Dispose();
				interopContext.Dispose();
				Dmb?.Dispose(); //will be null when released
				disposed = true;
			}
		}

		/// <inheritdoc />
		public Task<object> HandleInterop(IQueryCollection query, CancellationToken cancellationToken)
		{
			if (query == null)
				throw new ArgumentNullException(nameof(query));
		
			if (query.ContainsKey(DMQueryPortsClosed))
				lock (this)
					if(nextPort.HasValue)
					{
						var newPort = nextPort.Value;
						nextPort = null;
						portAssignmentTcs.SetResult(true);
						portAssignmentTcs = null;
						return Task.FromResult<object>(new { new_port = newPort });
					}
			return Task.FromResult(new object());
		}

		/// <summary>
		/// Throws an <see cref="ObjectDisposedException"/> if <see cref="Dispose"/> has been called
		/// </summary>
		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(SessionManager));
		}

		/// <inheritdoc />
		public ReattachInformation Release()
		{
			CheckDisposed();
			//we still don't want to dispose the dmb yet, even though we're keeping it alive
			var tmpProvider = reattachInformation.Dmb;
			reattachInformation.Dmb = null;
			Dispose();
			Dmb.KeepAlive();
			reattachInformation.Dmb = tmpProvider;
			return reattachInformation;
		}

		/// <inheritdoc />
		public Task<string> SendCommand(string command, CancellationToken cancellationToken) => byondTopicSender.SendTopic(new IPEndPoint(IPAddress.Loopback, reattachInformation.Port), String.Format(CultureInfo.InvariantCulture, "?{0}={1}&{2}={3}", DMParameterAccessIdentifier, reattachInformation.AccessIdentifier, DMParameterCommand, command), cancellationToken);

		async Task<bool> SetPortImpl(ushort port, CancellationToken cancellationToken) => await SendCommand(String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", DMCommandChangePort, DMParameterNewPort, port), cancellationToken).ConfigureAwait(false) == DMResponseOKGeneric;

		/// <inheritdoc />
		public async Task<bool> ClosePort(CancellationToken cancellationToken)
		{
			CheckDisposed();
			if (portClosed)
				return true;
			if (await SetPortImpl(0, cancellationToken).ConfigureAwait(false))
			{
				portClosed = true;
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		public async Task<bool> SetPort(ushort port, CancellationToken cancellatonToken)
		{
			CheckDisposed();
			if (portClosed)
			{
				Task toWait;
				lock (this)
				{
					nextPort = port;
					portAssignmentTcs = new TaskCompletionSource<bool>();
					toWait = portAssignmentTcs.Task;
				}
				await toWait.ConfigureAwait(false);
			}

			if (port == 0)
				throw new ArgumentOutOfRangeException(nameof(port), port, "port must not be zero!");
			return await SetPortImpl(port, cancellatonToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<bool> SetRebootState(RebootState newRebootState, CancellationToken cancellationToken)
		{
			if (RebootState == newRebootState)
				return true;

			return await SendCommand(String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", DMCommandChangeReboot, DMParameterNewRebootMode, (int)newRebootState), cancellationToken).ConfigureAwait(false) == DMResponseOKGeneric;
		}
	}
}
