using Byond.TopicSender;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class SessionController : ISessionController, IInteropConsumer
	{
		//interop values, match them up with the appropriate api.dm

		//api version 4.0.0.0
		const string DMParamInfoJson = "tgs_json";

		const string DMInteropAccessIdentifier = "tgs_tok";

		const string DMResponseSuccess = "tgs_succ";

		const string DMTopicChangePort = "tgs_port";
		const string DMTopicChangeReboot = "tgs_rmode";
		const string DMTopicChatCommand = "tgs_chat_comm";
		const string DMTopicEvent = "tgs_event";

		const string DMCommandOnline = "tgs_on";
		const string DMCommandIdentify = "tgs_ident";
		const string DMCommandApiValidate = "tgs_validate";
		const string DMCommandServerPrimed = "tgs_prime";
		const string DMCommandWorldReboot = "tgs_reboot";
		const string DMCommandEndProcess = "tgs_kill";
		const string DMCommandChat = "tgs_chat_send";

		const string DMParameterCommand = "tgs_com";
		const string DMParameterData = "tgs_data";

		const string DMParameterNewPort = "new_port";
		const string DMParameterNewRebootMode = "new_rmode";

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
		public bool ClosePortOnReboot { get; set; }

		/// <inheritdoc />
		public bool ApiValidated
		{
			get
			{
				if (!Lifetime.IsCompleted)
					throw new InvalidOperationException("ApiValidated cannot be checked while Lifetime is incomplete!");
				return apiValidated;
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

		/// <inheritdoc />
		public Task<LaunchResult> LaunchResult => session.LaunchResult;

		/// <inheritdoc />
		public Task<int> Lifetime => session.Lifetime;

		/// <inheritdoc />
		public Task OnReboot => rebootTcs.Task;

		/// <summary>
		/// The up to date <see cref="ReattachInformation"/>
		/// </summary>
		readonly ReattachInformation reattachInformation;

		/// <summary>
		/// The <see cref="IByondTopicSender"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IByondTopicSender byondTopicSender;

		/// <summary>
		/// The <see cref="IInteropContext"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IInteropContext interopContext;

		/// <summary>
		/// The <see cref="ISession"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly ISession session;

		/// <summary>
		/// The <see cref="IChatJsonTrackingContext"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IChatJsonTrackingContext chatJsonTrackingContext;

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IChat chat;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> <see cref="SetPortImpl(ushort, CancellationToken)"/> waits on when DreamDaemon currently has it's ports closed
		/// </summary>
		TaskCompletionSource<bool> portAssignmentTcs;
		/// <summary>
		/// The port to assign DreamDaemon when it queries for it
		/// </summary>
		ushort nextPort;
		
		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> that completes when DD tells us about a reboot
		/// </summary>
		TaskCompletionSource<object> rebootTcs;

		/// <summary>
		/// If we know DreamDaemon currently has it's port closed
		/// </summary>
		bool portClosed;
		/// <summary>
		/// If the <see cref="SessionController"/> has been disposed
		/// </summary>
		bool disposed;

		/// <summary>
		/// If the DMAPI was validated
		/// </summary>
		bool apiValidated;

		/// <summary>
		/// Construct a <see cref="SessionController"/>
		/// </summary>
		/// <param name="reattachInformation">The value of <see cref="reattachInformation"/></param>
		/// <param name="session">The value of <see cref="session"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="interopRegistrar">The <see cref="IInteropRegistrar"/> used to construct <see cref="interopContext"/></param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="chatJsonTrackingContext">The value of <see cref="chatJsonTrackingContext"/></param>
		public SessionController(ReattachInformation reattachInformation, ISession session, IByondTopicSender byondTopicSender, IInteropRegistrar interopRegistrar, IChatJsonTrackingContext chatJsonTrackingContext, IChat chat)
		{
			this.chatJsonTrackingContext = chatJsonTrackingContext; //null valid
			this.reattachInformation = reattachInformation ?? throw new ArgumentNullException(nameof(reattachInformation));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.session = session ?? throw new ArgumentNullException(nameof(session));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			if (interopRegistrar == null)
				throw new ArgumentNullException(nameof(interopRegistrar));

			interopContext = interopRegistrar.Register(reattachInformation.AccessIdentifier, this);

			portClosed = false;
			disposed = false;
			apiValidated = false;

			rebootTcs = new TaskCompletionSource<object>();
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
				chatJsonTrackingContext.Dispose();
				disposed = true;
			}
		}

		/// <inheritdoc />
		public Task<object> HandleInterop(IQueryCollection query, CancellationToken cancellationToken)
		{
			if (query == null)
				throw new ArgumentNullException(nameof(query));

			object content = new object();
			var status = HttpStatusCode.OK;

			if (query.TryGetValue(DMParameterCommand, out StringValues values))
				switch (values.First())
				{
					case DMCommandIdentify:
						lock (this)
							if (portClosed)
								content = new Dictionary<string, int> { { DMParameterNewPort, nextPort } };
						break;
					case DMCommandOnline:
						lock (this)
							if (portClosed)
							{
								reattachInformation.Port = nextPort;
								portAssignmentTcs.SetResult(true);
								portAssignmentTcs = null;
								portClosed = false;
							}
						break;
					case DMCommandApiValidate:
						apiValidated = true;
						break;
					case DMCommandWorldReboot:
						if (ClosePortOnReboot)
						{
							content = new Dictionary<string, int> { { DMParameterNewPort, 0 } };
							portClosed = true;
						}
						else
							ClosePortOnReboot = true;
						var oldTcs = rebootTcs;
						rebootTcs = new TaskCompletionSource<object>();
						oldTcs.SetResult(null);
						break;
					default:
						status = HttpStatusCode.BadRequest;
						content = new { message = "Requested command not supported!" };
						break;
				}
			return Task.FromResult<object>(new { STATUS = status, CONTENT = content });
		}

		/// <summary>
		/// Throws an <see cref="ObjectDisposedException"/> if <see cref="Dispose"/> has been called
		/// </summary>
		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(SessionController));
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
		public Task<string> SendCommand(string command, CancellationToken cancellationToken) => byondTopicSender.SendTopic(new IPEndPoint(IPAddress.Loopback, reattachInformation.Port), String.Format(CultureInfo.InvariantCulture, "?{0}={1}&{2}={3}", DMInteropAccessIdentifier, reattachInformation.AccessIdentifier, DMParameterCommand, command), cancellationToken);

		async Task<bool> SetPortImpl(ushort port, CancellationToken cancellationToken) => await SendCommand(String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", DMTopicChangePort, DMParameterNewPort, port), cancellationToken).ConfigureAwait(false) == DMResponseSuccess;

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
				Task<bool> toWait;
				lock (this)
				{
					if (portAssignmentTcs != null)
						//someone was trying to change the port before us, ignore them
						//shouldn't happen anyway, add logging here
						portAssignmentTcs.SetResult(false);
					nextPort = port;
					portAssignmentTcs = new TaskCompletionSource<bool>();
					toWait = portAssignmentTcs.Task;
				}
				return await toWait.ConfigureAwait(false);
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

			return await SendCommand(String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", DMTopicChangeReboot, DMParameterNewRebootMode, (int)newRebootState), cancellationToken).ConfigureAwait(false) == DMResponseSuccess;
		}
	}
}
