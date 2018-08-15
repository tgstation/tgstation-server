using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class SessionController : ISessionController, ICommHandler
	{
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
		public Task<LaunchResult> LaunchResult { get; }

		/// <inheritdoc />
		public Task<int> Lifetime => process.Lifetime;

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
		/// The <see cref="ICommContext"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly ICommContext interopContext;

		/// <summary>
		/// The <see cref="IProcess"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IProcess process;

		/// <summary>
		/// The <see cref="IByondExecutableLock"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IByondExecutableLock byondLock;

		/// <summary>
		/// The <see cref="IJsonTrackingContext"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IJsonTrackingContext chatJsonTrackingContext;

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IChat chat;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly ILogger<SessionController> logger;

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
		/// If <see cref="process"/> should be kept alive instead
		/// </summary>
		bool released;

		/// <summary>
		/// Construct a <see cref="SessionController"/>
		/// </summary>
		/// <param name="reattachInformation">The value of <see cref="reattachInformation"/></param>
		/// <param name="process">The value of <see cref="process"/></param>
		/// <param name="byondLock">The value of <see cref="byondLock"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="interopContext">The value of <see cref="interopContext"/></param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="chatJsonTrackingContext">The value of <see cref="chatJsonTrackingContext"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public SessionController(ReattachInformation reattachInformation, IProcess process, IByondExecutableLock byondLock, IByondTopicSender byondTopicSender, IJsonTrackingContext chatJsonTrackingContext, ICommContext interopContext, IChat chat, ILogger<SessionController> logger)
		{
			this.chatJsonTrackingContext = chatJsonTrackingContext; //null valid
			this.reattachInformation = reattachInformation ?? throw new ArgumentNullException(nameof(reattachInformation));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.process = process ?? throw new ArgumentNullException(nameof(process));
			this.byondLock = byondLock ?? throw new ArgumentNullException(nameof(byondLock));
			this.interopContext = interopContext ?? throw new ArgumentNullException(nameof(interopContext));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			interopContext.RegisterHandler(this);

			portClosed = false;
			disposed = false;
			apiValidated = false;
			released = false;

			rebootTcs = new TaskCompletionSource<object>();

			async Task<LaunchResult> GetLaunchResult()
			{
				var startTime = DateTimeOffset.Now;
				await process.Startup.ConfigureAwait(false);
				var result = new LaunchResult
				{
					ExitCode = process.Lifetime.IsCompleted ? (int?)await process.Lifetime.ConfigureAwait(false) : null,
					StartupTime = DateTimeOffset.Now - startTime
				};
				return result;
			};
			LaunchResult = GetLaunchResult();
		}

		/// <summary>
		/// Finalize the <see cref="SessionController"/>
		/// </summary>
		/// <remarks>The finalizer dispose pattern is necessary so we don't accidentally leak the executable</remarks>
#pragma warning disable CA1821 // Remove empty Finalizers //TODO remove this when https://github.com/dotnet/roslyn-analyzers/issues/1241 is fixed
		~SessionController() => Dispose(false);
#pragma warning restore CA1821 // Remove empty Finalizers

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc />
		void Dispose(bool disposing)
		{
			lock (this)
			{
				if (disposed)
					return;
				if (disposing)
				{
					if (!released)
					{
						process.Terminate();
						byondLock.Dispose();
					}
					process.Dispose();
					interopContext.Dispose();
					Dmb?.Dispose(); //will be null when released
					chatJsonTrackingContext.Dispose();
					disposed = true;
				}
				else
				{
					if (logger != null)
						logger.LogError("Being disposed via finalizer!");
					if (!released)
						if (process != null)
							process.Terminate();
						else if (logger != null)
							logger.LogCritical("Unable to terminate active DreamDaemon session due to finalizer ordering!");
				}
			}
		}

		/// <inheritdoc />
		public Task HandleInterop(CommCommand command, CancellationToken cancellationToken)
		{
			if (command == null)
				throw new ArgumentNullException(nameof(command));

			var query = command.Parameters;

			object content;
			if (query.TryGetValue(Constants.DMParameterCommand, out var method))
			{
				content = new object();
				switch (method)
				{
					case Constants.DMCommandIdentify:
						lock (this)
							if (portClosed)
								content = new Dictionary<string, int> { { Constants.DMParameterData, nextPort } };
						break;
					case Constants.DMCommandOnline:
						lock (this)
							if (portClosed)
							{
								reattachInformation.Port = nextPort;
								portAssignmentTcs.TrySetResult(true);
								portAssignmentTcs = null;
								portClosed = false;
							}
						break;
					case Constants.DMCommandApiValidate:
						apiValidated = true;
						break;
					case Constants.DMCommandWorldReboot:
						if (ClosePortOnReboot)
						{
							content = new Dictionary<string, int> { { Constants.DMParameterData, 0 } };
							portClosed = true;
						}
						else
							ClosePortOnReboot = true;
						var oldTcs = rebootTcs;
						rebootTcs = new TaskCompletionSource<object>();
						oldTcs.SetResult(null);
						break;
					default:
						content = new ErrorMessage { Message = "Requested command not supported!" };
						break;
				}
			}
			else
				content = new ErrorMessage { Message = "Missing command parameter!" };

			var json = JsonConvert.SerializeObject(content);
			return SendCommand(String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", byondTopicSender.SanitizeString(Constants.DMTopicInteropResponse), byondTopicSender.SanitizeString(Constants.DMParameterData), byondTopicSender.SanitizeString(json)), cancellationToken);
		}

		/// <summary>
		/// Throws an <see cref="ObjectDisposedException"/> if <see cref="Dispose(bool)"/> has been called
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
			released = true;
			Dispose();
			Dmb.KeepAlive();
			reattachInformation.Dmb = tmpProvider;
			return reattachInformation;
		}

		/// <inheritdoc />
		public async Task<string> SendCommand(string command, CancellationToken cancellationToken)
		{
			try
			{
				var commandString = String.Format(CultureInfo.InvariantCulture,
					"?{0}={1}&{2}={3}",
					byondTopicSender.SanitizeString(Constants.DMInteropAccessIdentifier),
					byondTopicSender.SanitizeString(reattachInformation.AccessIdentifier),
					byondTopicSender.SanitizeString(Constants.DMParameterCommand),
					//intentionally don't sanitize command, that's up to the caller
					command);

				logger.LogTrace("Export to :{0}. Query: {1}", reattachInformation.Port, commandString);

				return await byondTopicSender.SendTopic(
					new IPEndPoint(IPAddress.Loopback, reattachInformation.Port),
					commandString,
					cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				logger.LogInformation("Send command exception:{0}{1}", Environment.NewLine, e.Message);
				return null;
			}
		}

		async Task<bool> SetPortImpl(ushort port, CancellationToken cancellationToken) => await SendCommand(String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", byondTopicSender.SanitizeString(Constants.DMTopicChangePort), byondTopicSender.SanitizeString(Constants.DMParameterData), byondTopicSender.SanitizeString(port.ToString(CultureInfo.InvariantCulture))), cancellationToken).ConfigureAwait(false) == Constants.DMResponseSuccess;

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
					{
						//someone was trying to change the port before us, ignore them
						//shouldn't happen anyway, add logging here
						logger.LogWarning("Hey uhhh, this shouldn't happen ok? Pls to tell cyberboss. SessionController.SetPort");
						portAssignmentTcs.TrySetResult(false);
					}
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

			return await SendCommand(String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", Constants.DMTopicChangeReboot, Constants.DMParameterData, (int)newRebootState), cancellationToken).ConfigureAwait(false) == Constants.DMResponseSuccess;
		}

		/// <inheritdoc />
		public void ResetRebootState()
		{
			CheckDisposed();
			reattachInformation.RebootState = RebootState.Normal;
		}
	}
}
