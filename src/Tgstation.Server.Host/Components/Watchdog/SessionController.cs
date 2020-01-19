using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
using Tgstation.Server.Host.System;

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
		public ApiValidationStatus ApiValidationStatus
		{
			get
			{
				if (!Lifetime.IsCompleted)
					throw new InvalidOperationException("ApiValidated cannot be checked while Lifetime is incomplete!");
				return apiValidationStatus;
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
				if (portClosedForReboot)
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
		public bool ClosePortOnReboot { get; set; }

		/// <inheritdoc />
		public bool TerminationWasRequested { get; private set; }

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
		/// The <see cref="DreamDaemonSecurity"/> level the <see cref="process"/> was launched with
		/// </summary>
		readonly DreamDaemonSecurity? launchSecurityLevel;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> <see cref="SetPort(ushort, CancellationToken)"/> waits on when DreamDaemon currently has it's ports closed
		/// </summary>
		TaskCompletionSource<bool> portAssignmentTcs;

		/// <summary>
		/// The port to assign DreamDaemon when it queries for it
		/// </summary>
		ushort? nextPort;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> that completes when DD tells us about a reboot
		/// </summary>
		TaskCompletionSource<object> rebootTcs;

		/// <summary>
		/// If we know DreamDaemon currently has it's port closed
		/// </summary>
		bool portClosedForReboot;

		/// <summary>
		/// If the <see cref="SessionController"/> has been disposed
		/// </summary>
		bool disposed;

		/// <summary>
		/// The <see cref="ApiValidationStatus"/> for the <see cref="SessionController"/>
		/// </summary>
		ApiValidationStatus apiValidationStatus;

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
		/// <param name="launchSecurityLevel">The value of <see cref="launchSecurityLevel"/></param>
		/// <param name="startupTimeout">The optional time to wait before failing the <see cref="LaunchResult"/></param>
		public SessionController(
			ReattachInformation reattachInformation,
			IProcess process,
			IByondExecutableLock byondLock,
			IByondTopicSender byondTopicSender,
			IJsonTrackingContext chatJsonTrackingContext,
			ICommContext interopContext,
			IChat chat,
			ILogger<SessionController> logger,
			DreamDaemonSecurity? launchSecurityLevel,
			uint? startupTimeout)
		{
			this.chatJsonTrackingContext = chatJsonTrackingContext; // null valid
			this.reattachInformation = reattachInformation ?? throw new ArgumentNullException(nameof(reattachInformation));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.process = process ?? throw new ArgumentNullException(nameof(process));
			this.byondLock = byondLock ?? throw new ArgumentNullException(nameof(byondLock));
			this.interopContext = interopContext ?? throw new ArgumentNullException(nameof(interopContext));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			this.launchSecurityLevel = launchSecurityLevel;

			interopContext.RegisterHandler(this);

			portClosedForReboot = false;
			disposed = false;
			apiValidationStatus = ApiValidationStatus.NeverValidated;
			released = false;

			rebootTcs = new TaskCompletionSource<object>();

			process.Lifetime.ContinueWith(x => chatJsonTrackingContext.Active = false, TaskScheduler.Current);

			async Task<LaunchResult> GetLaunchResult()
			{
				var startTime = DateTimeOffset.Now;
				Task toAwait = process.Startup;

				if (startupTimeout.HasValue)
					toAwait = Task.WhenAny(process.Startup, Task.Delay(startTime.AddSeconds(startupTimeout.Value) - startTime));

				await toAwait.ConfigureAwait(false);

				var result = new LaunchResult
				{
					ExitCode = process.Lifetime.IsCompleted ? (int?)await process.Lifetime.ConfigureAwait(false) : null,
					StartupTime = process.Startup.IsCompleted ? (TimeSpan?)(DateTimeOffset.Now - startTime) : null
				};
				return result;
			}

			LaunchResult = GetLaunchResult();

			logger.LogDebug("Created session controller. Primary: {0}, CommsKey: {1}, Port: {2}", IsPrimary, reattachInformation.AccessIdentifier, Port);
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="SessionController"/> class.
		/// </summary>
		/// <remarks>The finalizer dispose pattern is necessary so we don't accidentally leak the executable</remarks>
#pragma warning disable CA1821 // Remove empty Finalizers TODO: remove this when https://github.com/dotnet/roslyn-analyzers/issues/1241 is fixed
		~SessionController() => Dispose(false);
#pragma warning restore CA1821 // Remove empty Finalizers

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Implements the <see cref="IDisposable"/> pattern
		/// </summary>
		/// <param name="disposing">If this function was NOT called by the finalizer</param>
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
					Dmb?.Dispose(); // will be null when released
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
		#pragma warning disable CA1502 // TODO: Decomplexify
		public async Task HandleInterop(CommCommand command, CancellationToken cancellationToken)
		{
			if (command == null)
				throw new ArgumentNullException(nameof(command));

			var query = command.Parameters;

			object content;
			Action postRespond = null;
			ushort? overrideResponsePort = null;
			if (query.TryGetValue(Constants.DMParameterCommand, out var method))
			{
				content = new object();
				switch (method)
				{
					case Constants.DMCommandChat:
						try
						{
							var message = JsonConvert.DeserializeObject<Response>(command.RawJson, new JsonSerializerSettings
							{
								ContractResolver = new CamelCasePropertyNamesContractResolver()
							});
							if (message.ChannelIds == null)
								throw new InvalidOperationException("Missing ChannelIds field!");
							if (message.Message == null)
								throw new InvalidOperationException("Missing Message field!");
							await chat.SendMessage(message.Message, message.ChannelIds, cancellationToken).ConfigureAwait(false);
						}
						catch (Exception e)
						{
							logger.LogDebug("Exception while decoding chat message! Exception: {0}", e);
							goto default;
						}

						break;
					case Constants.DMCommandServerPrimed:
						// currently unused, maybe in the future
						break;
					case Constants.DMCommandEndProcess:
						TerminationWasRequested = true;
						process.Terminate();
						return;
					case Constants.DMCommandNewPort:
						lock (this)
						{
							if (!query.TryGetValue(Constants.DMParameterData, out var stringPortObject) || !UInt16.TryParse(stringPortObject as string, out var currentPort))
							{
								/////UHHHH
								logger.LogWarning("DreamDaemon sent new port command without providing it's own!");
								content = new ErrorMessage { Message = "Missing stringified port as data parameter!" };
								break;
							}

							if (!nextPort.HasValue)
								reattachInformation.Port = currentPort; // not ready yet, so what we'll do is accept the random port DD opened on for now and change it later when we decide to
							else
							{
								// nextPort is ready, tell DD to switch to that
								// if it fails it'll kill itself
								content = new Dictionary<string, ushort> { { Constants.DMParameterData, nextPort.Value } };
								reattachInformation.Port = nextPort.Value;
								overrideResponsePort = currentPort;
								nextPort = null;

								// we'll also get here from SetPort so complete that task
								var tmpTcs = portAssignmentTcs;
								portAssignmentTcs = null;
								if (tmpTcs != null)
									postRespond = () => tmpTcs.SetResult(true);
							}

							portClosedForReboot = false;
						}

						break;
					case Constants.DMCommandApiValidate:
						if (!launchSecurityLevel.HasValue)
						{
							logger.LogWarning("DreamDaemon requested API validation but no intial security level was passed to the session controller!");
							apiValidationStatus = ApiValidationStatus.UnaskedValidationRequest;
							content = new ErrorMessage { Message = "Invalid API validation request!" };
							break;
						}

						if (!query.TryGetValue(Constants.DMParameterData, out var stringMinimumSecurityLevelObject) || !Enum.TryParse<DreamDaemonSecurity>(stringMinimumSecurityLevelObject as string, out var minimumSecurityLevel))
							apiValidationStatus = ApiValidationStatus.BadValidationRequest;
						else
							switch (minimumSecurityLevel)
							{
								case DreamDaemonSecurity.Safe:
									apiValidationStatus = ApiValidationStatus.RequiresSafe;
									break;
								case DreamDaemonSecurity.Ultrasafe:
									apiValidationStatus = ApiValidationStatus.RequiresUltrasafe;
									break;
								case DreamDaemonSecurity.Trusted:
									apiValidationStatus = ApiValidationStatus.RequiresTrusted;
									break;
								default:
									throw new InvalidOperationException("Enum.TryParse failed to validate the DreamDaemonSecurity range!");
							}

						break;
					case Constants.DMCommandWorldReboot:
						if (ClosePortOnReboot)
						{
							chatJsonTrackingContext.Active = false;
							content = new Dictionary<string, int> { { Constants.DMParameterData, 0 } };
							portClosedForReboot = true;
						}

						var oldTcs = rebootTcs;
						rebootTcs = new TaskCompletionSource<object>();
						postRespond = () => oldTcs.SetResult(null);
						break;
					default:
						content = new ErrorMessage { Message = "Requested command not supported!" };
						break;
				}
			}
			else
				content = new ErrorMessage { Message = "Missing command parameter!" };

			var json = JsonConvert.SerializeObject(content);
			var response = await SendCommand(String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", byondTopicSender.SanitizeString(Constants.DMTopicInteropResponse), byondTopicSender.SanitizeString(Constants.DMParameterData), byondTopicSender.SanitizeString(json)), overrideResponsePort, cancellationToken).ConfigureAwait(false);

			if (response != Constants.DMResponseSuccess)
				logger.LogWarning("Received error response while responding to interop: {0}", response);

			postRespond?.Invoke();
		}
		#pragma warning restore CA1502

		/// <summary>
		/// Throws an <see cref="ObjectDisposedException"/> if <see cref="Dispose(bool)"/> has been called
		/// </summary>
		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(SessionController));
		}

		/// <inheritdoc />
		public void EnableCustomChatCommands() => chatJsonTrackingContext.Active = true;

		/// <inheritdoc />
		public ReattachInformation Release()
		{
			CheckDisposed();

			// we still don't want to dispose the dmb yet, even though we're keeping it alive
			var tmpProvider = reattachInformation.Dmb;
			reattachInformation.Dmb = null;
			released = true;
			Dispose();
			byondLock.DoNotDeleteThisSession();
			tmpProvider.KeepAlive();
			reattachInformation.Dmb = tmpProvider;
			return reattachInformation;
		}

		/// <inheritdoc />
		public Task<string> SendCommand(string command, CancellationToken cancellationToken) => SendCommand(command, null, cancellationToken);

		async Task<string> SendCommand(string command, ushort? overridePort, CancellationToken cancellationToken)
		{
			if (Lifetime.IsCompleted)
			{
				logger.LogWarning(
					"Attempted to send a command to an inactive SessionController{1}: {0}",
					command,
					overridePort.HasValue ? $" (Override port: {overridePort.Value})" : String.Empty);
				return null;
			}

			try
			{
				var commandString = String.Format(CultureInfo.InvariantCulture,
					"?{0}={1}&{2}={3}",
					byondTopicSender.SanitizeString(Constants.DMInteropAccessIdentifier),
					byondTopicSender.SanitizeString(reattachInformation.AccessIdentifier),
					byondTopicSender.SanitizeString(Constants.DMParameterCommand),
					command); // intentionally don't sanitize command, that's up to the caller

				var targetPort = overridePort ?? reattachInformation.Port;
				logger.LogTrace("Export to :{0}. Query: {1}", targetPort, commandString);

				return await byondTopicSender.SendTopic(
					new IPEndPoint(IPAddress.Loopback, targetPort),
					commandString,
					cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				logger.LogInformation("Send command exception:{0}{1}", Environment.NewLine, e.Message);
				return null;
			}
		}

		/// <inheritdoc />
		public Task<bool> SetPort(ushort port, CancellationToken cancellationToken)
		{
			CheckDisposed();

			if (port == 0)
				throw new ArgumentOutOfRangeException(nameof(port), port, "port must not be zero!");

			async Task<bool> ImmediateTopicPortChange()
			{
				var commandResult = await SendCommand(String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", byondTopicSender.SanitizeString(Constants.DMTopicChangePort), byondTopicSender.SanitizeString(Constants.DMParameterData), byondTopicSender.SanitizeString(port.ToString(CultureInfo.InvariantCulture))), cancellationToken).ConfigureAwait(false);

				if (commandResult != Constants.DMResponseSuccess)
				{
					logger.LogWarning("Failed port change! DD says: {0}", commandResult);
					return false;
				}

				reattachInformation.Port = port;
				return true;
			}

			lock (this)
				if (portClosedForReboot)
				{
					if (portAssignmentTcs != null)
						throw new InvalidOperationException("A port change operation is already in progress!");
					nextPort = port;
					portAssignmentTcs = new TaskCompletionSource<bool>();
					return portAssignmentTcs.Task;
				}
				else
					return ImmediateTopicPortChange();
		}

		/// <inheritdoc />
		public async Task<bool> SetRebootState(RebootState newRebootState, CancellationToken cancellationToken)
		{
			if (RebootState == newRebootState)
				return true;
			reattachInformation.RebootState = newRebootState;
			return await SendCommand(String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", byondTopicSender.SanitizeString(Constants.DMTopicChangeReboot), byondTopicSender.SanitizeString(Constants.DMParameterData), (int)newRebootState), cancellationToken).ConfigureAwait(false) == Constants.DMResponseSuccess;
		}

		/// <inheritdoc />
		public void ResetRebootState()
		{
			CheckDisposed();
			reattachInformation.RebootState = RebootState.Normal;
		}

		/// <inheritdoc />
		public void SetHighPriority() => process.SetHighPriority();

		/// <inheritdoc />
		public void ReplaceDmbProvider(IDmbProvider dmbProvider)
		{
#pragma warning disable IDE0016 // Use 'throw' expression
			if (dmbProvider == null)
				throw new ArgumentNullException(nameof(dmbProvider));
#pragma warning restore IDE0016 // Use 'throw' expression

			reattachInformation.Dmb.Dispose();
			reattachInformation.Dmb = dmbProvider;
		}
	}
}
