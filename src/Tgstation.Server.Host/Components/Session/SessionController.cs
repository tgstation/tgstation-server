using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Newtonsoft.Json;

using Serilog.Context;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Components.Interop.Topic;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Session
{
	/// <inheritdoc cref="ISessionController" />
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
		public EngineVersion EngineVersion => ReattachInformation.Dmb.EngineVersion;

		/// <inheritdoc />
		public RebootState RebootState => ReattachInformation.RebootState;

		/// <inheritdoc />
		public Version? DMApiVersion { get; private set; }

		/// <inheritdoc />
		public bool TerminationWasIntentional => terminationWasIntentional || (Lifetime.IsCompleted && Lifetime.Result == 0);

		/// <inheritdoc />
		public Task<LaunchResult> LaunchResult { get; }

		/// <inheritdoc />
		public Task<int?> Lifetime { get; }

		/// <inheritdoc />
		public Task OnStartup => startupTcs.Task;

		/// <inheritdoc />
		public Task OnReboot => rebootTcs.Task;

		/// <inheritdoc />
		public Task RebootGate
		{
			get => rebootGate;
			set
			{
				var tcs = new TaskCompletionSource<Task>();
				async Task Wrap()
				{
					var toAwait = await tcs.Task;
					await toAwait;
					await value;
				}

				tcs.SetResult(Interlocked.Exchange(ref rebootGate, Wrap()));
			}
		}

		/// <inheritdoc />
		public Task OnPrime => primeTcs.Task;

		/// <inheritdoc />
		public bool DMApiAvailable => ReattachInformation.Dmb.CompileJob.DMApiVersion?.Major == DMApiConstants.InteropVersion.Major;

		/// <inheritdoc />
		public bool ProcessingRebootBridgeRequest => rebootBridgeRequestsProcessing > 0;

		/// <inheritdoc />
		public string DumpFileExtension => engineLock.UseDotnetDump
			? ".net.dmp"
			: ".dmp";

		/// <summary>
		/// The up to date <see cref="Session.ReattachInformation"/>.
		/// </summary>
		public ReattachInformation ReattachInformation { get; }

		/// <summary>
		/// The <see cref="FifoSemaphore"/> used to prevent concurrent calls into /world/Topic().
		/// </summary>
		public FifoSemaphore TopicSendSemaphore { get; }

		/// <inheritdoc />
		public long? MemoryUsage => process.MemoryUsage;

		/// <inheritdoc />
		public DateTimeOffset? LaunchTime => process.LaunchTime;

		/// <summary>
		/// The <see cref="Byond.TopicSender.ITopicClient"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly Byond.TopicSender.ITopicClient byondTopicSender;

		/// <summary>
		/// The <see cref="IBridgeRegistration"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IBridgeRegistration? bridgeRegistration;

		/// <summary>
		/// The <see cref="IProcess"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IProcess process;

		/// <summary>
		/// The <see cref="IEngineExecutableLock"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IEngineExecutableLock engineLock;

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
		/// The <see cref="IDotnetDumpService"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IDotnetDumpService dotnetDumpService;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="SessionController"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> that completes when DD makes it's first bridge request.
		/// </summary>
		readonly TaskCompletionSource initialBridgeRequestTcs;

		/// <summary>
		/// The <see cref="Instance"/> metadata.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// A <see cref="CancellationTokenSource"/> used for tasks that should not exceed the lifetime of the session.
		/// </summary>
		readonly CancellationTokenSource sessionDurationCts;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for port updates and <see cref="disposed"/>.
		/// </summary>
		readonly object synchronizationLock;

		/// <summary>
		/// If this session is meant to validate the presence of the DMAPI.
		/// </summary>
		readonly bool apiValidationSession;

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
		/// Backing field for <see cref="RebootGate"/>.
		/// </summary>
		volatile Task rebootGate;

		/// <summary>
		/// The <see cref="Task"/> representing calls to <see cref="TriggerCustomEvent(CustomEventInvocation?)"/>.
		/// </summary>
		volatile Task customEventProcessingTask;

		/// <summary>
		/// <see cref="Task"/> for shutting down the server if it is taking too long after validation.
		/// </summary>
		volatile Task? postValidationShutdownTask;

		/// <summary>
		/// The number of currently active calls to <see cref="ProcessBridgeRequest(BridgeParameters, CancellationToken)"/> from TgsReboot().
		/// </summary>
		volatile uint rebootBridgeRequestsProcessing;

		/// <summary>
		/// The <see cref="ApiValidationStatus"/> for the <see cref="SessionController"/>.
		/// </summary>
		ApiValidationStatus apiValidationStatus;

		/// <summary>
		/// If the <see cref="SessionController"/> has been disposed.
		/// </summary>
		bool disposed;

		/// <summary>
		/// If <see cref="process"/> should be kept alive instead.
		/// </summary>
		bool released;

		/// <summary>
		/// Backing field for overriding <see cref="TerminationWasIntentional"/>.
		/// </summary>
		bool terminationWasIntentional;

		/// <summary>
		/// Initializes a new instance of the <see cref="SessionController"/> class.
		/// </summary>
		/// <param name="reattachInformation">The value of <see cref="ReattachInformation"/>.</param>
		/// <param name="metadata">The owning <see cref="Instance"/>.</param>
		/// <param name="process">The value of <see cref="process"/>.</param>
		/// <param name="engineLock">The value of <see cref="engineLock"/>.</param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/>.</param>
		/// <param name="bridgeRegistrar">The <see cref="IBridgeRegistrar"/> used to populate <see cref="bridgeRegistration"/>.</param>
		/// <param name="chat">The value of <see cref="chat"/>.</param>
		/// <param name="chatTrackingContext">The value of <see cref="chatTrackingContext"/>.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> for the <see cref="SessionController"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="dotnetDumpService">The value of <see cref="dotnetDumpService"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="logger">The value of <see cref="Chunker.Logger"/>.</param>
		/// <param name="postLifetimeCallback">The <see cref="Func{TResult}"/> returning a <see cref="ValueTask"/> to be run after the <paramref name="process"/> ends.</param>
		/// <param name="startupTimeout">The optional time to wait before failing the <see cref="LaunchResult"/>.</param>
		/// <param name="reattached">If this is a reattached session.</param>
		/// <param name="apiValidate">The value of <see cref="apiValidationSession"/>.</param>
		public SessionController(
			ReattachInformation reattachInformation,
			Api.Models.Instance metadata,
			IProcess process,
			IEngineExecutableLock engineLock,
			Byond.TopicSender.ITopicClient byondTopicSender,
			IChatTrackingContext chatTrackingContext,
			IBridgeRegistrar bridgeRegistrar,
			IChatManager chat,
			IAssemblyInformationProvider assemblyInformationProvider,
			IAsyncDelayer asyncDelayer,
			IDotnetDumpService dotnetDumpService,
			IEventConsumer eventConsumer,
			ILogger<SessionController> logger,
			Func<ValueTask> postLifetimeCallback,
			uint? startupTimeout,
			bool reattached,
			bool apiValidate)
			: base(logger)
		{
			ReattachInformation = reattachInformation ?? throw new ArgumentNullException(nameof(reattachInformation));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			this.process = process ?? throw new ArgumentNullException(nameof(process));
			this.engineLock = engineLock ?? throw new ArgumentNullException(nameof(engineLock));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.chatTrackingContext = chatTrackingContext ?? throw new ArgumentNullException(nameof(chatTrackingContext));
			ArgumentNullException.ThrowIfNull(bridgeRegistrar);

			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);

			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.dotnetDumpService = dotnetDumpService ?? throw new ArgumentNullException(nameof(dotnetDumpService));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));

			apiValidationSession = apiValidate;

			disposed = false;
			apiValidationStatus = ApiValidationStatus.NeverValidated;
			released = false;

			startupTcs = new TaskCompletionSource();
			rebootTcs = new TaskCompletionSource();
			primeTcs = new TaskCompletionSource();

			rebootGate = Task.CompletedTask;
			customEventProcessingTask = Task.CompletedTask;

			// Run this asynchronously because we want to try to avoid any effects sending topics to the server while the initial bridge request is processing
			// It MAY be the source of a DD crash. See this gist https://gist.github.com/Cyberboss/7776bbeff3a957d76affe0eae95c9f14
			// Worth further investigation as to if that sequence of events is a reliable crash vector and opening a BYOND bug if it is
			initialBridgeRequestTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			sessionDurationCts = new CancellationTokenSource();

			TopicSendSemaphore = new FifoSemaphore();
			synchronizationLock = new object();

			if (apiValidationSession || DMApiAvailable)
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

			async Task<int?> WrapLifetime()
			{
				var exitCode = await process.Lifetime;
				await postLifetimeCallback();
				if (postValidationShutdownTask != null)
					await postValidationShutdownTask;

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

			sessionDurationCts.Cancel();
			var cancellationToken = CancellationToken.None; // DCT: None available
			var semaphoreLockTask = TopicSendSemaphore.Lock(cancellationToken);

			if (!released)
			{
				await engineLock.StopServerProcess(
					Logger,
					process,
					ReattachInformation.AccessIdentifier,
					ReattachInformation.Port,
					cancellationToken);
			}

			await process.DisposeAsync();
			engineLock.Dispose();
			bridgeRegistration?.Dispose();
			var regularDmbDisposeTask = ReattachInformation.Dmb.DisposeAsync();
			var initialDmb = ReattachInformation.InitialDmb;
			if (initialDmb != null)
				await initialDmb.DisposeAsync();

			await regularDmbDisposeTask;

			chatTrackingContext.Dispose();
			sessionDurationCts.Dispose();

			if (!released)
				await Lifetime; // finish the async callback

			(await semaphoreLockTask).Dispose();
			TopicSendSemaphore.Dispose();

			await customEventProcessingTask;
		}

		/// <inheritdoc />
		public async ValueTask<BridgeResponse?> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken)
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
		public ValueTask Release()
		{
			CheckDisposed();

			ReattachInformation.Dmb.KeepAlive();
			ReattachInformation.InitialDmb?.KeepAlive();
			engineLock.DoNotDeleteThisSession();
			released = true;
			return DisposeAsync();
		}

		/// <inheritdoc />
		public ValueTask<TopicResponse?> SendCommand(TopicParameters parameters, CancellationToken cancellationToken)
			=> SendCommand(parameters, false, cancellationToken);

		/// <inheritdoc />
		public async ValueTask<bool> SetRebootState(RebootState newRebootState, CancellationToken cancellationToken)
		{
			if (RebootState == newRebootState)
				return true;

			Logger.LogTrace("Changing reboot state to {newRebootState}", newRebootState);

			ReattachInformation.RebootState = newRebootState;
			var result = await SendCommand(
				new TopicParameters(newRebootState),
				cancellationToken);

			return result != null && result.ErrorMessage == null;
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
		public void SuspendProcess() => process.SuspendProcess();

		/// <inheritdoc />
		public void ResumeProcess() => process.ResumeProcess();

		/// <inheritdoc />
		public IAsyncDisposable ReplaceDmbProvider(IDmbProvider dmbProvider)
		{
			var oldDmb = ReattachInformation.Dmb;
			ReattachInformation.Dmb = dmbProvider ?? throw new ArgumentNullException(nameof(dmbProvider));
			return oldDmb;
		}

		/// <inheritdoc />
		public async ValueTask InstanceRenamed(string newInstanceName, CancellationToken cancellationToken)
		{
			var runtimeInformation = ReattachInformation.RuntimeInformation;
			if (runtimeInformation != null)
				runtimeInformation.InstanceName = newInstanceName;

			await SendCommand(
				TopicParameters.CreateInstanceRenamedTopicParameters(newInstanceName),
				cancellationToken);
		}

		/// <inheritdoc />
		public async ValueTask UpdateChannels(IEnumerable<ChannelRepresentation> newChannels, CancellationToken cancellationToken)
			=> await SendCommand(
				new TopicParameters(
					new ChatUpdate(newChannels)),
				cancellationToken);

		/// <inheritdoc />
		public ValueTask CreateDump(string outputFile, bool minidump, CancellationToken cancellationToken)
		{
			if (engineLock.UseDotnetDump)
				return dotnetDumpService.Dump(process, outputFile, minidump, cancellationToken);

			return process.CreateDump(outputFile, minidump, cancellationToken);
		}

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
						ReattachInformation.RuntimeInformation!.ServerPort),
					true,
					sessionDurationCts.Token);

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
							CompileJob.DMApiVersion!.Semver());
				}
			}

			return result;
		}

		/// <summary>
		/// Throws an <see cref="ObjectDisposedException"/> if <see cref="DisposeAsync"/> has been called.
		/// </summary>
		void CheckDisposed() => ObjectDisposedException.ThrowIf(disposed, this);

		/// <summary>
		/// Terminates the server after ten seconds if it does not exit.
		/// </summary>
		/// <param name="proceedTask">A <see cref="Task{TResult}"/> that this method <see langword="await"/>s before executing. If the <see cref="Task{TResult}.Result"/> is <see langword="false"/>, this method will return immediately.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task PostValidationShutdown(Task<bool> proceedTask)
		{
			Logger.LogTrace("Entered post validation terminate task.");
			if (!await proceedTask)
			{
				Logger.LogTrace("Not running post validation terminate task for repeated bridge request.");
				return;
			}

			const int GracePeriodSeconds = 30;
			Logger.LogDebug("Server will terminated in {gracePeriodSeconds}s if it does not exit...", GracePeriodSeconds);
			var delayTask = asyncDelayer.Delay(TimeSpan.FromSeconds(GracePeriodSeconds), CancellationToken.None); // DCT: None available
			await Task.WhenAny(process.Lifetime, delayTask);

			if (!process.Lifetime.IsCompleted)
			{
				Logger.LogWarning("DMAPI took too long to shutdown server after validation request!");
				process.Terminate();
				apiValidationStatus = ApiValidationStatus.BadValidationRequest;
			}
			else
				Logger.LogTrace("Server exited properly post validation.");
		}

		/// <summary>
		/// Handle a set of bridge <paramref name="parameters"/>.
		/// </summary>
		/// <param name="parameters">The <see cref="BridgeParameters"/> to handle.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="BridgeResponse"/> for the request or <see langword="null"/> if the request could not be dispatched.</returns>
