using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Serilog.Context;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Components.Interop.Topic;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Session
{
	/// <inheritdoc />
	sealed class SessionController : Chunker, ISessionController, IBridgeHandler, IChannelSink
	{
		/// <summary>
		/// If calls to <see cref="SendTopicRequest(TopicParameters, CancellationToken)"/> should be trace logged.
		/// </summary>
		internal static bool LogTopicRequests { get; set; } = true;

		/// <inheritdoc />
		public DMApiParameters DMApiParameters => ReattachInformation;

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
		public Models.CompileJob CompileJob => ReattachInformation.Dmb.CompileJob;

		/// <inheritdoc />
		public RebootState RebootState => ReattachInformation.RebootState;

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
		public Task OnStartup => startupTcs.Task;

		/// <inheritdoc />
		public Task OnReboot => rebootTcs.Task;

		/// <inheritdoc />
		public Task OnPrime => primeTcs.Task;

		/// <inheritdoc />
		public bool DMApiAvailable => ReattachInformation.Dmb.CompileJob.DMApiVersion?.Major == DMApiConstants.InteropVersion.Major;

		/// <inheritdoc />
		public bool ProcessingRebootBridgeRequest => rebootBridgeRequestsProcessing > 0;

		/// <summary>
		/// The up to date <see cref="Session.ReattachInformation"/>.
		/// </summary>
		public ReattachInformation ReattachInformation { get; }

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> that completes when DD makes it's first bridge request.
		/// </summary>
		readonly TaskCompletionSource initialBridgeRequestTcs;

		/// <summary>
		/// The <see cref="Instance"/> metadata.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// A <see cref="CancellationTokenSource"/> used for the topic send operation made on reattaching.
		/// </summary>
		readonly CancellationTokenSource reattachTopicCts;

		/// <summary>
		/// The <see cref="global::Byond.TopicSender.ITopicClient"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly global::Byond.TopicSender.ITopicClient byondTopicSender;

		/// <summary>
		/// The <see cref="IBridgeRegistration"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IBridgeRegistration bridgeRegistration;

		/// <summary>
		/// The <see cref="IProcess"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IProcess process;

		/// <summary>
		/// The <see cref="IByondExecutableLock"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IByondExecutableLock byondLock;

		/// <summary>
		/// The <see cref="IChatTrackingContext"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IChatTrackingContext chatTrackingContext;

		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IChatManager chat;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for port updates and <see cref="disposed"/>.
		/// </summary>
		readonly object synchronizationLock;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> <see cref="SetPort(ushort, CancellationToken)"/> waits on when DreamDaemon currently has it's ports closed.
		/// </summary>
		TaskCompletionSource<bool> portAssignmentTcs;

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> that completes when DD sends a valid startup bridge request.
		/// </summary>
		volatile TaskCompletionSource startupTcs;

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> that completes when DD tells us about a reboot.
		/// </summary>
		volatile TaskCompletionSource rebootTcs;

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> that completes when DD tells us it's primed.
		/// </summary>
		volatile TaskCompletionSource primeTcs;

		/// <summary>
		/// The number of currently active calls to <see cref="ProcessBridgeRequest(BridgeParameters, CancellationToken)"/> from TgsReboot().
		/// </summary>
		volatile uint rebootBridgeRequestsProcessing;

		/// <summary>
		/// The port to assign DreamDaemon when it queries for it.
		/// </summary>
		ushort? nextPort;

		/// <summary>
		/// The <see cref="ApiValidationStatus"/> for the <see cref="SessionController"/>.
		/// </summary>
		ApiValidationStatus apiValidationStatus;

		/// <summary>
		/// If we know DreamDaemon currently has it's port closed.
		/// </summary>
		bool portClosedForReboot;

		/// <summary>
		/// If the <see cref="SessionController"/> has been disposed.
		/// </summary>
		bool disposed;

		/// <summary>
		/// If <see cref="process"/> should be kept alive instead.
		/// </summary>
		bool released;

		/// <summary>
		/// Initializes a new instance of the <see cref="SessionController"/> class.
		/// </summary>
		/// <param name="reattachInformation">The value of <see cref="ReattachInformation"/>.</param>
		/// <param name="metadata">The owning <see cref="Instance"/>.</param>
		/// <param name="process">The value of <see cref="process"/>.</param>
		/// <param name="byondLock">The value of <see cref="byondLock"/>.</param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/>.</param>
		/// <param name="bridgeRegistrar">The <see cref="IBridgeRegistrar"/> used to populate <see cref="bridgeRegistration"/>.</param>
		/// <param name="chat">The value of <see cref="chat"/>.</param>
		/// <param name="chatTrackingContext">The value of <see cref="chatTrackingContext"/>.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> for the <see cref="SessionController"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/> for the <see cref="SessionController"/>.</param>
		/// <param name="logger">The value of <see cref="Chunker.Logger"/>.</param>
		/// <param name="postLifetimeCallback">The <see cref="Func{TResult}"/> returning a <see cref="Task"/> to be run after the <paramref name="process"/> ends.</param>
		/// <param name="startupTimeout">The optional time to wait before failing the <see cref="LaunchResult"/>.</param>
		/// <param name="reattached">If this is a reattached session.</param>
		/// <param name="apiValidate">If this is a DMAPI validation session.</param>
		public SessionController(
			ReattachInformation reattachInformation,
			Api.Models.Instance metadata,
			IProcess process,
			IByondExecutableLock byondLock,
			global::Byond.TopicSender.ITopicClient byondTopicSender,
			IChatTrackingContext chatTrackingContext,
			IBridgeRegistrar bridgeRegistrar,
			IChatManager chat,
			IAssemblyInformationProvider assemblyInformationProvider,
			IAsyncDelayer asyncDelayer,
			ILogger<SessionController> logger,
			Func<Task> postLifetimeCallback,
			uint? startupTimeout,
			bool reattached,
			bool apiValidate)
			: base(logger)
		{
			ReattachInformation = reattachInformation ?? throw new ArgumentNullException(nameof(reattachInformation));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			this.process = process ?? throw new ArgumentNullException(nameof(process));
			this.byondLock = byondLock ?? throw new ArgumentNullException(nameof(byondLock));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.chatTrackingContext = chatTrackingContext ?? throw new ArgumentNullException(nameof(chatTrackingContext));
			ArgumentNullException.ThrowIfNull(bridgeRegistrar);

			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);

			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));

			portClosedForReboot = false;
			disposed = false;
			apiValidationStatus = ApiValidationStatus.NeverValidated;
			released = false;

			startupTcs = new TaskCompletionSource();
			rebootTcs = new TaskCompletionSource();
			primeTcs = new TaskCompletionSource();

			// Run this asynchronously because we want to try to avoid any effects sending topics to the server while the initial bridge request is processing
			// It MAY be the source of a DD crash. See this gist https://gist.github.com/Cyberboss/7776bbeff3a957d76affe0eae95c9f14
			// Worth further investigation as to if that sequence of events is a reliable crash vector and opening a BYOND bug if it is
			initialBridgeRequestTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			reattachTopicCts = new CancellationTokenSource();
			synchronizationLock = new object();

			if (apiValidate || DMApiAvailable)
			{
				bridgeRegistration = bridgeRegistrar.RegisterHandler(this);
				this.chatTrackingContext.SetChannelSink(this);
			}
			else
				logger.LogTrace(
					"Not registering session with {reasonWhyDmApiIsBad} DMAPI version for interop!",
					reattachInformation.Dmb.CompileJob.DMApiVersion == null
						? "no"
						: $"incompatible ({reattachInformation.Dmb.CompileJob.DMApiVersion})");

			async Task<int> WrapLifetime()
			{
				var exitCode = await process.Lifetime;
				await postLifetimeCallback();
				return exitCode;
			}

			Lifetime = WrapLifetime();

			LaunchResult = GetLaunchResult(
				assemblyInformationProvider,
				asyncDelayer,
				startupTimeout,
				reattached,
				apiValidate);

			logger.LogDebug(
				"Created session controller. CommsKey: {accessIdentifier}, Port: {port}",
				reattachInformation.AccessIdentifier,
				reattachInformation.Port);
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			lock (synchronizationLock)
			{
				if (disposed)
					return;
				disposed = true;
			}

			Logger.LogTrace("Disposing...");
			if (!released)
			{
				process.Terminate();
				await process.Lifetime;
			}

			await process.DisposeAsync();
			byondLock.Dispose();
			bridgeRegistration?.Dispose();
			ReattachInformation.Dmb.Dispose();
			ReattachInformation.InitialDmb?.Dispose();
			chatTrackingContext.Dispose();
			reattachTopicCts.Dispose();

			if (!released)
				await Lifetime; // finish the async callback
		}

		/// <inheritdoc />
		public async Task<BridgeResponse> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(parameters);

			using (LogContext.PushProperty(SerilogContextHelper.InstanceIdContextProperty, metadata.Id))
			{
				Logger.LogTrace("Handling bridge request...");

				try
				{
					return await ProcessBridgeCommand(parameters, cancellationToken);
				}
				finally
				{
					initialBridgeRequestTcs.TrySetResult();
				}
			}
		}

		/// <inheritdoc />
		public void EnableCustomChatCommands() => chatTrackingContext.Active = DMApiAvailable;

		/// <inheritdoc />
		public async Task Release()
		{
			CheckDisposed();

			ReattachInformation.Dmb.KeepAlive();
			ReattachInformation.InitialDmb?.KeepAlive();
			byondLock.DoNotDeleteThisSession();
			released = true;
			await DisposeAsync();
		}

		/// <inheritdoc />
		public async Task<TopicResponse> SendCommand(TopicParameters parameters, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(parameters);

			if (Lifetime.IsCompleted)
			{
				Logger.LogWarning(
					"Attempted to send a command to an inactive SessionController: {commandType}",
					parameters.CommandType);
				return null;
			}

			if (!DMApiAvailable)
			{
				Logger.LogTrace("Not sending topic request {commandType} to server without/with incompatible DMAPI!", parameters.CommandType);
				return null;
			}

			TopicResponse fullResponse = null;
			try
			{
				var combinedResponse = await SendTopicRequest(parameters, cancellationToken);

				void LogCombinedResponse()
				{
					if (LogTopicRequests && combinedResponse != null)
						Logger.LogTrace("Topic response: {topicString}", combinedResponse.ByondTopicResponse.StringData ?? "(NO STRING DATA)");
				}

				LogCombinedResponse();

				if (combinedResponse?.InteropResponse?.Chunk != null)
				{
					Logger.LogTrace("Topic response is chunked...");

					ChunkData nextChunk = combinedResponse.InteropResponse.Chunk;
					do
					{
						var nextRequest = await ProcessChunk<TopicResponse, ChunkedTopicParameters>(
							(completedResponse, cancellationToken) =>
							{
								fullResponse = completedResponse;
								return Task.FromResult<ChunkedTopicParameters>(null);
							},
							error =>
							{
								Logger.LogWarning("Topic response chunking error: {message}", error);
								return null;
							},
							combinedResponse?.InteropResponse?.Chunk,
							cancellationToken);

						if (nextRequest != null)
						{
							nextRequest.PayloadId = nextChunk.PayloadId;
							combinedResponse = await SendTopicRequest(nextRequest, cancellationToken);
							LogCombinedResponse();
							nextChunk = combinedResponse?.InteropResponse?.Chunk;
						}
						else
							nextChunk = null;
					}
					while (nextChunk != null);
				}
				else
					fullResponse = combinedResponse?.InteropResponse;
			}
			catch (OperationCanceledException ex)
			{
				Logger.LogDebug(
					ex,
					"Topic request {cancellationType}!",
					cancellationToken.IsCancellationRequested
						? "aborted"
						: "timed out");
				cancellationToken.ThrowIfCancellationRequested();
			}

			if (fullResponse?.ErrorMessage != null)
				Logger.LogWarning(
					"Errored topic response for command {commandType}: {errorMessage}",
					parameters.CommandType,
					fullResponse.ErrorMessage);

			return fullResponse;
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
					cancellationToken);

				if (commandResult?.ErrorMessage != null)
					return false;

				ReattachInformation.Port = port;
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

			Logger.LogTrace("Changing reboot state to {newRebootState}", newRebootState);

			ReattachInformation.RebootState = newRebootState;
			var result = await SendCommand(
				new TopicParameters(newRebootState),
				cancellationToken);

			return result?.ErrorMessage == null;
		}

		/// <inheritdoc />
		public void ResetRebootState()
		{
			CheckDisposed();
			Logger.LogTrace("Resetting reboot state...");
			ReattachInformation.RebootState = RebootState.Normal;
		}

		/// <inheritdoc />
		public void AdjustPriority(bool higher) => process.AdjustPriority(higher);

		/// <inheritdoc />
		public void Suspend() => process.Suspend();

		/// <inheritdoc />
		public void Resume() => process.Resume();

		/// <inheritdoc />
		public IDisposable ReplaceDmbProvider(IDmbProvider dmbProvider)
		{
			var oldDmb = ReattachInformation.Dmb;
			ReattachInformation.Dmb = dmbProvider ?? throw new ArgumentNullException(nameof(dmbProvider));
			return oldDmb;
		}

		/// <inheritdoc />
		public Task InstanceRenamed(string newInstanceName, CancellationToken cancellationToken)
		{
			ReattachInformation.RuntimeInformation.InstanceName = newInstanceName;
			return SendCommand(new TopicParameters(newInstanceName), cancellationToken);
		}

		/// <inheritdoc />
		public Task UpdateChannels(IEnumerable<ChannelRepresentation> newChannels, CancellationToken cancellationToken)
			=> SendCommand(
				new TopicParameters(
					new ChatUpdate(newChannels)),
				cancellationToken);

		/// <inheritdoc />
		public Task CreateDump(string outputFile, CancellationToken cancellationToken) => process.CreateDump(outputFile, cancellationToken);

		/// <summary>
		/// The <see cref="Task{TResult}"/> for <see cref="LaunchResult"/>.
		/// </summary>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/>.</param>
		/// <param name="asyncDelayer">The <see cref="IAsyncDelayer"/>.</param>
		/// <param name="startupTimeout">The, optional, startup timeout in seconds.</param>
		/// <param name="reattached">If DreamDaemon was reattached.</param>
		/// <param name="apiValidate">If this is a DMAPI validation session.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Session.LaunchResult"/> for the operation.</returns>
		async Task<LaunchResult> GetLaunchResult(
			IAssemblyInformationProvider assemblyInformationProvider,
			IAsyncDelayer asyncDelayer,
			uint? startupTimeout,
			bool reattached,
			bool apiValidate)
		{
			var startTime = DateTimeOffset.UtcNow;
			var useBridgeRequestForLaunchResult = !reattached && (apiValidate || DMApiAvailable);
			var startupTask = useBridgeRequestForLaunchResult
				? initialBridgeRequestTcs.Task
				: process.Startup;
			var toAwait = Task.WhenAny(startupTask, process.Lifetime);

			if (startupTimeout.HasValue)
				toAwait = Task.WhenAny(
					toAwait,
					asyncDelayer.Delay(
						TimeSpan.FromSeconds(startupTimeout.Value),
						CancellationToken.None)); // DCT: None available, task will clean up after delay

			Logger.LogTrace(
				"Waiting for LaunchResult based on {launchResultCompletionCause}{possibleTimeout}...",
				useBridgeRequestForLaunchResult ? "initial bridge request" : "process startup",
				startupTimeout.HasValue ? $" with a timeout of {startupTimeout.Value}s" : String.Empty);

			await toAwait;

			var result = new LaunchResult
			{
				ExitCode = process.Lifetime.IsCompleted ? await process.Lifetime : null,
				StartupTime = startupTask.IsCompleted ? (DateTimeOffset.UtcNow - startTime) : null,
			};

			Logger.LogTrace("Launch result: {launchResult}", result);

			if (!result.ExitCode.HasValue && reattached && !disposed)
			{
				var reattachResponse = await SendCommand(
					new TopicParameters(
						assemblyInformationProvider.Version,
						ReattachInformation.RuntimeInformation.ServerPort),
					reattachTopicCts.Token);

				if (reattachResponse != null)
				{
					if (reattachResponse?.CustomCommands != null)
						chatTrackingContext.CustomCommands = reattachResponse.CustomCommands;
					else if (reattachResponse != null)
						Logger.Log(
							CompileJob.DMApiVersion >= new Version(5, 2, 0)
								? LogLevel.Warning
								: LogLevel.Debug,
							"DMAPI Interop v{interopVersion} isn't returning the TGS custom commands list. Functionality added in v5.2.0.",
							CompileJob.DMApiVersion.Semver());
				}
			}

			return result;
		}

		/// <summary>
		/// Throws an <see cref="ObjectDisposedException"/> if <see cref="DisposeAsync"/> has been called.
		/// </summary>
		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(SessionController));
		}

		/// <summary>
		/// Handle a set of bridge <paramref name="parameters"/>.
		/// </summary>
		/// <param name="parameters">The <see cref="BridgeParameters"/> to handle.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="BridgeResponse"/> for the request or <see langword="null"/> if the request could not be dispatched.</returns>
		async Task<BridgeResponse> ProcessBridgeCommand(BridgeParameters parameters, CancellationToken cancellationToken)
		{
			var response = new BridgeResponse();
			switch (parameters.CommandType)
			{
				case BridgeCommandType.ChatSend:
					if (parameters.ChatMessage == null)
						return BridgeError("Missing chatMessage field!");

					if (parameters.ChatMessage.ChannelIds == null)
						return BridgeError("Missing channelIds field in chatMessage!");

					if (parameters.ChatMessage.ChannelIds.Any(channelIdString => !UInt64.TryParse(channelIdString, out var _)))
						return BridgeError("Invalid channelIds in chatMessage!");

					if (parameters.ChatMessage.Text == null)
						return BridgeError("Missing message field in chatMessage!");

					var anyFailed = false;
					var parsedChannels = parameters.ChatMessage.ChannelIds.Select(
						channelString =>
						{
							anyFailed |= !UInt64.TryParse(channelString, out var channelId);
							return channelId;
						});

					if (anyFailed)
						return BridgeError("Failed to parse channelIds as U64!");

					chat.QueueMessage(
						parameters.ChatMessage,
						parsedChannels);
					break;
				case BridgeCommandType.Prime:
					Interlocked.Exchange(ref primeTcs, new TaskCompletionSource()).SetResult();
					break;
				case BridgeCommandType.Kill:
					Logger.LogInformation("Bridge requested process termination!");
					TerminationWasRequested = true;
					process.Terminate();
					break;
				case BridgeCommandType.PortUpdate:
					lock (synchronizationLock)
					{
						if (!parameters.CurrentPort.HasValue)
						{
							/////UHHHH
							Logger.LogWarning("DreamDaemon sent new port command without providing it's own!");
							return BridgeError("Missing stringified port as data parameter!");
						}

						var currentPort = parameters.CurrentPort.Value;
						if (!nextPort.HasValue)
							ReattachInformation.Port = parameters.CurrentPort.Value; // not ready yet, so what we'll do is accept the random port DD opened on for now and change it later when we decide to
						else
						{
							// nextPort is ready, tell DD to switch to that
							// if it fails it'll kill itself
							response.NewPort = nextPort.Value;
							ReattachInformation.Port = nextPort.Value;
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
						return BridgeError("Missing dmApiVersion field!");

					DMApiVersion = parameters.Version;
					if (DMApiVersion.Major != DMApiConstants.InteropVersion.Major)
					{
						apiValidationStatus = ApiValidationStatus.Incompatible;
						return BridgeError("Incompatible dmApiVersion!");
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
							return BridgeError("Missing minimumSecurityLevel field!");
						default:
							return BridgeError("Invalid minimumSecurityLevel!");
					}

					Logger.LogTrace("ApiValidationStatus set to {apiValidationStatus}", apiValidationStatus);

					response.RuntimeInformation = new RuntimeInformation(
						chatTrackingContext,
						ReattachInformation.Dmb,
						ReattachInformation.RuntimeInformation.ServerVersion,
						ReattachInformation.RuntimeInformation.InstanceName,
						ReattachInformation.RuntimeInformation.SecurityLevel,
						ReattachInformation.RuntimeInformation.Visibility,
						ReattachInformation.RuntimeInformation.ServerPort,
						ReattachInformation.RuntimeInformation.ApiValidateOnly);

					// Load custom commands
					chatTrackingContext.CustomCommands = parameters.CustomCommands;
					Interlocked.Exchange(ref startupTcs, new TaskCompletionSource()).SetResult();
					break;
				case BridgeCommandType.Reboot:
					Interlocked.Increment(ref rebootBridgeRequestsProcessing);
					try
					{
						if (ClosePortOnReboot)
						{
							chatTrackingContext.Active = false;
							response.NewPort = 0;
							portClosedForReboot = true;
						}

						Interlocked.Exchange(ref rebootTcs, new TaskCompletionSource()).SetResult();
					}
					finally
					{
						Interlocked.Decrement(ref rebootBridgeRequestsProcessing);
					}

					break;
				case BridgeCommandType.Chunk:
					return await ProcessChunk<BridgeParameters, BridgeResponse>(ProcessBridgeCommand, BridgeError, parameters.Chunk, cancellationToken);
				case null:
					return BridgeError("Missing commandType!");
				default:
					return BridgeError($"commandType {parameters.CommandType} not supported!");
			}

			return response;
		}

		/// <summary>
		/// Log and return a <see cref="BridgeResponse"/> for a given <paramref name="message"/>.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <returns>A new errored <see cref="BridgeResponse"/>.</returns>
		BridgeResponse BridgeError(string message)
		{
			Logger.LogWarning("Bridge request chunking error: {message}", message);
			return new BridgeResponse
			{
				ErrorMessage = message,
			};
		}

		/// <summary>
		/// Send a topic request for given <paramref name="parameters"/> to DreamDaemon, chunking it if necessary.
		/// </summary>
		/// <param name="parameters">The <see cref="TopicParameters"/> to send.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="CombinedTopicResponse"/> of the topic request.</returns>
		async Task<CombinedTopicResponse> SendTopicRequest(TopicParameters parameters, CancellationToken cancellationToken)
		{
			parameters.AccessIdentifier = ReattachInformation.AccessIdentifier;

			var fullCommandString = GenerateQueryString(parameters, out var json);
			if (LogTopicRequests)
				Logger.LogTrace("Topic request: {json}", json);
			var fullCommandByteCount = Encoding.UTF8.GetByteCount(fullCommandString);
			var topicPriority = parameters.IsPriority;
			if (fullCommandByteCount <= DMApiConstants.MaximumTopicRequestLength)
				return await SendRawTopic(fullCommandString, topicPriority, cancellationToken);

			var interopChunkingVersion = new Version(5, 6, 0);
			if (ReattachInformation.Dmb.CompileJob.DMApiVersion < interopChunkingVersion)
			{
				Logger.LogWarning(
					"Cannot send topic request as it is exceeds the single request limit of {limitBytes}B ({actualBytes}B) and requires chunking and the current compile job's interop version must be at least {chunkingVersionRequired}!",
					DMApiConstants.MaximumTopicRequestLength,
					fullCommandByteCount,
					interopChunkingVersion);
				return null;
			}

			var payloadId = NextPayloadId;

			// AccessIdentifer is just noise in a chunked request
			parameters.AccessIdentifier = null;
			GenerateQueryString(parameters, out json);

			// yes, this straight up ignores unicode, precalculating it is useless when we don't
			// even know if the UTF8 bytes of the url encoded chunk will fit the window until we do said encoding
			var fullPayloadSize = (uint)json.Length;

			List<string> chunkQueryStrings = null;
			for (var chunkCount = 2; chunkQueryStrings == null; ++chunkCount)
			{
				var standardChunkSize = fullPayloadSize / chunkCount;
				var bigChunkSize = standardChunkSize + (fullPayloadSize % chunkCount);
				if (bigChunkSize > DMApiConstants.MaximumTopicRequestLength)
					continue;

				chunkQueryStrings = new List<string>();
				for (var i = 0U; i < chunkCount; ++i)
				{
					var startIndex = i * standardChunkSize;
					var subStringLength = Math.Min(
						fullPayloadSize - startIndex,
						i == chunkCount - 1
							? bigChunkSize
							: standardChunkSize);
					var chunkPayload = json.Substring((int)startIndex, (int)subStringLength);

					var chunk = new ChunkData
					{
						Payload = chunkPayload,
						PayloadId = payloadId,
						SequenceId = i,
						TotalChunks = (uint)chunkCount,
					};

					var chunkParameters = new TopicParameters(chunk)
					{
						AccessIdentifier = ReattachInformation.AccessIdentifier,
					};

					var chunkCommandString = GenerateQueryString(chunkParameters, out _);
					if (Encoding.UTF8.GetByteCount(chunkCommandString) > DMApiConstants.MaximumTopicRequestLength)
					{
						// too long when encoded, need more chunks
						chunkQueryStrings = null;
						break;
					}

					chunkQueryStrings.Add(chunkCommandString);
				}
			}

			Logger.LogTrace("Chunking topic request ({totalChunks} total)...", chunkQueryStrings.Count);

			CombinedTopicResponse combinedResponse = null;
			bool LogRequestIssue(bool possiblyFromCompletedRequest)
			{
				if (combinedResponse?.InteropResponse == null || combinedResponse.InteropResponse.ErrorMessage != null)
				{
					Logger.LogWarning(
						"Topic request {chunkingStatus} failed!{potentialRequestError}",
						possiblyFromCompletedRequest ? "final chunk" : "chunking",
						combinedResponse?.InteropResponse?.ErrorMessage != null
							? $" Request error: {combinedResponse.InteropResponse.ErrorMessage}"
							: String.Empty);
					return true;
				}

				return false;
			}

			foreach (var chunkCommandString in chunkQueryStrings)
			{
				combinedResponse = await SendRawTopic(chunkCommandString, topicPriority, cancellationToken);
				if (LogRequestIssue(chunkCommandString == chunkQueryStrings.Last()))
					return null;
			}

			while ((combinedResponse.InteropResponse.MissingChunks?.Count ?? 0) > 0)
			{
				Logger.LogWarning("DD is still missing some chunks of topic request P{payloadId}! Sending missing chunks...", payloadId);
				var lastIndex = combinedResponse.InteropResponse.MissingChunks.Last();
				foreach (var missingChunkIndex in combinedResponse.InteropResponse.MissingChunks)
				{
					var chunkCommandString = chunkQueryStrings[(int)missingChunkIndex];
					combinedResponse = await SendRawTopic(chunkCommandString, topicPriority, cancellationToken);
					if (LogRequestIssue(missingChunkIndex == lastIndex))
						return null;
				}
			}

			return combinedResponse;
		}

		/// <summary>
		/// Generates a <see cref="global::Byond.TopicSender.ITopicClient"/> query string for a given set of <paramref name="parameters"/>.
		/// </summary>
		/// <param name="parameters">The <see cref="TopicParameters"/> to serialize.</param>
		/// <param name="json">The intermediate JSON <see cref="string"/> prior to URL encoding.</param>
		/// <returns>The <see cref="global::Byond.TopicSender.ITopicClient"/> query string for the given <paramref name="parameters"/>.</returns>
		string GenerateQueryString(TopicParameters parameters, out string json)
		{
			json = JsonConvert.SerializeObject(parameters, DMApiConstants.SerializerSettings);
			var commandString = String.Format(
				CultureInfo.InvariantCulture,
				"?{0}={1}",
				byondTopicSender.SanitizeString(DMApiConstants.TopicData),
				byondTopicSender.SanitizeString(json));
			return commandString;
		}

		/// <summary>
		/// Send a given <paramref name="queryString"/> to DreamDaemon's /world/Topic.
		/// </summary>
		/// <param name="queryString">The sanitized topic query string to send.</param>
		/// <param name="priority">If this is a priority message. If so, the topic will make 5 attempts to send unless BYOND reboots or exits.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="CombinedTopicResponse"/> of the topic request.</returns>
		async Task<CombinedTopicResponse> SendRawTopic(string queryString, bool priority, CancellationToken cancellationToken)
		{
			var targetPort = ReattachInformation.Port;
			var killedOrRebootedTask = Task.WhenAny(Lifetime, OnReboot);
			global::Byond.TopicSender.TopicResponse byondResponse = null;
			var firstSend = true;

			const int PrioritySendAttempts = 5;
			for (var i = PrioritySendAttempts - 1; i >= 0 && (priority || firstSend); --i)
				try
				{
					firstSend = false;
					if (!killedOrRebootedTask.IsCompleted)
						byondResponse = await byondTopicSender.SendTopic(
							new IPEndPoint(IPAddress.Loopback, targetPort),
							queryString,
							cancellationToken);

					break;
				}
				catch (Exception ex)
				{
					Logger.LogWarning(ex, "SendTopic exception!{retryDetails}", priority ? $" {i} attempts remaining." : String.Empty);

					if (priority && i > 0)
					{
						var delayTask = asyncDelayer.Delay(TimeSpan.FromSeconds(2), cancellationToken);
						await Task.WhenAny(killedOrRebootedTask, delayTask);
					}
				}

			if (byondResponse == null)
			{
				if (priority)
					if (killedOrRebootedTask.IsCompleted)
						Logger.LogWarning(
							"Unable to send priority topic \"{queryString}\" DreamDaemon {stateClearAction}!",
							queryString,
							Lifetime.IsCompleted ? "process ended" : "rebooted");
					else
						Logger.LogError(
							"Unable to send priority topic \"{queryString}\"!",
							queryString);

				return null;
			}

			var topicReturn = byondResponse.StringData;

			TopicResponse interopResponse = null;
			if (topicReturn != null)
				try
				{
					interopResponse = JsonConvert.DeserializeObject<TopicResponse>(topicReturn, DMApiConstants.SerializerSettings);
				}
				catch (Exception ex)
				{
					Logger.LogWarning(ex, "Invalid interop response: {topicReturnString}", topicReturn);
				}

			return new CombinedTopicResponse(byondResponse, interopResponse);
		}
	}
}
