using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Helpers.Extensions;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Session
{
	/// <inheritdoc />
	sealed class SessionControllerFactory : ISessionControllerFactory
	{
		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IByondManager"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IByondManager byond;

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
		/// The <see cref="IIOManager"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IIOManager ioManager;

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
		/// The <see cref="IServerPortProvider"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IServerPortProvider serverPortProvider;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly ILogger<SessionControllerFactory> logger;

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="SessionControllerFactory"/>.
		/// </summary>
		readonly Api.Models.Instance instance;

		/// <summary>
		/// Change a given <paramref name="securityLevel"/> into the appropriate DreamDaemon command line word.
		/// </summary>
		/// <param name="securityLevel">The <see cref="DreamDaemonSecurity"/> level to change.</param>
		/// <returns>A <see cref="string"/> representation of the command line parameter.</returns>
		static string SecurityWord(DreamDaemonSecurity securityLevel)
		{
			return securityLevel switch
			{
				DreamDaemonSecurity.Safe => "safe",
				DreamDaemonSecurity.Trusted => "trusted",
				DreamDaemonSecurity.Ultrasafe => "ultrasafe",
				_ => throw new ArgumentOutOfRangeException(nameof(securityLevel), securityLevel, String.Format(CultureInfo.InvariantCulture, "Bad DreamDaemon security level: {0}", securityLevel)),
			};
		}

		/// <summary>
		/// Change a given <paramref name="visibility"/> into the appropriate DreamDaemon command line word.
		/// </summary>
		/// <param name="visibility">The <see cref="DreamDaemonVisibility"/> level to change.</param>
		/// <returns>A <see cref="string"/> representation of the command line parameter.</returns>
		static string VisibilityWord(DreamDaemonVisibility visibility)
		{
			return visibility switch
			{
				DreamDaemonVisibility.Public => "public",
				DreamDaemonVisibility.Private => "private",
				DreamDaemonVisibility.Invisible => "invisible",
				_ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, String.Format(CultureInfo.InvariantCulture, "Bad DreamDaemon visibility level: {0}", visibility)),
			};
		}

		/// <summary>
		/// Check if a given <paramref name="port"/> can be bound to.
		/// </summary>
		/// <param name="port">The port number to test.</param>
		void PortBindTest(ushort port)
		{
			try
			{
				logger.LogTrace("Bind test: {0}", port);
				SocketExtensions.BindTest(port, false);
			}
			catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
			{
				throw new JobException(ErrorCode.DreamDaemonPortInUse, ex);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SessionControllerFactory"/> class.
		/// </summary>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		/// <param name="byond">The value of <see cref="byond"/>.</param>
		/// <param name="topicClientFactory">The value of <see cref="topicClientFactory"/>.</param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="instance">The value of <see cref="instance"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="chat">The value of <see cref="chat"/>.</param>
		/// <param name="networkPromptReaper">The value of <see cref="networkPromptReaper"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="bridgeRegistrar">The value of <see cref="bridgeRegistrar"/>.</param>
		/// <param name="serverPortProvider">The value of <see cref="serverPortProvider"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="EventConsumer"/>.</param>
		public SessionControllerFactory(
			IProcessExecutor processExecutor,
			IByondManager byond,
			ITopicClientFactory topicClientFactory,
			ICryptographySuite cryptographySuite,
			IAssemblyInformationProvider assemblyInformationProvider,
			IIOManager ioManager,
			IChatManager chat,
			INetworkPromptReaper networkPromptReaper,
			IPlatformIdentifier platformIdentifier,
			IBridgeRegistrar bridgeRegistrar,
			IServerPortProvider serverPortProvider,
			EventConsumer eventConsumer,
			ILoggerFactory loggerFactory,
			ILogger<SessionControllerFactory> logger,
			Api.Models.Instance instance)
		{
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.topicClientFactory = topicClientFactory ?? throw new ArgumentNullException(nameof(topicClientFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.networkPromptReaper = networkPromptReaper ?? throw new ArgumentNullException(nameof(networkPromptReaper));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.bridgeRegistrar = bridgeRegistrar ?? throw new ArgumentNullException(nameof(bridgeRegistrar));
			this.serverPortProvider = serverPortProvider ?? throw new ArgumentNullException(nameof(serverPortProvider));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task<ISessionController> LaunchNew(
			IDmbProvider dmbProvider,
			IByondExecutableLock currentByondLock,
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

			var chatTrackingContext = chat.CreateTrackingContext();
			try
			{
				// get the byond lock
				var byondLock = currentByondLock ?? await byond.UseExecutables(Version.Parse(dmbProvider.CompileJob.ByondVersion), cancellationToken).ConfigureAwait(false);
				try
				{
					logger.LogDebug(
						"Launching session with CompileJob {0}...",
						dmbProvider.CompileJob.Id);

					if (launchParameters.SecurityLevel == DreamDaemonSecurity.Trusted)
						await byondLock.TrustDmbPath(ioManager.ConcatPath(dmbProvider.Directory, dmbProvider.DmbName), cancellationToken).ConfigureAwait(false);

					PortBindTest(launchParameters.Port.Value);
					await CheckPagerIsNotRunning(cancellationToken).ConfigureAwait(false);

					var accessIdentifier = cryptographySuite.GetSecureString();

					var byondTopicSender = topicClientFactory.CreateTopicClient(
						TimeSpan.FromMilliseconds(
							launchParameters.TopicRequestTimeout.Value));

					// set command line options
					// more sanitization here cause it uses the same scheme
					var parameters = $"{DMApiConstants.ParamApiVersion}={byondTopicSender.SanitizeString(DMApiConstants.InteropVersion.Semver().ToString())}&{byondTopicSender.SanitizeString(DMApiConstants.ParamServerPort)}={serverPortProvider.HttpApiPort}&{byondTopicSender.SanitizeString(DMApiConstants.ParamAccessIdentifier)}={byondTopicSender.SanitizeString(accessIdentifier)}";

					if (!String.IsNullOrEmpty(launchParameters.AdditionalParameters))
						parameters = $"{parameters}&{launchParameters.AdditionalParameters}";

					// important to run on all ports to allow port changing
					Guid? logFileGuid = null;
					var arguments = String.Format(
						CultureInfo.InvariantCulture,
						"{0} -port {1} -ports 1-65535 {2}-close -logself -{3} -{4}{5} -params \"{6}\"",
						dmbProvider.DmbName,
						launchParameters.Port.Value,
						launchParameters.AllowWebClient.Value ? "-webclient " : String.Empty,
						SecurityWord(launchParameters.SecurityLevel.Value),
						VisibilityWord(launchParameters.Visibility.Value),
						platformIdentifier.IsWindows
							? $" -log {logFileGuid = Guid.NewGuid()}"
							: String.Empty, // Just use stdout on linux
						parameters);

					// See https://github.com/tgstation/tgstation-server/issues/719
					var noShellExecute = !platformIdentifier.IsWindows;

					if (!apiValidate && dmbProvider.CompileJob.DMApiVersion == null)
						logger.LogDebug("Session will have no DMAPI support!");

					// launch dd
					var process = processExecutor.LaunchProcess(
						byondLock.DreamDaemonPath,
						dmbProvider.Directory,
						arguments,
						noShellExecute,
						noShellExecute,
						noShellExecute: noShellExecute);

					async Task<string> GetDDOutput()
					{
						// DCT x2: None available
						if (!platformIdentifier.IsWindows)
							return await process.GetCombinedOutput(default).ConfigureAwait(false);

						var logFilePath = ioManager.ConcatPath(dmbProvider.Directory, logFileGuid.ToString());
						try
						{
							var dreamDaemonLogBytes = await ioManager.ReadAllBytes(
								logFilePath,
								default)
								.ConfigureAwait(false);

							return Encoding.UTF8.GetString(dreamDaemonLogBytes);
						}
						finally
						{
							try
							{
								// DCT: No token available
								await ioManager.DeleteFile(logFilePath, default).ConfigureAwait(false);
							}
							catch (Exception ex)
							{
								logger.LogWarning(ex, "Failed to delete DreamDaemon log file {0}!", logFilePath);
							}
						}
					}

					// Log DD output
					async Task PostLifetime()
					{
						try
						{
							var ddOutput = await GetDDOutput().ConfigureAwait(false);
							logger.LogTrace(
								"DreamDaemon Output:{0}{1}",
								Environment.NewLine,
								ddOutput);
						}
						catch (Exception ex)
						{
							logger.LogWarning(ex, "Error reading DreamDaemon output!");
						}
					}

					try
					{
						networkPromptReaper.RegisterProcess(process);

						var runtimeInformation = CreateRuntimeInformation(
							dmbProvider,
							chatTrackingContext,
							launchParameters.SecurityLevel.Value,
							launchParameters.Visibility.Value,
							apiValidate);

						var reattachInformation = new ReattachInformation(
							dmbProvider,
							process,
							runtimeInformation,
							accessIdentifier,
							launchParameters.Port.Value);

						var sessionController = new SessionController(
							reattachInformation,
							instance,
							process,
							byondLock,
							byondTopicSender,
							chatTrackingContext,
							bridgeRegistrar,
							chat,
							assemblyInformationProvider,
							loggerFactory.CreateLogger<SessionController>(),
							PostLifetime,
							launchParameters.StartupTimeout,
							false,
							apiValidate);

						// If this isnt a staging DD (From a Deployment), fire off an event
						if (!apiValidate)
							await eventConsumer.HandleEvent(
								EventType.DreamDaemonLaunch,
								new List<string>
								{
									process.Id.ToString(CultureInfo.InvariantCulture),
								},
								cancellationToken)
								.ConfigureAwait(false);

						return sessionController;
					}
					catch
					{
						using (process)
						{
							process.Terminate();
							await process.Lifetime.ConfigureAwait(false);
							throw;
						}
					}
				}
				catch
				{
					if (currentByondLock == null)
						byondLock.Dispose();
					throw;
				}
			}
			catch
			{
				chatTrackingContext.Dispose();
				throw;
			}
		}
#pragma warning restore CA1506

		/// <inheritdoc />
		public async Task<ISessionController> Reattach(
			ReattachInformation reattachInformation,
			CancellationToken cancellationToken)
		{
			if (reattachInformation == null)
				throw new ArgumentNullException(nameof(reattachInformation));

			logger.LogTrace("Begin session reattach...");
			var byondTopicSender = topicClientFactory.CreateTopicClient(reattachInformation.TopicRequestTimeout);
			var chatTrackingContext = chat.CreateTrackingContext();
			try
			{
				var byondLock = await byond.UseExecutables(Version.Parse(reattachInformation.Dmb.CompileJob.ByondVersion), cancellationToken).ConfigureAwait(false);

				try
				{
					logger.LogDebug(
						"Attaching to session PID: {0}, CompileJob: {1}...",
						reattachInformation.ProcessId,
						reattachInformation.Dmb.CompileJob.Id);

					var process = processExecutor.GetProcess(reattachInformation.ProcessId);
					if (process == null)
						return null;

					try
					{
						networkPromptReaper.RegisterProcess(process);
						var runtimeInformation = CreateRuntimeInformation(
							reattachInformation.Dmb,
							chatTrackingContext,
							null,
							null,
							false);
						reattachInformation.SetRuntimeInformation(runtimeInformation);

						var controller = new SessionController(
							reattachInformation,
							instance,
							process,
							byondLock,
							byondTopicSender,
							chatTrackingContext,
							bridgeRegistrar,
							chat,
							assemblyInformationProvider,
							loggerFactory.CreateLogger<SessionController>(),
							() => Task.CompletedTask,
							null,
							true,
							false);

						process = null;
						byondLock = null;
						chatTrackingContext = null;

						return controller;
					}
					finally
					{
						process?.Dispose();
					}
				}
				finally
				{
					byondLock?.Dispose();
				}
			}
			finally
			{
				chatTrackingContext?.Dispose();
			}
		}

		/// <summary>
		/// Create <see cref="RuntimeInformation"/>.
		/// </summary>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/>.</param>
		/// <param name="chatTrackingContext">The <see cref="IChatTrackingContext"/>.</param>
		/// <param name="securityLevel">The <see cref="DreamDaemonSecurity"/> level if any.</param>
		/// <param name="visibility">The <see cref="DreamDaemonVisibility"/> if any.</param>
		/// <param name="apiValidateOnly">The value of <see cref="RuntimeInformation.ApiValidateOnly"/>.</param>
		/// <returns>A new <see cref="RuntimeInformation"/> class.</returns>
		RuntimeInformation CreateRuntimeInformation(
			IDmbProvider dmbProvider,
			IChatTrackingContext chatTrackingContext,
			DreamDaemonSecurity? securityLevel,
			DreamDaemonVisibility? visibility,
			bool apiValidateOnly)
			=> new RuntimeInformation(
				chatTrackingContext,
				dmbProvider,
				assemblyInformationProvider.Version,
				instance.Name,
				securityLevel,
				visibility,
				serverPortProvider.HttpApiPort,
				apiValidateOnly);

		/// <summary>
		/// Make sure the BYOND pager is not running.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task CheckPagerIsNotRunning(CancellationToken cancellationToken)
		{
			if (!platformIdentifier.IsWindows)
				return;

			using var otherProcess = processExecutor.GetProcessByName("byond");
			if (otherProcess == null)
				return;

			var otherUsernameTask = otherProcess.GetExecutingUsername(cancellationToken);
			using var ourProcess = processExecutor.GetCurrentProcess();
			var ourUserName = await ourProcess.GetExecutingUsername(cancellationToken).ConfigureAwait(false);
			var otherUserName = await otherUsernameTask.ConfigureAwait(false);

			if (otherUserName.Equals(ourUserName, StringComparison.Ordinal))
				throw new JobException(ErrorCode.DeploymentPagerRunning);
		}
	}
}