#pragma warning disable CA1502 // TODO: Decomplexify
		async ValueTask<BridgeResponse?> ProcessBridgeCommand(BridgeParameters parameters, CancellationToken cancellationToken)
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
					chatTrackingContext.Active = false;
					terminationWasIntentional = true;
					process.Terminate();
					break;
				case BridgeCommandType.DeprecatedPortUpdate:
					return BridgeError("Port switching is no longer supported!");
				case BridgeCommandType.Startup:
					apiValidationStatus = ApiValidationStatus.BadValidationRequest;

					if (apiValidationSession)
					{
						var proceedTcs = new TaskCompletionSource<bool>();
						var firstValidationRequest = Interlocked.CompareExchange(ref postValidationShutdownTask, PostValidationShutdown(proceedTcs.Task), null) == null;
						proceedTcs.SetResult(firstValidationRequest);

						if (!firstValidationRequest)
							return BridgeError("Startup bridge request was repeated!");
					}

					if (parameters.Version == null)
						return BridgeError("Missing dmApiVersion field!");

					DMApiVersion = parameters.Version;

					// TODO: When OD figures out how to unite port and topic_port, set an upper version bound on OD for this check
					if (DMApiVersion.Major != DMApiConstants.InteropVersion.Major
						|| (EngineVersion.Engine == EngineType.OpenDream && DMApiVersion < new Version(5, 7)))
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

					// we create new runtime info here because of potential .Dmb changes (i think. i forget...)
					response.RuntimeInformation = new RuntimeInformation(
						chatTrackingContext,
						ReattachInformation.Dmb,
						ReattachInformation.RuntimeInformation!.ServerVersion,
						ReattachInformation.RuntimeInformation.InstanceName,
						ReattachInformation.RuntimeInformation.SecurityLevel,
						ReattachInformation.RuntimeInformation.Visibility,
						ReattachInformation.RuntimeInformation.ServerPort,
						ReattachInformation.RuntimeInformation.ApiValidateOnly);

					if (parameters.TopicPort.HasValue)
					{
						var newTopicPort = parameters.TopicPort.Value;
						Logger.LogInformation("Server is requesting use of port {topicPort} for topic communications", newTopicPort);
						ReattachInformation.TopicPort = newTopicPort;
					}

					// Load custom commands
					chatTrackingContext.CustomCommands = parameters.CustomCommands ?? Array.Empty<CustomCommand>();
					chatTrackingContext.Active = true;
					Interlocked.Exchange(ref startupTcs, new TaskCompletionSource()).SetResult();
					break;
				case BridgeCommandType.Reboot:
					Interlocked.Increment(ref rebootBridgeRequestsProcessing);
					try
					{
						chatTrackingContext.Active = false;
						Interlocked.Exchange(ref rebootTcs, new TaskCompletionSource()).SetResult();
						await RebootGate.WaitAsync(cancellationToken);
					}
					finally
					{
						Interlocked.Decrement(ref rebootBridgeRequestsProcessing);
					}

					break;
				case BridgeCommandType.Chunk:
					return await ProcessChunk<BridgeParameters, BridgeResponse>(ProcessBridgeCommand, BridgeError, parameters.Chunk, cancellationToken);
				case BridgeCommandType.Event:
					return TriggerCustomEvent(parameters.EventInvocation);
				case null:
					return BridgeError("Missing commandType!");
				default:
					return BridgeError($"commandType {parameters.CommandType} not supported!");
			}

			return response;
		}
