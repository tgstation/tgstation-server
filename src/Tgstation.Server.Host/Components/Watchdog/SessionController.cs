using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Components.Interop.Topic;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class SessionController : ISessionController, IBridgeHandler, IChannelSink
	{
		/// <inheritdoc />
		public DMApiParameters DMApiParameters => reattachInformation;

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
		public Version DMApiVersion { get; private set; }

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
		/// The <see cref="IBridgeRegistration"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IBridgeRegistration bridgeRegistration;

		/// <summary>
		/// The <see cref="IProcess"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IProcess process;

		/// <summary>
		/// The <see cref="IByondExecutableLock"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IByondExecutableLock byondLock;

		/// <summary>
		/// The <see cref="IChatTrackingContext"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IChatTrackingContext chatTrackingContext;

		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly IChatManager chat;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly ILogger<SessionController> logger;

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
		/// <param name="bridgeRegistrar">The <see cref="IBridgeRegistrar"/> used to populate <see cref="bridgeRegistration"/>.</param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="chatTrackingContext">The value of <see cref="chatTrackingContext"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="startupTimeout">The optional time to wait before failing the <see cref="LaunchResult"/></param>
		/// <param name="reattached">If this is a reattached session.</param>
		public SessionController(
			ReattachInformation reattachInformation,
			IProcess process,
			IByondExecutableLock byondLock,
			IByondTopicSender byondTopicSender,
			IChatTrackingContext chatTrackingContext,
			IBridgeRegistrar bridgeRegistrar,
			IChatManager chat,
			ILogger<SessionController> logger,
			uint? startupTimeout,
			bool reattached)
		{
			this.reattachInformation = reattachInformation ?? throw new ArgumentNullException(nameof(reattachInformation));
			this.process = process ?? throw new ArgumentNullException(nameof(process));
			this.byondLock = byondLock ?? throw new ArgumentNullException(nameof(byondLock));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.chatTrackingContext = chatTrackingContext ?? throw new ArgumentNullException(nameof(chatTrackingContext));
			bridgeRegistration = bridgeRegistrar?.RegisterHandler(this) ?? throw new ArgumentNullException(nameof(bridgeRegistrar));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			this.chatTrackingContext.SetChannelSink(this);

			portClosedForReboot = false;
			disposed = false;
			apiValidationStatus = ApiValidationStatus.NeverValidated;
			released = false;

			rebootTcs = new TaskCompletionSource<object>();

			CancellationTokenSource cts = null;
			Task lifetimeContinuation = null;
			lifetimeContinuation = process.Lifetime.ContinueWith(
				x =>
				{
					lock (lifetimeContinuation)
						cts?.Cancel();
					chatTrackingContext.Active = false;

					if (x.Result == 17)
						logger.LogError(
							"DreamDaemon exit code indicates it could not bind to the requested port ({0}). Is there another proccess using it?",
							reattachInformation.Port);
				},
				TaskScheduler.Current);

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

				if (!result.ExitCode.HasValue && reattached)
					using (cts = new CancellationTokenSource())
						try
						{
							await SendCommand(
								new TopicParameters(
									reattachInformation.RuntimeInformation.ServerPort,
									true),
								cts.Token)
								.ConfigureAwait(false);
						}
						finally
						{
							lock (lifetimeContinuation)
								cts = null;
						}

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
					bridgeRegistration.Dispose();
					Dmb?.Dispose(); // will be null when released
					chatTrackingContext.Dispose();
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
		public async Task<BridgeResponse> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			var response = new BridgeResponse();
			switch (parameters.CommandType)
			{
				case BridgeCommandType.ChatSend:
					if (parameters.ChatMessage == null)
						return new BridgeResponse
						{
							ErrorMessage = "Missing chatMessage field!"
						};

					if (parameters.ChatMessage.ChannelIds == null)
						return new BridgeResponse
						{
							ErrorMessage = "Missing channelIds field in chatMessage!"
						};

					if(parameters.ChatMessage.ChannelIds.Any(channelIdString => !UInt64.TryParse(channelIdString, out var _)))
						return new BridgeResponse
						{
							ErrorMessage = "Invalid channelIds in chatMessage!"
						};

					if (parameters.ChatMessage.Text == null)
						return new BridgeResponse
						{
							ErrorMessage = "Missing message field in chatMessage!"
						};

					await chat.SendMessage(
						parameters.ChatMessage.Text,
						parameters.ChatMessage.ChannelIds.Select(UInt64.Parse),
						cancellationToken).ConfigureAwait(false);
					break;
				case BridgeCommandType.Prime:
					// currently unused, maybe in the future
					break;
				case BridgeCommandType.Kill:
					TerminationWasRequested = true;
					process.Terminate();
					break;
				case BridgeCommandType.PortUpdate:
					lock (this)
					{
						if (!parameters.CurrentPort.HasValue)
						{
							/////UHHHH
							logger.LogWarning("DreamDaemon sent new port command without providing it's own!");
							return new BridgeResponse
							{
								ErrorMessage = "Missing stringified port as data parameter!"
							};
						}

						var currentPort = parameters.CurrentPort.Value;
						if (!nextPort.HasValue)
							reattachInformation.Port = parameters.CurrentPort.Value; // not ready yet, so what we'll do is accept the random port DD opened on for now and change it later when we decide to
						else
						{
							// nextPort is ready, tell DD to switch to that
							// if it fails it'll kill itself
							response.NewPort = nextPort.Value;
							reattachInformation.Port = nextPort.Value;
							nextPort = null;

							// we'll also get here from SetPort so complete that task
							var tmpTcs = portAssignmentTcs;
							portAssignmentTcs = null;
							tmpTcs.SetResult(true);
						}

						portClosedForReboot = false;
					}

					break;
				case BridgeCommandType.Startup:
					apiValidationStatus = ApiValidationStatus.BadValidationRequest;
					if (parameters.Version == null)
						return new BridgeResponse
						{
							ErrorMessage = "Missing dmApiVersion field!"
						};

					DMApiVersion = parameters.Version;
					switch (parameters.MinimumSecurityLevel)
					{
						case DreamDaemonSecurity.Ultrasafe:
							apiValidationStatus = ApiValidationStatus.RequiresUltrasafe;
							break;
						case DreamDaemonSecurity.Safe:
							apiValidationStatus = ApiValidationStatus.RequiresSafe;
							break;
						case DreamDaemonSecurity.Trusted:
							apiValidationStatus = ApiValidationStatus.RequiresTrusted;
							break;
						case null:
							return new BridgeResponse
							{
								ErrorMessage = "Missing minimumSecurityLevel field!"
							};
						default:
							return new BridgeResponse
							{
								ErrorMessage = "Invalid minimumSecurityLevel!"
							};
					}

					response.RuntimeInformation = reattachInformation.RuntimeInformation;
					break;
				case BridgeCommandType.Reboot:
					if (ClosePortOnReboot)
					{
						chatTrackingContext.Active = false;
						response.NewPort = 0;
						portClosedForReboot = true;
					}

					var oldTcs = rebootTcs;
					rebootTcs = new TaskCompletionSource<object>();
					oldTcs.SetResult(null);
					break;
				case null:
					response.ErrorMessage = "Missing commandType!";
					break;
				default:
					response.ErrorMessage = "Requested commandType not supported!";
					break;
			}

			return response;
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
		public void EnableCustomChatCommands() => chatTrackingContext.Active = true;

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
		public async Task<TopicResponse> SendCommand(TopicParameters parameters, CancellationToken cancellationToken)
		{
			if (Lifetime.IsCompleted)
			{
				logger.LogWarning(
					"Attempted to send a command to an inactive SessionController: {0}",
					parameters.CommandType);
				return null;
			}

			parameters.AccessIdentifier = reattachInformation.AccessIdentifier;

			var json = JsonConvert.SerializeObject(parameters, DMApiConstants.SerializerSettings);
			logger.LogTrace("Topic request: {0}", json);
			try
			{
				var commandString = String.Format(CultureInfo.InvariantCulture,
					"?{0}={1}",
					byondTopicSender.SanitizeString(DMApiConstants.TopicData),
					byondTopicSender.SanitizeString(json));

				var targetPort = reattachInformation.Port;
				logger.LogTrace("Export to :{0}. Query: {1}", targetPort, commandString);

				var topicReturn = await byondTopicSender.SendTopic(
					new IPEndPoint(IPAddress.Loopback, targetPort),
					commandString,
					cancellationToken).ConfigureAwait(false);

				if (topicReturn != null)
					logger.LogTrace("Topic response: {0}", topicReturn);

				try
				{
					var result = JsonConvert.DeserializeObject<TopicResponse>(topicReturn, DMApiConstants.SerializerSettings);
					if (result.ErrorMessage != null)
					{
						logger.LogWarning("Errored topic response for command {0}: {1}", parameters.CommandType, result.ErrorMessage);
					}

					return result;
				}
				catch
				{
					logger.LogWarning("Invalid topic response: {0}", topicReturn);
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				logger.LogWarning("Send command exception:{0}{1}", Environment.NewLine, e.Message);
			}

			return null;
		}

		/// <inheritdoc />
		public Task<bool> SetPort(ushort port, CancellationToken cancellationToken)
		{
			CheckDisposed();

			if (port == 0)
				throw new ArgumentOutOfRangeException(nameof(port), port, "port must not be zero!");

			async Task<bool> ImmediateTopicPortChange()
			{
				var commandResult = await SendCommand(
					new TopicParameters(port, false),
					cancellationToken)
					.ConfigureAwait(false);

				if (commandResult.ErrorMessage != null)
					return false;

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
			var result = await SendCommand(
				new TopicParameters(newRebootState),
				cancellationToken)
				.ConfigureAwait(false);

			return result != null && result.ErrorMessage != null;
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
		public void Suspend() => process.Suspend();

		/// <inheritdoc />
		public void Resume() => process.Resume();

		/// <inheritdoc />
		public void ReplaceDmbProvider(IDmbProvider dmbProvider)
		{
			var oldDmb = reattachInformation.Dmb;
			reattachInformation.Dmb = dmbProvider ?? throw new ArgumentNullException(nameof(dmbProvider));
			oldDmb.Dispose();
		}

		/// <inheritdoc />
		public Task InstanceRenamed(string newInstanceName, CancellationToken cancellationToken)
			=> SendCommand(new TopicParameters(newInstanceName), cancellationToken);

		/// <inheritdoc />
		public Task UpdateChannels(IEnumerable<ChannelRepresentation> newChannels, CancellationToken cancellationToken)
			=> SendCommand(
				new TopicParameters(
					new ChatUpdate(newChannels)),
				cancellationToken);
	}
}
