using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Prometheus;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Session
{
	/// <inheritdoc />
	sealed class SessionControllerFactory : ISessionControllerFactory
	{
		/// <summary>
		/// Path in Diagnostics folder to DreamDaemon logs.
		/// </summary>
		const string DreamDaemonLogsPath = "DreamDaemonLogs";

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IEngineManager"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IEngineManager engineManager;

		/// <summary>
		/// The <see cref="ITopicClientFactory"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly ITopicClientFactory topicClientFactory;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IIOManager"/> for the Game directory.
		/// </summary>
		readonly IIOManager gameIOManager;

		/// <summary>
		/// The <see cref="IIOManager"/> for the Diagnostics directory.
		/// </summary>
		readonly IIOManager diagnosticsIOManager;

		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IChatManager chat;

		/// <summary>
		/// The <see cref="INetworkPromptReaper"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly INetworkPromptReaper networkPromptReaper;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IBridgeRegistrar"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IBridgeRegistrar bridgeRegistrar;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IDotnetDumpService"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IDotnetDumpService dotnetDumpService;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly ILogger<SessionControllerFactory> logger;

		/// <summary>
		/// The <see cref="SessionConfiguration"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly SessionConfiguration sessionConfiguration;

		/// <summary>
		/// The number of sessions launched.
		/// </summary>
		readonly Counter sessionsLaunched;

		/// <summary>
		/// The time the current session was launched.
		/// </summary>
		readonly Gauge lastSessionLaunch;

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly Api.Models.Instance instance;

		/// <summary>
		/// Check if a given <paramref name="port"/> can be bound to.
		/// </summary>
		/// <param name="port">The port number to test.</param>
		/// <param name="engineType">The <see cref="EngineType"/> we're bind testing for.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask PortBindTest(ushort port, EngineType engineType, CancellationToken cancellationToken)
		{
			logger.LogTrace("Bind test: {port}", port);
			try
			{
				// GIVE ME THE FUCKING PORT BACK WINDOWS!!!!
				const int MaxAttempts = 5;
				for (var i = 0; i < MaxAttempts; ++i)
					try
					{
						SocketExtensions.BindTest(platformIdentifier, new IPEndPoint(IPAddress.Any, port), engineType == EngineType.OpenDream);
						if (i > 0)
							logger.LogDebug("Clearing the socket took {iterations} attempts :/", i + 1);

						break;
					}
					catch (SocketException ex) when (platformIdentifier.IsWindows && ex.SocketErrorCode == SocketError.AddressAlreadyInUse && i < (MaxAttempts - 1))
					{
						await asyncDelayer.Delay(TimeSpan.FromSeconds(1), cancellationToken);
					}
			}
			catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
			{
				throw new JobException(ErrorCode.GameServerPortInUse, ex);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SessionControllerFactory"/> class.
		/// </summary>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		/// <param name="engineManager">The value of <see cref="engineManager"/>.</param>
		/// <param name="topicClientFactory">The value of <see cref="topicClientFactory"/>.</param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="instance">The value of <see cref="instance"/>.</param>
		/// <param name="gameIOManager">The value of <see cref="gameIOManager"/>.</param>
		/// <param name="diagnosticsIOManager">The value of <see cref="diagnosticsIOManager"/>.</param>
		/// <param name="chat">The value of <see cref="chat"/>.</param>
		/// <param name="networkPromptReaper">The value of <see cref="networkPromptReaper"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="bridgeRegistrar">The value of <see cref="bridgeRegistrar"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="dotnetDumpService">The value of <see cref="dotnetDumpService"/>.</param>
		/// <param name="metricFactory">The <see cref="IMetricFactory"/> used to create metrics.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="sessionConfiguration">The value of <see cref="sessionConfiguration"/>.</param>
		public SessionControllerFactory(
			IProcessExecutor processExecutor,
			IEngineManager engineManager,
			ITopicClientFactory topicClientFactory,
			ICryptographySuite cryptographySuite,
			IAssemblyInformationProvider assemblyInformationProvider,
			IIOManager gameIOManager,
			IIOManager diagnosticsIOManager,
			IChatManager chat,
			INetworkPromptReaper networkPromptReaper,
			IPlatformIdentifier platformIdentifier,
			IBridgeRegistrar bridgeRegistrar,
			IEventConsumer eventConsumer,
			IAsyncDelayer asyncDelayer,
			IDotnetDumpService dotnetDumpService,
			IMetricFactory metricFactory,
			ILoggerFactory loggerFactory,
			ILogger<SessionControllerFactory> logger,
			SessionConfiguration sessionConfiguration,
			Api.Models.Instance instance)
		{
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.engineManager = engineManager ?? throw new ArgumentNullException(nameof(engineManager));
			this.topicClientFactory = topicClientFactory ?? throw new ArgumentNullException(nameof(topicClientFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.gameIOManager = gameIOManager ?? throw new ArgumentNullException(nameof(gameIOManager));
			this.diagnosticsIOManager = diagnosticsIOManager ?? throw new ArgumentNullException(nameof(diagnosticsIOManager));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.networkPromptReaper = networkPromptReaper ?? throw new ArgumentNullException(nameof(networkPromptReaper));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.bridgeRegistrar = bridgeRegistrar ?? throw new ArgumentNullException(nameof(bridgeRegistrar));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.dotnetDumpService = dotnetDumpService ?? throw new ArgumentNullException(nameof(dotnetDumpService));
			ArgumentNullException.ThrowIfNull(metricFactory);
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.sessionConfiguration = sessionConfiguration ?? throw new ArgumentNullException(nameof(sessionConfiguration));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));

			sessionsLaunched = metricFactory.CreateCounter("tgs_sessions_launched", "The number of game server processes created");
			lastSessionLaunch = metricFactory.CreateGauge("tgs_session_start_time", "The UTC unix timestamp the most recent session was started");
		}

		/// <inheritdoc />
		#pragma warning disable CA1506 // TODO: Decomplexify
		public async ValueTask<ISessionController> LaunchNew(
			IDmbProvider dmbProvider,
			IEngineExecutableLock? currentByondLock,
			DreamDaemonLaunchParameters launchParameters,
			bool apiValidate,
			CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin session launch...");
			if (!launchParameters.Port.HasValue)
				throw new InvalidOperationException("Given port is null!");

			switch (dmbProvider.CompileJob.MinimumSecurityLevel)
			{
				case DreamDaemonSecurity.Ultrasafe:
					break;
				case DreamDaemonSecurity.Safe:
					if (launchParameters.SecurityLevel == DreamDaemonSecurity.Ultrasafe)
					{
						logger.LogTrace("Boosting security level to minimum of Safe");
						launchParameters.SecurityLevel = DreamDaemonSecurity.Safe;
					}

					break;
				case DreamDaemonSecurity.Trusted:
					if (launchParameters.SecurityLevel != DreamDaemonSecurity.Trusted)
						logger.LogTrace("Boosting security level to minimum of Trusted");

					launchParameters.SecurityLevel = DreamDaemonSecurity.Trusted;
					break;
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid DreamDaemonSecurity value: {0}", dmbProvider.CompileJob.MinimumSecurityLevel));
			}

			// get the byond lock
			var engineLock = currentByondLock ?? await engineManager.UseExecutables(
				dmbProvider.EngineVersion,
				gameIOManager.ConcatPath(dmbProvider.Directory, dmbProvider.DmbName),
				cancellationToken);
			try
			{
				logger.LogDebug(
					"Launching session with CompileJob {compileJobId}...",
					dmbProvider.CompileJob.Id);

				// mad this isn't abstracted but whatever
				var engineType = dmbProvider.EngineVersion.Engine!.Value;
				if (engineType == EngineType.Byond)
					await CheckPagerIsNotRunning();

				await PortBindTest(launchParameters.Port.Value, engineType, cancellationToken);

				string? outputFilePath = null;
				var preserveLogFile = true;

				var hasStandardOutput = engineLock.HasStandardOutput;
				if (launchParameters.LogOutput!.Value)
				{
					var now = DateTimeOffset.UtcNow;
					var dateDirectory = diagnosticsIOManager.ConcatPath(DreamDaemonLogsPath, now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
					await diagnosticsIOManager.CreateDirectory(dateDirectory, cancellationToken);
					outputFilePath = diagnosticsIOManager.ResolvePath(
						diagnosticsIOManager.ConcatPath(
							dateDirectory,
							$"server-utc-{now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture)}{(apiValidate ? "-dmapi" : String.Empty)}.log"));

					logger.LogInformation("Logging server output to {path}...", outputFilePath);
				}
				else if (!hasStandardOutput)
				{
					outputFilePath = gameIOManager.ConcatPath(dmbProvider.Directory, $"{Guid.NewGuid()}.server.log");
					preserveLogFile = false;
				}

				var accessIdentifier = cryptographySuite.GetSecureString();

				if (!apiValidate && dmbProvider.CompileJob.DMApiVersion == null)
					logger.LogDebug("Session will have no DMAPI support!");

				// launch dd
				var process = await CreateGameServerProcess(
					dmbProvider,
					engineLock,
					launchParameters,
					accessIdentifier,
					outputFilePath,
					apiValidate,
					cancellationToken);

				try
				{
					var chatTrackingContext = chat.CreateTrackingContext();

					try
					{
						var runtimeInformation = CreateRuntimeInformation(
							dmbProvider,
							chatTrackingContext,
							launchParameters.SecurityLevel!.Value,
							launchParameters.Visibility!.Value,
							apiValidate);

						var reattachInformation = new ReattachInformation(
							dmbProvider,
							process,
							runtimeInformation,
							accessIdentifier,
							launchParameters.Port.Value);

						var byondTopicSender = topicClientFactory.CreateTopicClient(
							TimeSpan.FromMilliseconds(
								launchParameters.TopicRequestTimeout!.Value));

						var sessionController = new SessionController(
							reattachInformation,
							instance,
							process,
							engineLock,
							byondTopicSender,
							chatTrackingContext,
							bridgeRegistrar,
							chat,
							assemblyInformationProvider,
							asyncDelayer,
							dotnetDumpService,
							eventConsumer,
							loggerFactory.CreateLogger<SessionController>(),
							() => LogDDOutput(
								process,
								outputFilePath,
								hasStandardOutput,
								preserveLogFile,
								CancellationToken.None), // DCT: None available
							launchParameters.StartupTimeout,
							false,
							apiValidate);

						if (!apiValidate)
						{
							sessionsLaunched.Inc();
							lastSessionLaunch.SetToCurrentTimeUtc();
						}

						return sessionController;
					}
					catch
					{
						chatTrackingContext.Dispose();
						throw;
					}
				}
				catch
				{
					await using (process)
					{
						process.Terminate();
						await process.Lifetime;
						throw;
					}
				}
			}
			catch
			{
				if (currentByondLock == null)
					engineLock.Dispose();
				throw;
			}
		}
#pragma warning restore CA1506

		/// <inheritdoc />
		public async ValueTask<ISessionController?> Reattach(
			ReattachInformation reattachInformation,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(reattachInformation);

			logger.LogTrace("Begin session reattach...");
			var byondTopicSender = topicClientFactory.CreateTopicClient(reattachInformation.TopicRequestTimeout);
			var engineLock = await engineManager.UseExecutables(
				reattachInformation.Dmb.EngineVersion,
				null, // Doesn't matter if it's trusted or not on reattach
				cancellationToken);

			try
			{
				logger.LogDebug(
					"Attaching to session PID: {pid}, CompileJob: {compileJobId}...",
					reattachInformation.ProcessId,
					reattachInformation.Dmb.CompileJob.Id);

				var process = processExecutor.GetProcess(reattachInformation.ProcessId);
				if (process == null)
					return null;

				try
				{
					if (engineLock.PromptsForNetworkAccess)
						networkPromptReaper.RegisterProcess(process);

					var chatTrackingContext = chat.CreateTrackingContext();
					try
					{
						var runtimeInformation = CreateRuntimeInformation(
							reattachInformation.Dmb,
							chatTrackingContext,
							reattachInformation.LaunchSecurityLevel,
							reattachInformation.LaunchVisibility,
							false);
						reattachInformation.SetRuntimeInformation(runtimeInformation);

						var controller = new SessionController(
							reattachInformation,
							instance,
							process,
							engineLock,
							byondTopicSender,
							chatTrackingContext,
							bridgeRegistrar,
							chat,
							assemblyInformationProvider,
							asyncDelayer,
							dotnetDumpService,
							eventConsumer,
							loggerFactory.CreateLogger<SessionController>(),
							() => ValueTask.CompletedTask,
							null,
							true,
							false);

						process = null;
						engineLock = null;
						chatTrackingContext = null;

						return controller;
					}
					catch
					{
						chatTrackingContext?.Dispose();
						throw;
					}
				}
				catch
				{
					if (process != null)
						await process.DisposeAsync();

					throw;
				}
			}
			catch
			{
				engineLock?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates the game server <see cref="IProcess"/>.
		/// </summary>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/>.</param>
		/// <param name="engineLock">The <see cref="IEngineExecutableLock"/>.</param>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/>.</param>
		/// <param name="accessIdentifier">The secure string to use for the session.</param>
		/// <param name="logFilePath">The optional full path to log DreamDaemon output to.</param>
		/// <param name="apiValidate">If we are only validating the DMAPI then exiting.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the DreamDaemon <see cref="IProcess"/>.</returns>
		async ValueTask<IProcess> CreateGameServerProcess(
			IDmbProvider dmbProvider,
			IEngineExecutableLock engineLock,
			DreamDaemonLaunchParameters launchParameters,
			string accessIdentifier,
			string? logFilePath,
			bool apiValidate,
			CancellationToken cancellationToken)
		{
			// important to run on all ports to allow port changing
			var environment = await engineLock.LoadEnv(logger, false, cancellationToken);
			var arguments = engineLock.FormatServerArguments(
				dmbProvider,
				new Dictionary<string, string>
				{
					{ DMApiConstants.ParamApiVersion, DMApiConstants.InteropVersion.Semver().ToString() },
					{ DMApiConstants.ParamServerPort, sessionConfiguration.BridgePort.ToString(CultureInfo.InvariantCulture) },
					{ DMApiConstants.ParamAccessIdentifier, accessIdentifier },
				},
				launchParameters,
				!engineLock.HasStandardOutput || engineLock.PreferFileLogging
					? logFilePath
					: null);

			// If this isnt a staging DD (From a Deployment), fire off events
			if (!apiValidate)
				await eventConsumer.HandleEvent(
					EventType.DreamDaemonPreLaunch,
					Enumerable.Empty<string?>(),
					false,
					cancellationToken);

			var process = await processExecutor.LaunchProcess(
				engineLock.ServerExePath,
				dmbProvider.Directory,
				arguments,
				cancellationToken,
				environment,
				logFilePath,
				engineLock.HasStandardOutput,
				true);

			try
			{
				if (!apiValidate)
				{
					if (sessionConfiguration.HighPriorityLiveDreamDaemon)
						process.AdjustPriority(true);
				}
				else if (sessionConfiguration.LowPriorityDeploymentProcesses)
					process.AdjustPriority(false);

				if (!engineLock.HasStandardOutput)
					networkPromptReaper.RegisterProcess(process);

				if (!apiValidate)
					await eventConsumer.HandleEvent(
						EventType.DreamDaemonLaunch,
						new List<string>
						{
							process.Id.ToString(CultureInfo.InvariantCulture),
						},
						false,
						cancellationToken);

				return process;
			}
			catch
			{
				await using (process)
				{
					process.Terminate();
					await process.Lifetime;
					throw;
				}
			}
		}

		/// <summary>
		/// Attempts to log DreamDaemon output.
		/// </summary>
		/// <param name="process">The DreamDaemon <see cref="IProcess"/>.</param>
		/// <param name="outputFilePath">The path to the DreamDaemon log file. Will be deleted if <paramref name="preserveFile"/> is <see langword="false"/>.</param>
		/// <param name="cliSupported">If DreamDaemon was launched with CLI capabilities.</param>
		/// <param name="preserveFile">If <see langword="false"/>, <paramref name="outputFilePath"/> will be deleted.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask LogDDOutput(IProcess process, string? outputFilePath, bool cliSupported, bool preserveFile, CancellationToken cancellationToken)
		{
			try
			{
				string? ddOutput = null;
				if (cliSupported)
					ddOutput = (await process.GetCombinedOutput(cancellationToken))!;

				if (ddOutput == null)
					try
					{
						var dreamDaemonLogBytes = await gameIOManager.ReadAllBytes(
							outputFilePath!,
							cancellationToken);

						ddOutput = Encoding.UTF8.GetString(dreamDaemonLogBytes.Span);
					}
					finally
					{
						if (!preserveFile)
							try
							{
								logger.LogTrace("Deleting temporary log file {path}...", outputFilePath);
								await gameIOManager.DeleteFile(outputFilePath!, cancellationToken);
							}
							catch (Exception ex)
							{
								// this is expected on OD at time of the support changes.
								// I've open a change to fix it: https://github.com/space-wizards/RobustToolbox/pull/4501
								logger.LogWarning(ex, "Failed to delete server log file {outputFilePath}!", outputFilePath);
							}
					}

				logger.LogTrace(
					"Server Output:{newLine}{output}",
					Environment.NewLine,
					ddOutput);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error reading server output!");
			}
		}

		/// <summary>
		/// Create <see cref="RuntimeInformation"/>.
		/// </summary>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/>.</param>
		/// <param name="chatTrackingContext">The <see cref="IChatTrackingContext"/>.</param>
		/// <param name="securityLevel">The <see cref="DreamDaemonSecurity"/> the server was launched with.</param>
		/// <param name="visibility">The <see cref="DreamDaemonVisibility"/> the server was launched with.</param>
		/// <param name="apiValidateOnly">The value of <see cref="RuntimeInformation.ApiValidateOnly"/>.</param>
		/// <returns>A new <see cref="RuntimeInformation"/> class.</returns>
		RuntimeInformation CreateRuntimeInformation(
			IDmbProvider dmbProvider,
			IChatTrackingContext chatTrackingContext,
			DreamDaemonSecurity securityLevel,
			DreamDaemonVisibility visibility,
			bool apiValidateOnly)
			=> new(
				chatTrackingContext,
				dmbProvider,
				assemblyInformationProvider.Version,
				instance.Name!,
				securityLevel,
				visibility,
				sessionConfiguration.BridgePort,
				apiValidateOnly);

		/// <summary>
		/// Make sure the BYOND pager is not running.
		/// </summary>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask CheckPagerIsNotRunning()
		{
			if (!platformIdentifier.IsWindows)
				return;

			await using var otherProcess = processExecutor.GetProcessByName("byond");
			if (otherProcess == null)
				return;

			var otherUsername = otherProcess.GetExecutingUsername();

			await using var ourProcess = processExecutor.GetCurrentProcess();
			var ourUsername = ourProcess.GetExecutingUsername();

			if (otherUsername.Equals(ourUsername, StringComparison.Ordinal))
				throw new JobException(ErrorCode.DreamDaemonPagerRunning);
		}
	}
}