#pragma warning restore CA1502

		/// <summary>
		/// Log and return a <see cref="BridgeResponse"/> for a given <paramref name="message"/>.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <returns>A new errored <see cref="BridgeResponse"/>.</returns>
		BridgeResponse BridgeError(string message)
		{
			Logger.LogWarning("Bridge request error: {message}", message);
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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="CombinedTopicResponse"/> of the topic request.</returns>
		async ValueTask<CombinedTopicResponse?> SendTopicRequest(TopicParameters parameters, CancellationToken cancellationToken)
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
			parameters.AccessIdentifier = null!;
			GenerateQueryString(parameters, out json);

			// yes, this straight up ignores unicode, precalculating it is useless when we don't
			// even know if the UTF8 bytes of the url encoded chunk will fit the window until we do said encoding
			var fullPayloadSize = (uint)json.Length;

			List<string>? chunkQueryStrings = null;
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

			CombinedTopicResponse? combinedResponse = null;
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

			while ((combinedResponse?.InteropResponse?.MissingChunks?.Count ?? 0) > 0)
			{
				Logger.LogWarning("DD is still missing some chunks of topic request P{payloadId}! Sending missing chunks...", payloadId);
				var missingChunks = combinedResponse!.InteropResponse!.MissingChunks!;
				var lastIndex = missingChunks.Last();
				foreach (var missingChunkIndex in missingChunks)
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
		/// Generates a <see cref="Byond.TopicSender.ITopicClient"/> query string for a given set of <paramref name="parameters"/>.
		/// </summary>
		/// <param name="parameters">The <see cref="TopicParameters"/> to serialize.</param>
		/// <param name="json">The intermediate JSON <see cref="string"/> prior to URL encoding.</param>
		/// <returns>The <see cref="Byond.TopicSender.ITopicClient"/> query string for the given <paramref name="parameters"/>.</returns>
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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="CombinedTopicResponse"/> of the topic request.</returns>
		async ValueTask<CombinedTopicResponse?> SendRawTopic(string queryString, bool priority, CancellationToken cancellationToken)
		{
			if (disposed)
			{
				Logger.LogWarning(
					"Attempted to send a topic on a disposed SessionController");
				return null;
			}

			var targetPort = ReattachInformation.TopicPort ?? ReattachInformation.Port;
			Byond.TopicSender.TopicResponse? byondResponse;
			using (await TopicSendSemaphore.Lock(cancellationToken))
				byondResponse = await byondTopicSender.SendWithOptionalPriority(
					asyncDelayer,
					LogTopicRequests
						? Logger
						: NullLogger.Instance,
					queryString,
					targetPort,
					priority,
					cancellationToken);

			if (byondResponse == null)
			{
				if (priority)
					Logger.LogError(
						"Unable to send priority topic \"{queryString}\"!",
						queryString);

				return null;
			}

			var topicReturn = byondResponse.StringData;

			TopicResponse? interopResponse = null;
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

		/// <summary>
		/// Sends a command to DreamDaemon through /world/Topic().
		/// </summary>
		/// <param name="parameters">The <see cref="TopicParameters"/> to send.</param>
		/// <param name="bypassLaunchResult">If waiting for the <see cref="LaunchResult"/> should be bypassed.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="TopicResponse"/> of /world/Topic().</returns>
		async ValueTask<TopicResponse?> SendCommand(TopicParameters parameters, bool bypassLaunchResult, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(parameters);

			if (Lifetime.IsCompleted || disposed)
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

			var reboot = OnReboot;
			if (!bypassLaunchResult)
			{
				var launchResult = await LaunchResult.WaitAsync(cancellationToken);
				if (launchResult.ExitCode.HasValue)
				{
					Logger.LogDebug("Not sending topic request {commandType} to server that failed to launch!", parameters.CommandType);
					return null;
				}
			}

			// meh, this is kind of a hack, but it works
			if (!chatTrackingContext.Active)
			{
				Logger.LogDebug("Not sending topic request {commandType} to server that is rebooting/starting.", parameters.CommandType);
				return null;
			}

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var combinedCancellationToken = cts.Token;
			async ValueTask CancelIfLifetimeElapses()
			{
				try
				{
					var completed = await Task.WhenAny(Lifetime, reboot).WaitAsync(combinedCancellationToken);

					Logger.LogDebug(
						"Server {action}, cancelling pending command: {commandType}",
						completed != reboot
							? "process ended"
							: "rebooting",
						parameters.CommandType);
					cts.Cancel();
				}
				catch (OperationCanceledException)
				{
					// expected, not even worth tracing
				}
				catch (Exception ex)
				{
					Logger.LogError(ex, "Error in CancelIfLifetimeElapses!");
				}
			}

			TopicResponse? fullResponse = null;
			var lifetimeWatchingTask = CancelIfLifetimeElapses();
			try
			{
				var combinedResponse = await SendTopicRequest(parameters, combinedCancellationToken);

				void LogCombinedResponse()
				{
					if (LogTopicRequests && combinedResponse != null)
						Logger.LogTrace("Topic response: {topicString}", combinedResponse.ByondTopicResponse.StringData ?? "(NO STRING DATA)");
				}

				LogCombinedResponse();

				if (combinedResponse?.InteropResponse?.Chunk != null)
				{
					Logger.LogTrace("Topic response is chunked...");

					ChunkData? nextChunk = combinedResponse.InteropResponse.Chunk;
					do
					{
						var nextRequest = await ProcessChunk<TopicResponse, ChunkedTopicParameters>(
							(completedResponse, _) =>
							{
								fullResponse = completedResponse;
								return ValueTask.FromResult<ChunkedTopicParameters?>(null);
							},
							error =>
							{
								Logger.LogWarning("Topic response chunking error: {message}", error);
								return null;
							},
							combinedResponse?.InteropResponse?.Chunk,
							combinedCancellationToken);

						if (nextRequest != null)
						{
							nextRequest.PayloadId = nextChunk.PayloadId;
							combinedResponse = await SendTopicRequest(nextRequest, combinedCancellationToken);
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
					combinedCancellationToken.IsCancellationRequested
						? cancellationToken.IsCancellationRequested
							? "cancelled"
							: "aborted"
						: "timed out");

				// throw only if the original token was the trigger
				cancellationToken.ThrowIfCancellationRequested();
			}
			finally
			{
				cts.Cancel();
				await lifetimeWatchingTask;
			}

			if (fullResponse?.ErrorMessage != null)
				Logger.LogWarning(
					"Errored topic response for command {commandType}: {errorMessage}",
					parameters.CommandType,
					fullResponse.ErrorMessage);

			return fullResponse;
		}

		/// <summary>
		/// Trigger a custom event from a given <paramref name="invocation"/>.
		/// </summary>
		/// <param name="invocation">The <see cref="CustomEventInvocation"/>.</param>
		/// <returns>An appropriate <see cref="BridgeResponse"/>.</returns>
		BridgeResponse TriggerCustomEvent(CustomEventInvocation? invocation)
		{
			if (invocation == null)
				return BridgeError("Missing eventInvocation!");

			var eventName = invocation.EventName;
			if (eventName == null)
				return BridgeError("Missing eventName!");

			var notifyCompletion = invocation.NotifyCompletion;
			if (!notifyCompletion.HasValue)
				return BridgeError("Missing notifyCompletion!");

			var eventParams = new List<string>
			{
				ReattachInformation.Dmb.Directory,
			};

			eventParams.AddRange(invocation
				.Parameters?
				.Where(param => param != null)
				.Cast<string>()
				?? Enumerable.Empty<string>());

			var eventId = Guid.NewGuid();
			Logger.LogInformation("Triggering custom event \"{eventName}\": {eventId}", eventName, eventId);

			var cancellationToken = sessionDurationCts.Token;
			ValueTask? eventTask = eventConsumer.HandleCustomEvent(eventName, eventParams, cancellationToken);

			async Task ProcessEvent()
			{
				try
				{
					await eventTask.Value;

					if (notifyCompletion.Value)
						await SendCommand(
							new TopicParameters(eventId),
							cancellationToken);
					else
						Logger.LogTrace("Finished custom event {eventId}, not sending notification.", eventId);
				}
				catch (OperationCanceledException ex)
				{
					Logger.LogDebug(ex, "Custom event invocation {eventId} aborted!", eventId);
				}
				catch (Exception ex)
				{
					Logger.LogWarning(ex, "Custom event invocation {eventId} errored!", eventId);
				}
			}

			if (!eventTask.HasValue)
				return BridgeError("Event refused to execute due to matching a TGS event!");

			lock (sessionDurationCts)
			{
				var previousEventProcessingTask = customEventProcessingTask;
				var eventProcessingTask = ProcessEvent();
				customEventProcessingTask = Task.WhenAll(customEventProcessingTask, eventProcessingTask);
			}

			return new BridgeResponse
			{
				EventId = notifyCompletion.Value
					? eventId.ToString()
					: null,
			};
		}
	}
}
