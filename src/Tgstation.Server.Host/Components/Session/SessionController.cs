using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Components.Interop.Topic;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Session
{
	/// <inheritdoc />
	sealed class SessionController : ISessionController, IBridgeHandler, IChannelSink
	{
		/// <inheritdoc />
		public DMApiParameters DMApiParameters => reattachInformation;

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
		public Task<int> Lifetime { get; }

		/// <inheritdoc />
		public Task OnReboot => rebootTcs.Task;

		/// <inheritdoc />
		public Task OnPrime => primeTcs.Task;

		/// <inheritdoc />
		public bool DMApiAvailable => reattachInformation.Dmb.CompileJob.DMApiVersion?.Major == DMApiConstants.Version.Major;

		/// <summary>
		/// The up to date <see cref="ReattachInformation"/>
		/// </summary>
		readonly ReattachInformation reattachInformation;

		/// <summary>
		/// The <see cref="Instance"/> metadata.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// A <see cref="CancellationTokenSource"/> used for the topic send operation made on reattaching.
		/// </summary>
		readonly CancellationTokenSource reattachTopicCts;

		/// <summary>
		/// The <see cref="ITopicClient"/> for the <see cref="SessionController"/>
		/// </summary>
		readonly ITopicClient byondTopicSender;

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
		/// <see langword="lock"/> <see cref="object"/> for port updates and <see cref="disposed"/>.
		/// </summary>
		readonly object synchronizationLock;

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
		/// The <see cref="TaskCompletionSource{TResult}"/> that completes when DD tells us it's primed.
		/// </summary>
		TaskCompletionSource<object> primeTcs;

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
		/// <param name="metadata">The owning <see cref="Instance"/>.</param>
		/// <param name="process">The value of <see cref="process"/></param>
		/// <param name="byondLock">The value of <see cref="byondLock"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="bridgeRegistrar">The <see cref="IBridgeRegistrar"/> used to populate <see cref="bridgeRegistration"/>.</param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="chatTrackingContext">The value of <see cref="chatTrackingContext"/></param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> for the <see cref="SessionController"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="postLifetimeCallback">The <see cref="Func{TResult}"/> returning a <see cref="Task"/> to be run after the <paramref name="process"/> ends.</param>
		/// <param name="startupTimeout">The optional time to wait before failing the <see cref="LaunchResult"/></param>
		/// <param name="reattached">If this is a reattached session.</param>
		/// <param name="apiValidate">If this is a DMAPI validation session.</param>
		public SessionController(
			ReattachInformation reattachInformation,
			Api.Models.Instance metadata,
			IProcess process,
			IByondExecutableLock byondLock,
			ITopicClient byondTopicSender,
			IChatTrackingContext chatTrackingContext,
			IBridgeRegistrar bridgeRegistrar,
			IChatManager chat,
			IAssemblyInformationProvider assemblyInformationProvider,
			ILogger<SessionController> logger,
			Func<Task> postLifetimeCallback,
			uint? startupTimeout,
			bool reattached,
			bool apiValidate)
		{
			this.reattachInformation = reattachInformation ?? throw new ArgumentNullException(nameof(reattachInformation));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			this.process = process ?? throw new ArgumentNullException(nameof(process));
			this.byondLock = byondLock ?? throw new ArgumentNullException(nameof(byondLock));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.chatTrackingContext = chatTrackingContext ?? throw new ArgumentNullException(nameof(chatTrackingContext));
			if (bridgeRegistrar == null)
				throw new ArgumentNullException(nameof(bridgeRegistrar));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			if (apiValidate || DMApiAvailable)
			{
				bridgeRegistration = bridgeRegistrar.RegisterHandler(this);
				this.chatTrackingContext.SetChannelSink(this);
			}
			else
				logger.LogTrace(
					"Not registering session with {0} DMAPI version for interop!",
					reattachInformation.Dmb.CompileJob.DMApiVersion == null
						? "no"
						: $"incompatible ({reattachInformation.Dmb.CompileJob.DMApiVersion})");

			portClosedForReboot = false;
			disposed = false;
			apiValidationStatus = ApiValidationStatus.NeverValidated;
			released = false;

			rebootTcs = new TaskCompletionSource<object>();
			primeTcs = new TaskCompletionSource<object>();
			reattachTopicCts = new CancellationTokenSource();
			synchronizationLock = new object();

			async Task<int> WrapLifetime()
			{
				var exitCode = await process.Lifetime.ConfigureAwait(false);
				await postLifetimeCallback().ConfigureAwait(false);
				return exitCode;
			}

			Lifetime = WrapLifetime();

			LaunchResult = GetLaunchResult(
				assemblyInformationProvider,
				startupTimeout,
				reattached);

			logger.LogDebug("Created session controller. CommsKey: {0}, Port: {1}", reattachInformation.AccessIdentifier, Port);
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			lock (synchronizationLock)
			{
				if (disposed)
					return;
				disposed = true;

				logger.LogTrace("Disposing...");
				if (!released)
				{
					process.Terminate();
					byondLock.Dispose();
				}

				process.Dispose();
				bridgeRegistration?.Dispose();
				reattachInformation.Dmb?.Dispose(); // will be null when released
				chatTrackingContext.Dispose();
				reattachTopicCts.Dispose();
			}

			if (!released)
			{
				// finish the async callback
				await Lifetime.ConfigureAwait(false);
			}
		}

		/// <summary>
		/// The <see cref="Task{TResult}"/> for <see cref="LaunchResult"/>.
		/// </summary>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/>.</param>
		/// <param name="startupTimeout">The, optional, startup timeout in seconds.</param>
		/// <param name="reattached">If DreamDaemon was reattached.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Session.LaunchResult"/> for the operation.</returns>
		async Task<LaunchResult> GetLaunchResult(
			IAssemblyInformationProvider assemblyInformationProvider,
			uint? startupTimeout,
			bool reattached)
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

			logger.LogTrace("Launch result: {0}", result);

			if (!result.ExitCode.HasValue && reattached && !disposed)
			{
				var reattachResponse = await SendCommand(
					new TopicParameters(
						assemblyInformationProvider.Version,
						reattachInformation.RuntimeInformation.ServerPort),
					reattachTopicCts.Token)
					.ConfigureAwait(false);

				if (reattachResponse != null)
				{
					if (reattachResponse.InteropResponse?.CustomCommands != null)
						chatTrackingContext.CustomCommands = reattachResponse.InteropResponse.CustomCommands;
					else if (reattachResponse.InteropResponse != null)
						logger.LogWarning(
							"DMAPI v{0} isn't returning the TGS custom commands list. Functionality added in v5.2.0.",
							Dmb.CompileJob.DMApiVersion.Semver());
				}
			}

			return result;
		}

		/// <inheritdoc />
		public async Task<BridgeResponse> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			using (LogContext.PushProperty("Instance", metadata.Id))
			{
				logger.LogTrace("Handling bridge request...");
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

						if (parameters.ChatMessage.ChannelIds.Any(channelIdString => !UInt64.TryParse(channelIdString, out var _)))
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
						var oldPrimeTcs = primeTcs;
						primeTcs = new TaskCompletionSource<object>();
						oldPrimeTcs.SetResult(null);
						break;
					case BridgeCommandType.Kill:
						logger.LogInformation("Bridge requested process termination!");
						TerminationWasRequested = true;
						process.Terminate();
						break;
					case BridgeCommandType.PortUpdate:
						lock (synchronizationLock)
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
						if (DMApiVersion.Major != DMApiConstants.Version.Major)
						{
							apiValidationStatus = ApiValidationStatus.Incompatible;
							return new BridgeResponse
							{
								ErrorMessage = "Incompatible dmApiVersion!"
							};
						}

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

						// Load custom commands
						chatTrackingContext.CustomCommands = parameters.CustomCommands;
						break;
					case BridgeCommandType.Reboot:
						if (ClosePortOnReboot)
						{
							chatTrackingContext.Active = false;
							response.NewPort = 0;
							portClosedForReboot = true;
						}

						var oldRebootTcs = rebootTcs;
						rebootTcs = new TaskCompletionSource<object>();
						oldRebootTcs.SetResult(null);
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
		}

		/// <summary>
		/// Throws an <see cref="ObjectDisposedException"/> if <see cref="DisposeAsync"/> has been called
		/// </summary>
		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(SessionController));
		}

		/// <inheritdoc />
		public void EnableCustomChatCommands() => chatTrackingContext.Active = DMApiAvailable;

		/// <inheritdoc />
		public async Task<ReattachInformation> Release()
		{
			CheckDisposed();

			// we still don't want to dispose the dmb yet, even though we're keeping it alive
			var tmpProvider = reattachInformation.Dmb;
			reattachInformation.Dmb = null;
			released = true;
			await DisposeAsync().ConfigureAwait(false);
			byondLock.DoNotDeleteThisSession();
			tmpProvider.KeepAlive();
			reattachInformation.Dmb = tmpProvider;
			return reattachInformation;
		}

		/// <inheritdoc />
		public async Task<CombinedTopicResponse> SendCommand(TopicParameters parameters, CancellationToken cancellationToken)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			if (Lifetime.IsCompleted)
			{
				logger.LogWarning(
					"Attempted to send a command to an inactive SessionController: {0}",
					parameters.CommandType);
				return null;
			}

			if (!DMApiAvailable)
			{
				logger.LogTrace("Not sending topic request {0} to server without/with incompatible DMAPI!", parameters.CommandType);
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

				var topicResponse = await byondTopicSender.SendTopic(
					new IPEndPoint(IPAddress.Loopback, targetPort),
					commandString,
					cancellationToken).ConfigureAwait(false);

				var topicReturn = topicResponse.StringData;

				Interop.Topic.TopicResponse interopResponse = null;
				if (topicReturn != null)
					try
					{
						interopResponse = JsonConvert.DeserializeObject<Interop.Topic.TopicResponse>(topicReturn, DMApiConstants.SerializerSettings);
						if (interopResponse.ErrorMessage != null)
						{
							logger.LogWarning("Errored topic response for command {0}: {1}", parameters.CommandType, interopResponse.ErrorMessage);
						}

						logger.LogTrace("Interop response: {0}", topicReturn);
					}
					catch
					{
						logger.LogWarning("Invalid interop response: {0}", topicReturn);
					}

				return new CombinedTopicResponse(topicResponse, interopResponse);
			}
			catch (OperationCanceledException)
			{
				logger.LogTrace(
					"Topic request {0}!",
					cancellationToken.IsCancellationRequested
						? "aborted"
						: "timed out");
				cancellationToken.ThrowIfCancellationRequested();
			}
			catch (Exception e)
			{
				logger.LogWarning("Send command exception:{0}{1}", Environment.NewLine, e);
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
					new TopicParameters(port),
					cancellationToken)
					.ConfigureAwait(false);

				if (commandResult.InteropResponse?.ErrorMessage != null)
					return false;

				reattachInformation.Port = port;
				return true;
			}

			lock (synchronizationLock)
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

			logger.LogTrace("Changing reboot state to {0}", newRebootState);

			reattachInformation.RebootState = newRebootState;
			var result = await SendCommand(
				new TopicParameters(newRebootState),
				cancellationToken)
				.ConfigureAwait(false);

			return result?.InteropResponse != null && result.InteropResponse?.ErrorMessage == null;
		}

		/// <inheritdoc />
		public void ResetRebootState()
		{
			CheckDisposed();
			logger.LogTrace("Resetting reboot state...");
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

		/// <inheritdoc />
		public Task CreateDump(string outputFile, CancellationToken cancellationToken) => process.CreateDump(outputFile, cancellationToken);
	}
}
