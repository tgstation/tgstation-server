using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog, IEventConsumer, ICustomCommandHandler
	{
		/// <summary>
		/// The time in milliseconds to wait from starting <see cref="alphaServer"/> to start <see cref="bravoServer"/>. Does not take responsiveness into account
		/// </summary>
		const int AlphaBravoStartupSeperationInterval = 3000;

		/// <inheritdoc />
		public bool Running { get; private set; }

		/// <inheritdoc />
		public bool AlphaIsActive { get; private set; }

		/// <inheritdoc />
		public LaunchResult LastLaunchResult { get; private set; }

		/// <inheritdoc />
		public DreamDaemonLaunchParameters ActiveLaunchParameters { get; private set; }

		/// <inheritdoc />
		public DreamDaemonLaunchParameters LastLaunchParameters { get; private set; }

		/// <inheritdoc />
		public RebootState? RebootState => Running ? (AlphaIsActive ? alphaServer?.RebootState : bravoServer?.RebootState) : null;

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IChat chat;

		/// <summary>
		/// The <see cref="ISessionControllerFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly ISessionControllerFactory sessionControllerFactory;

		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IDmbFactory dmbFactory;

		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly ILogger<Watchdog> logger;

		/// <summary>
		/// The <see cref="IReattachInfoHandler"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IReattachInfoHandler reattachInfoHandler;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IByondTopicSender"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IByondTopicSender byondTopicSender;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// The <see cref="Api.Models.Instance.Id"/> the <see cref="Watchdog"/> belongs to
		/// </summary>
		readonly long instanceId;

		/// <summary>
		/// If the <see cref="Watchdog"/> should <see cref="LaunchNoLock(bool, bool, bool, CancellationToken)"/> in <see cref="StartAsync(CancellationToken)"/>
		/// </summary>
		readonly bool autoStart;

		CancellationTokenSource monitorCts;
		Task monitorTask;

		ISessionController alphaServer;
		ISessionController bravoServer;

		/// <summary>
		/// If the servers should be released instead of shutdown
		/// </summary>
		bool releaseServers;

		/// <summary>
		/// Construct a <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="sessionControllerFactory">The value of <see cref="sessionControllerFactory"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="serverUpdater">The <see cref="IServerUpdater"/> for the <see cref="Watchdog"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="reattachInfoHandler">The value of <see cref="reattachInfoHandler"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="initialLaunchParameters">The initial value of <see cref="ActiveLaunchParameters"/></param>
		/// <param name="instance">The <see cref="Models.Instance"/> containing the value of <see cref="instanceId"/></param>
		/// <param name="autoStart">The value of <see cref="autoStart"/></param>
		public Watchdog(IChat chat, ISessionControllerFactory sessionControllerFactory, IDmbFactory dmbFactory, IServerUpdater serverUpdater, ILogger<Watchdog> logger, IReattachInfoHandler reattachInfoHandler, IDatabaseContextFactory databaseContextFactory, IByondTopicSender byondTopicSender, DreamDaemonLaunchParameters initialLaunchParameters, Models.Instance instance, bool autoStart)
		{
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.sessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.reattachInfoHandler = reattachInfoHandler ?? throw new ArgumentNullException(nameof(reattachInfoHandler));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			instanceId = instance?.Id ?? throw new ArgumentNullException(nameof(instance));
			this.autoStart = autoStart;

			if (serverUpdater == null)
				throw new ArgumentNullException(nameof(serverUpdater));

			serverUpdater.RegisterForUpdate(() => releaseServers = true);

			chat.RegisterCommandHandler(this);

			AlphaIsActive = true;
			ActiveLaunchParameters = initialLaunchParameters;
			releaseServers = false;
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			DisposeAndNullControllers();
			semaphore.Dispose();
		}

		void DisposeAndNullControllers()
		{
			logger.LogTrace("DisposeAndNullControllers");
			alphaServer?.Dispose();
			alphaServer = null;
			bravoServer?.Dispose();
			bravoServer = null;
		}

		async Task<WatchdogLaunchResult> RestartNoLock(bool graceful, CancellationToken cancellationToken)
		{
			var running = Running;
			if (!graceful || !running)
			{
				Task chatTask;
				if (running)
				{
					chatTask = chat.SendWatchdogMessage("Manual restart triggered...", cancellationToken);
					await TerminateNoLock(false, false, cancellationToken).ConfigureAwait(false);
				}
				else
					chatTask = Task.CompletedTask;
				var result = await LaunchNoLock(true, !running, false, cancellationToken).ConfigureAwait(false);
				await chatTask.ConfigureAwait(false);
				return result;
			}
			var toReboot = AlphaIsActive ? alphaServer : bravoServer;
			var other = AlphaIsActive ? bravoServer : alphaServer;
			if (toReboot != null)
				//todo, log the result
				await toReboot.SetRebootState(Components.Watchdog.RebootState.Restart, cancellationToken).ConfigureAwait(false);
			return null;
		}

		async Task TerminateNoLock(bool graceful, bool announce, CancellationToken cancellationToken)
		{
			if (!Running)
				return;
			if (!graceful)
			{
				var chatTask = announce ? chat.SendWatchdogMessage("Terminating...", cancellationToken) : Task.CompletedTask;
				await StopMonitor().ConfigureAwait(false);
				DisposeAndNullControllers();
				await chatTask.ConfigureAwait(false);
				return;
			}
			var toKill = AlphaIsActive ? alphaServer : bravoServer;
			var other = AlphaIsActive ? bravoServer : alphaServer;
			if (toKill != null)
				await toKill.SetRebootState(Components.Watchdog.RebootState.Shutdown, cancellationToken).ConfigureAwait(false);
		}

		async Task HandlerMonitorWakeup(MonitorActivationReason activationReason, MonitorState monitorState)
		{
			logger.LogInformation("Monitor activation. Reason: {0}", activationReason);
			await Task.Yield();
			throw new NotImplementedException(nameof(monitorState));
		}

		/// <summary>
		/// The loop that watches the watchdog
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task MonitorLifetimes(CancellationToken cancellationToken)
		{
			logger.LogDebug("Entered MonitorLifetimes");
			var iteration = 1;
			for(var state = new MonitorState(); state.NextAction != MonitorAction.Exit; ++iteration)
			{
				logger.LogDebug("New iteration of monitor loop");
				try
				{
					if(AlphaIsActive)
						logger.LogDebug("Alpha is the active server");
					else
						logger.LogDebug("Bravo is the active server");

					if(state.InactiveServerHasStagedDmb)
						logger.LogDebug("Inactive server has staged .dmb");
					if (state.RebootingInactiveServer)
						logger.LogDebug("Inactive server is rebooting");

					state.ActiveServer = AlphaIsActive ? alphaServer : bravoServer;
					state.InactiveServer = AlphaIsActive ? bravoServer : alphaServer;

					var activeServerLifetime = state.ActiveServer.Lifetime;
					var inactiveServerLifetime = state.InactiveServer.Lifetime;
					var activeServerReboot = state.ActiveServer.OnReboot;
					var inactiveServerReboot = state.InactiveServer.OnReboot;
					var inactiveServerStartup = state.InactiveServer.LaunchResult;
					var newDmbAvailable = dmbFactory.OnNewerDmb;

					var cancelTcs = new TaskCompletionSource<object>();
					using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
					{
						var toWaitOn = Task.WhenAny(activeServerLifetime, inactiveServerLifetime, activeServerReboot, inactiveServerReboot, newDmbAvailable, cancelTcs.Task);
						if (state.RebootingInactiveServer)
							toWaitOn = Task.WhenAny(toWaitOn, inactiveServerStartup);
						await toWaitOn.ConfigureAwait(false);
					}

					var chatTask = Task.CompletedTask;
					using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
					{
						MonitorActivationReason activationReason = default;
						//multiple things may have happened, handle them one at a time
						for (var moreActivationsToProcess = true; moreActivationsToProcess && state.NextAction == MonitorAction.Continue; await HandlerMonitorWakeup(activationReason, state).ConfigureAwait(false))
						{
							if (activeServerLifetime?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.ActiveServerCrashed;
								activeServerLifetime = null;
							}
							else if (inactiveServerLifetime?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.InactiveServerCrashed;
								inactiveServerLifetime = null;
							}
							else if (activeServerReboot?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.ActiveServerRebooted;
								activeServerReboot = null;
							}
							else if (inactiveServerReboot?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.InactiveServerRebooted;
								inactiveServerReboot = null;
							}
							else if (inactiveServerStartup?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.InactiveServerStartupComplete;
								inactiveServerStartup = null;
							}
							else if (newDmbAvailable?.IsCompleted == true)
							{
								activationReason = MonitorActivationReason.NewDmbAvailable;
								newDmbAvailable = null;
							}
							else
								moreActivationsToProcess = false;
						}
						//full reboot required
						if (state.NextAction == MonitorAction.Restart)
						{
							logger.LogDebug("Next state action is to restart");
							DisposeAndNullControllers();
							Running = false;
							chatTask = chat.SendWatchdogMessage("Restarting due to complications...", cancellationToken);
						}
					}

					for (var retryAttempts = 1; state.NextAction == MonitorAction.Restart; ++retryAttempts)
					{
						WatchdogLaunchResult result;
						using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
							result = await LaunchNoLock(false, false, false, cancellationToken).ConfigureAwait(false);

						await chatTask.ConfigureAwait(false);
						if (Running)
							state.NextAction = MonitorAction.Continue;
						else
						{
							logger.LogWarning("Failed to automatically restart the watchdog! Alpha: {0}; Bravo: {1}", result.Alpha.ToString(), result.Bravo.ToString());
							var retryDelay = Math.Min(Math.Pow(2, retryAttempts), 3600); //max of one hour
							chatTask = chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Failed to restart watchdog (Attempt: {0}), retrying in {1} seconds...", retryAttempts, retryDelay), cancellationToken);
							await Task.WhenAll(Task.Delay((int)retryDelay, cancellationToken), chatTask).ConfigureAwait(false);
						}
					}
				}
				catch (OperationCanceledException)
				{
					logger.LogDebug("Monitor cancelled");
					break;
				}
				catch (Exception e)
				{
					logger.LogError("Monitor crashed! Iteration: {0}, State: {1}", iteration, JsonConvert.SerializeObject(state));
					await chat.SendWatchdogMessage(String.Format(CultureInfo.InvariantCulture, "Monitor crashed, this should NEVER happen! Please report this, full details in logs! Restarting monitor... Error: {0}", e.Message), cancellationToken).ConfigureAwait(false);
				}
			}
		}

		async Task<bool> StopMonitor()
		{
			logger.LogTrace("StopMonitor");
			if (monitorTask == null)
				return false;
			monitorCts.Cancel();
			await monitorTask.ConfigureAwait(false);
			monitorCts.Dispose();
			monitorTask = null;
			return true;
		}


		/// <inheritdoc />
		public async Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				ActiveLaunchParameters = launchParameters;
				if (Running)
					await RestartNoLock(true, cancellationToken).ConfigureAwait(false);
			}
		}

		async Task<WatchdogLaunchResult> LaunchNoLock(bool startMonitor, bool announce, bool doReattach, CancellationToken cancellationToken)
		{
			using (var alphaStartCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				logger.LogTrace("Begin LaunchNoLock");
				if (Running)
				{
					logger.LogTrace("Aborted due to already running!");
					return null;
				}
				
				Task chatTask;
				//this is necessary, the monitor could be in it's sleep loop trying to restart
				if (startMonitor && await StopMonitor().ConfigureAwait(false))
					chatTask = chat.SendWatchdogMessage("Automatic retry sequence cancelled by manual launch. Restarting...", cancellationToken);
				else if (announce)
					chatTask = chat.SendWatchdogMessage("Starting...", cancellationToken);
				else
					chatTask = Task.CompletedTask;
				//start both servers
				LastLaunchParameters = ActiveLaunchParameters;
				try
				{
					//good ole sanity
					if (alphaServer != null || bravoServer != null)
						throw new InvalidOperationException("Entered LaunchNoLock with one or more of the servers not being null!");

					var reattachInfo = doReattach ? await reattachInfoHandler.Load(cancellationToken).ConfigureAwait(false) : null;
					var doesntNeedNewDmb = doReattach && reattachInfo.Alpha != null && reattachInfo.Bravo != null;
					var dmbToUse = doesntNeedNewDmb ? null : await dmbFactory.LockNextDmb(cancellationToken).ConfigureAwait(false);

					Task<ISessionController> alphaServerTask = null;
					try
					{
						if (!doReattach || reattachInfo.Alpha == null)
							alphaServerTask = sessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbToUse, null, true, true, false, alphaStartCts.Token);
						else
							alphaServerTask = sessionControllerFactory.Reattach(reattachInfo.Alpha, cancellationToken);
						//do a few seconds of delay so that any backends the servers use know that alpha came first
						await Task.Delay(AlphaBravoStartupSeperationInterval, cancellationToken).ConfigureAwait(false);
						Task<ISessionController> bravoServerTask;
						if (!doReattach || reattachInfo.Bravo == null)
							bravoServerTask = sessionControllerFactory.LaunchNew(ActiveLaunchParameters, dmbToUse, null, false, false, false, cancellationToken);
						else
							bravoServerTask = sessionControllerFactory.Reattach(reattachInfo.Bravo, cancellationToken);

						await Task.WhenAll(alphaServerTask, bravoServerTask).ConfigureAwait(false);

						async Task<LaunchResult> CheckLaunch(ISessionController controller, string serverName)
						{
							var launch = await controller.LaunchResult.ConfigureAwait(false);
							if (launch.ExitCode.HasValue)
								//you killed us ray...
								throw new Exception(String.Format(CultureInfo.InvariantCulture, "{1} server failed to start: {0}", launch.ToString(), serverName));
							return launch;
						}

						var alphaLrt = CheckLaunch(alphaServer, "Alpha");
						var bravoLrt = CheckLaunch(bravoServer, "Bravo");
						//now we have two booting servers, get them up and running
						var allTask = Task.WhenAll(alphaLrt, bravoLrt);

						//don't forget about the cancelationToken
						var cancelTcs = new TaskCompletionSource<object>();
						using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
							await Task.WhenAny(allTask, cancelTcs.Task).ConfigureAwait(false);

						//update the live and staged jobs in the db
						await databaseContextFactory.UseContext(async db =>
						{
							var settings = new Models.DreamDaemonSettings
							{
								InstanceId = instanceId
							};
							var cj = (AlphaIsActive ? alphaServer : bravoServer).Dmb.CompileJob;
							db.CompileJobs.Attach(cj);
							db.DreamDaemonSettings.Attach(settings);
							settings.StagedCompileJob = null;
							settings.ActiveCompileJob = cj;
							await db.Save(cancellationToken).ConfigureAwait(false);
						}).ConfigureAwait(false);

						//both servers are now running, alpha is the active server, huzzah
						AlphaIsActive = doReattach ? reattachInfo.AlphaIsActive : true;
						LastLaunchResult = alphaLrt.Result;
						Running = true;

						if (startMonitor)
						{
							await StopMonitor().ConfigureAwait(false);
							monitorCts = new CancellationTokenSource();
							monitorTask = MonitorLifetimes(monitorCts.Token);
						}
						return new WatchdogLaunchResult
						{
							Alpha = alphaLrt.Result,
							Bravo = bravoLrt.Result
						};
					}
					catch
					{
						if (alphaServer == null && bravoServer == null)
							dmbToUse.Dispose(); //guaranteed to not be null here
						DisposeAndNullControllers();
						throw;
					}
				}
				catch (Exception e)
				{
					logger.LogWarning("Failed to start watchdog: {0}", e.ToString());
					throw;
				}
				finally
				{
					try
					{
						await chatTask.ConfigureAwait(false);
					}
					catch (OperationCanceledException) { }
				}
			}
		}

		/// <inheritdoc />
		public async Task<WatchdogLaunchResult> Launch(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				return await LaunchNoLock(true, true, false, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<WatchdogLaunchResult> Restart(bool graceful, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				return await RestartNoLock(graceful, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task Terminate(bool graceful, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				await TerminateNoLock(graceful, true, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (autoStart)
				await LaunchNoLock(true, true, true, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			if (releaseServers)
			{
				var reattachInformation = new WatchdogReattachInformation { AlphaIsActive = AlphaIsActive };
				reattachInformation.Alpha = alphaServer?.Release();
				reattachInformation.Bravo = bravoServer?.Release();
				await reattachInfoHandler.Save(reattachInformation, cancellationToken).ConfigureAwait(false);
			}
			await Terminate(false, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task HandleEvent(EventType eventType, IEnumerable<string> parameters, CancellationToken cancellationToken)
		{
			string results;
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!Running)
					return;

				var builder = new StringBuilder(InteropConstants.DMTopicEvent);
				foreach (var I in parameters)
				{
					builder.Append("&");
					builder.Append(byondTopicSender.SanitizeString(I));
				}

				var activeServer = AlphaIsActive ? alphaServer : bravoServer;
				results = await activeServer.SendCommand(builder.ToString(), cancellationToken).ConfigureAwait(false);
			}

			if (results == null)
				return;

			List<Response> responses;
			try
			{
				responses = JsonConvert.DeserializeObject<List<Response>>(results);
			}
			catch
			{
				logger.LogInformation("Recieved invalid response from DD when parsing event {0}:{1}{2}", eventType, Environment.NewLine, results);
				return;
			}

			await Task.WhenAll(responses.Select(x => chat.SendMessage(x.Message, x.ChannelIds, cancellationToken))).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<string> HandleChatCommand(string commandName, IEnumerable<string> arguments, Chat.User sender, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (!Running)
					return "ERROR: Server offline!";

				var command = String.Format(CultureInfo.InvariantCulture, "{0}&{1}={2}", byondTopicSender.SanitizeString(InteropConstants.DMTopicChatCommand), byondTopicSender.SanitizeString(InteropConstants.DMParameterData), byondTopicSender.SanitizeString(JsonConvert.SerializeObject(arguments)));

				var activeServer = AlphaIsActive ? alphaServer : bravoServer;
				return await activeServer.SendCommand(command, cancellationToken).ConfigureAwait(false) ?? "ERROR: Bad topic exchange!";
			}
		}
	}
}
