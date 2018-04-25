using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class DreamDaemon : IDreamDaemon, IDisposable
	{
		/// <inheritdoc />
		public bool Running { get; private set; }

		/// <inheritdoc />
		public ushort? CurrentPort { get; private set; }

		/// <inheritdoc />
		public string AccessToken { get; private set; }

		/// <inheritdoc />
		public DreamDaemonSecurity? CurrentSecurity { get; private set; }

		/// <inheritdoc />
		public bool SoftRebooting { get; private set; }

		/// <inheritdoc />
		public bool SoftStopping { get; private set; }

		/// <summary>
		/// The <see cref="IEventConsumer"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;
		/// <summary>
		/// The <see cref="IByond"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IByond byond;
		/// <summary>
		/// The <see cref="ICryptographySuite"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;
		/// <summary>
		/// The <see cref="IInterop"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IInterop interop;
		/// <summary>
		/// The <see cref="IInstanceShutdownMethod"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IInstanceShutdownMethod instanceShutdownMethod;
		/// <summary>
		/// The <see cref="IDreamDaemonExecutor"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IDreamDaemonExecutor dreamDaemonExecutor;
		/// <summary>
		/// The <see cref="IDmbFactory"/> for <see cref="DreamDaemon"/>
		/// </summary>
		readonly IDmbFactory dmbFactory;

		/// <summary>
		/// Used for write control to class variables
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// If <see cref="StartAsync(CancellationToken)"/> should start DD
		/// </summary>
		readonly bool autoStart;

		/// <summary>
		/// The current <see cref="DreamDaemonLaunchParameters"/>
		/// </summary>
		DreamDaemonLaunchParameters currentLaunchParameters;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="watchdogTask"/>
		/// </summary>
		CancellationTokenSource watchdogCancellationTokenSource;
		/// <summary>
		/// The monitor for the DD process
		/// </summary>
		Task watchdogTask;
		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> to complete when the primary server is primed
		/// </summary>
		TaskCompletionSource<object> onPrimaryServerPrimed;
		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> to complete when the primary server is rebooted
		/// </summary>
		TaskCompletionSource<object> onPrimaryServerRebooted;
		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> to complete when the secondary server is rebooted
		/// </summary>
		TaskCompletionSource<object> onSecondaryServerRebooted;

		/// <summary>
		/// Construct <see cref="DreamDaemon"/>
		/// </summary>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="interop">The value of <see cref="interop"/></param>
		/// <param name="instanceShutdownMethod">The value of <see cref="instanceShutdownMethod"/></param>
		/// <param name="dreamDaemonExecutor">The value of <see cref="dreamDaemonExecutor"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="initialSettings">The initial value of <see cref="currentLaunchParameters"/> and <see cref="autoStart"/></param>
		public DreamDaemon(IEventConsumer eventConsumer, IByond byond, ICryptographySuite cryptographySuite, IInterop interop, IInstanceShutdownMethod instanceShutdownMethod, IDreamDaemonExecutor dreamDaemonExecutor, IDmbFactory dmbFactory, DreamDaemonSettings initialSettings)
		{
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.interop = interop ?? throw new ArgumentNullException(nameof(interop));
			this.instanceShutdownMethod = instanceShutdownMethod ?? throw new ArgumentNullException(nameof(instanceShutdownMethod));
			this.dreamDaemonExecutor = dreamDaemonExecutor ?? throw new ArgumentNullException(nameof(dreamDaemonExecutor));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			currentLaunchParameters = initialSettings ?? throw new ArgumentNullException(nameof(initialSettings));

			interop.SetServerControlHandler(OnServerControl);

			autoStart = initialSettings.AutoStart;

			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (watchdogCancellationTokenSource != null)
				watchdogCancellationTokenSource.Dispose();
			semaphore.Dispose();
		}

		/// <summary>
		/// Handler for server control events
		/// </summary>
		/// <param name="serverControlEventArgs">The <see cref="ServerControlEvent"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		static async Task OnServerControl(ServerControlEvent serverControlEventArgs, CancellationToken cancellationToken)
		{
			if (serverControlEventArgs == null)
				throw new ArgumentNullException(nameof(serverControlEventArgs));

			await Task.Yield();
			throw new NotImplementedException();
		}

		/// <summary>
		/// Main DD execution and monitoring <see cref="Task"/>
		/// </summary>
		/// <param name="onSuccessfulStartup">The <see cref="TaskCompletionSource{TResult}"/> to be completed when the server initially starts</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task Watchdog(TaskCompletionSource<object> onSuccessfulStartup, CancellationToken cancellationToken)
		{
			if (await byond.GetVersion(cancellationToken).ConfigureAwait(false) == null)
				throw new InvalidOperationException("No byond version installed!");
			await byond.ClearCache(cancellationToken).ConfigureAwait(false);
			
			//lock the byond executable and run the server
			async Task<int> RunServer(DreamDaemonLaunchParameters launchParameters, string dreamDaemonPath, string accessToken, bool isPrimary, CancellationToken serverCancellationToken)
			{
				using (var dmb = await dmbFactory.LockNextDmb(cancellationToken).ConfigureAwait(false))
				{
					return await dreamDaemonExecutor.RunDreamDaemon(launchParameters, onSuccessfulStartup, dreamDaemonPath, String.Concat(dmb.PrimaryDirectory, dmb.DmbName), accessToken, isPrimary, serverCancellationToken).ConfigureAwait(false);
				}
			};

			void StartServer(DreamDaemonLaunchParameters launchParameters, bool isPrimary, out Task<int> ddTask, out CancellationTokenSource cancellationTokenSource)
			{
				cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				try
				{
					var accessToken = cryptographySuite.GetSecureString();
					var ddToken = cancellationTokenSource.Token;
					ddTask = byond.UseExecutable(dreamDaemonPath => RunServer(launchParameters, dreamDaemonPath, accessToken, isPrimary, ddToken), false, true);
					interop.SetRun(isPrimary ? launchParameters.PrimaryPort : launchParameters.SecondaryPort, accessToken, isPrimary);
				}
				catch
				{
					cancellationTokenSource.Dispose();
					throw;
				}
			};

			bool secondaryIsOther = true;
			var retries = 0;
			do
			{
				var retryDelay = (int)Math.Min(Math.Pow(2, retries), TimeSpan.FromHours(1).Milliseconds); //max of one hour
				await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
				
				//load the event tcs' and get the initial launch parameters
				var primaryRebootedTcs = new TaskCompletionSource<object>();
				var secondaryRebootedTcs = new TaskCompletionSource<object>();
				var primaryPrimedTcs = new TaskCompletionSource<object>();
				DreamDaemonLaunchParameters initialLaunchParameters;
				await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
				try
				{
					onPrimaryServerRebooted = primaryRebootedTcs;
					onSecondaryServerRebooted = secondaryRebootedTcs;
					onPrimaryServerPrimed = primaryPrimedTcs;
					initialLaunchParameters = currentLaunchParameters;
				}
				finally
				{
					semaphore.Release();
				}

				//start the primary server
				StartServer(initialLaunchParameters, true, out Task<int> ddPrimaryTask, out CancellationTokenSource primaryCts);
				using (primaryCts)
				{
					//wait to make sure we got this far
					await onSuccessfulStartup.Task.ConfigureAwait(false);
					onSuccessfulStartup = null;

					//wait for either the server to exit or the server to be primed
					await Task.WhenAny(ddPrimaryTask, primaryPrimedTcs.Task).ConfigureAwait(false);


					//if the server has exited
					async Task<bool> HandleServerCrashed(Task<int> serverTask, bool isPrimary)
					{

						int exitCode;
						try
						{
							//nothing to do except try and reboot it
							exitCode = await serverTask.ConfigureAwait(false);
						}
						catch (OperationCanceledException)
						{
							return true;
						}
						await eventConsumer.HandleEvent(exitCode == 0 ? (isPrimary ? EventType.DDExit : EventType.DDOtherExit) : (isPrimary ? EventType.DDCrash : EventType.DDOtherCrash), null, cancellationToken).ConfigureAwait(false);
						return false;
					};

					if (ddPrimaryTask.IsCompleted)
					{
						if (await HandleServerCrashed(ddPrimaryTask, true).ConfigureAwait(false))
							return;
						++retries;
						continue;
					}

					//start the secondary server
					StartServer(initialLaunchParameters, false, out Task<int> ddSecondaryTask, out CancellationTokenSource secondaryCts);
					using (secondaryCts)
					{
						//now we wait for something to happen
						await Task.WhenAny(ddSecondaryTask, ddPrimaryTask, primaryRebootedTcs.Task).ConfigureAwait(false);
						

						if (ddSecondaryTask.IsCompleted && ddPrimaryTask.IsCompleted)
						{
							//catastrophic, start over
							var t1 = HandleServerCrashed(ddPrimaryTask, secondaryIsOther);
							var t2 = HandleServerCrashed(ddSecondaryTask, !secondaryIsOther);
							await Task.WhenAll(t1, t2).ConfigureAwait(false);
							if(t1.Result)
								return;
							++retries;
							continue;
						}

						//crash of otherServer
						if ((ddSecondaryTask.IsCompleted && secondaryIsOther) || (ddPrimaryTask.IsCompleted && !secondaryIsOther))
						{
							
						}
					}
				}
			} while (true);
		}

		/// <inheritdoc />
		public async Task CancelGracefulActions(CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				SoftRebooting = false;
				SoftStopping = false;
			}
			finally
			{
				semaphore.Release();
			}
		}

		/// <inheritdoc />
		public async Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			if (launchParameters == null)
				throw new ArgumentNullException(nameof(launchParameters));
			Task launchTask;
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				currentLaunchParameters = launchParameters;
				if (!Running)
					launchTask = Launch(launchParameters, cancellationToken);
				else
					launchTask = Restart(true, cancellationToken);
			}
			finally
			{
				semaphore.Release();
			}
			await launchTask.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task Launch(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			if (launchParameters == null)
				throw new ArgumentNullException(nameof(launchParameters));
			TaskCompletionSource<object> startupTcs;
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (Running)
					throw new InvalidOperationException("DreamDaemon already running!");
				Running = true;
				await eventConsumer.HandleEvent(EventType.DDLaunched, null, cancellationToken).ConfigureAwait(false);
				watchdogCancellationTokenSource?.Dispose();
				watchdogCancellationTokenSource = new CancellationTokenSource();
				startupTcs = new TaskCompletionSource<object>();
				watchdogTask = Watchdog(startupTcs, watchdogCancellationTokenSource.Token);
			}
			finally
			{
				semaphore.Release();
			}
			//important to leave the lock so the watchdog can enter it
			await startupTcs.Task.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task Restart(bool graceful, CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (graceful)
				{
					if (!Running)
						throw new InvalidOperationException("DreamDaemon not running!");
					if (SoftStopping)
						throw new InvalidOperationException("DreamDaemon has a graceful stop queued!");
					SoftRebooting = true;
					return;
				}
				await eventConsumer.HandleEvent(EventType.DDRestart, null, cancellationToken).ConfigureAwait(false);
				if (Running)
					watchdogCancellationTokenSource.Cancel();
				await Launch(currentLaunchParameters, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				semaphore.Release();
			}
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken) => autoStart ? Launch(currentLaunchParameters, cancellationToken) : Task.CompletedTask;

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Terminate(false, cancellationToken);

		/// <inheritdoc />
		public async Task Terminate(bool graceful, CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (graceful)
				{
					if (!Running)
						throw new InvalidOperationException("DreamDaemon not running!");
					SoftRebooting = false;
					SoftStopping = true;
					return;
				}
				await eventConsumer.HandleEvent(EventType.DDTerminated, null, cancellationToken).ConfigureAwait(false);
				watchdogCancellationTokenSource.Cancel();
				await watchdogTask.ConfigureAwait(false);
			}
			finally
			{
				semaphore.Release();
			}
		}
	}
}
