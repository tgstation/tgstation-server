using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog, IDisposable
	{
		/// <summary>
		/// The <see cref="IByond"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IByond byond;
		/// <summary>
		/// The <see cref="IDreamDaemonExecutor"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IDreamDaemonExecutor dreamDaemonExecutor;
		/// <summary>
		/// The <see cref="IInterop"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IInterop interop;
		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IDmbFactory dmbFactory;
		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;
		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// Represents the currently running <see cref="Watchdog"/>
		/// </summary>
		Task watchdogTask;
		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="watchdogTask"/>
		/// </summary>
		CancellationTokenSource watchdogCancellationTokenSource;

		/// <summary>
		/// Construct a <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="dreamDaemonExecutor">The value of <see cref="dreamDaemonExecutor"/></param>
		/// <param name="interop">The value of <see cref="interop"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		public Watchdog(IByond byond, IDreamDaemonExecutor dreamDaemonExecutor, IInterop interop, IDmbFactory dmbFactory, ICryptographySuite cryptographySuite, IEventConsumer eventConsumer)
		{
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.dreamDaemonExecutor = dreamDaemonExecutor ?? throw new ArgumentNullException(nameof(dreamDaemonExecutor));
			this.interop = interop ?? throw new ArgumentNullException(nameof(interop));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
		}

		/// <inheritdoc />
		public void Dispose() => watchdogCancellationTokenSource?.Dispose();

		/// <summary>
		/// Loads a dmb and runs it through <see cref="dreamDaemonExecutor"/>
		/// </summary>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the run</param>
		/// <param name="onSuccessfulStartup">The <see cref="TaskCompletionSource{TResult}"/> to be completed once the server starts if any</param>
		/// <param name="accessToken">The access token for the server</param>
		/// <param name="dreamDaemonPath">The path to the DreamDaemon executable</param>
		/// <param name="isPrimary">If a primary server is being launched</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the exit code of DreamDaemon</returns>
		async Task<int> RunServer(DreamDaemonLaunchParameters launchParameters, TaskCompletionSource<object> onSuccessfulStartup, string accessToken, string dreamDaemonPath, bool isPrimary, CancellationToken cancellationToken)
		{
			using (var dmb = await dmbFactory.LockNextDmb(cancellationToken).ConfigureAwait(false))
				return await dreamDaemonExecutor.RunDreamDaemon(launchParameters, onSuccessfulStartup, dreamDaemonPath, String.Concat(dmb.PrimaryDirectory, dmb.DmbName), accessToken, isPrimary, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Locks in a <see cref="IByond"/> version and runs a server through <see cref="RunServer(DreamDaemonLaunchParameters, TaskCompletionSource{object}, string, string, bool, CancellationToken)"/>
		/// </summary>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the run</param>
		/// <param name="onSuccessfulStartup">The <see cref="TaskCompletionSource{TResult}"/> to be completed once the server starts if any</param>
		/// <param name="accessToken">The access token for the server</param>
		/// <param name="isPrimary">If a primary server is being launched</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <param name="cancellationTokenSource">A <see cref="CancellationTokenSource"/> tied to the lifetime of the resulting <see cref="Task{TResult}"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the exit code of DreamDaemon</returns>
		Task<int> StartServer(DreamDaemonLaunchParameters launchParameters, TaskCompletionSource<object> onSuccessfulStartup, string accessToken, bool isPrimary, CancellationToken cancellationToken, out CancellationTokenSource cancellationTokenSource)
		{
			cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			try
			{
				var ddToken = cancellationTokenSource.Token;
				var ddTask = byond.UseExecutable(dreamDaemonPath => RunServer(launchParameters, onSuccessfulStartup, accessToken, dreamDaemonPath, isPrimary, ddToken), false, true);
				interop.SetRun(isPrimary ? launchParameters.PrimaryPort : launchParameters.SecondaryPort, accessToken, isPrimary);
				return ddTask;
			}
			catch
			{
				cancellationTokenSource.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Handle a crash or exit of a server
		/// </summary>
		/// <param name="serverTask">The <see cref="Task{TResult}"/> resulting in the exit code of the server</param>
		/// <param name="isPrimary">If the ended server was the primary server</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the server was ended due to a <see cref="CancellationToken"/>, <see langword="false"/> otherwise</returns>
		async Task<bool> HandleServerCrashed(Task<int> serverTask, bool isPrimary, CancellationToken cancellationToken)
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
		}

		/// <summary>
		/// Main <see cref="Watchdog"/> loop
		/// </summary>
		/// <param name="launchParametersFactory">The <see cref="ILaunchParametersFactory"/> for the run</param>
		/// <param name="onSuccessfulStartup">The <see cref="TaskCompletionSource{TResult}"/> to be completed once the first server starts</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task Run(ILaunchParametersFactory launchParametersFactory, TaskCompletionSource<object> onSuccessfulStartup, CancellationToken cancellationToken)
		{
			if (await byond.GetVersion(cancellationToken).ConfigureAwait(false) == null)
				throw new InvalidOperationException("No byond version installed!");
			await byond.ClearCache(cancellationToken).ConfigureAwait(false);
			var accessToken = cryptographySuite.GetSecureString();

			var retries = 0;
			do
			{
				var retryDelay = (int)Math.Min(Math.Pow(2, retries), TimeSpan.FromHours(1).Milliseconds); //max of one hour
				await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);

				//load the event tcs' and get the initial launch parameters
				var primaryPrimedTcs = new TaskCompletionSource<object>();
				interop.OnServerPrimed(() => primaryPrimedTcs.SetResult(null));
				var initialLaunchParameters = await launchParametersFactory.GetLaunchParameters(cancellationToken).ConfigureAwait(false);
				//start the primary server
				var ddPrimaryTask = StartServer(initialLaunchParameters, onSuccessfulStartup, accessToken, true, cancellationToken, out CancellationTokenSource primaryCts);
				try
				{
					//wait to make sure we got this far
					await onSuccessfulStartup.Task.ConfigureAwait(false);
					onSuccessfulStartup = null;

					//wait for either the server to exit or be primed
					await Task.WhenAny(ddPrimaryTask, primaryPrimedTcs.Task).ConfigureAwait(false);

					if (ddPrimaryTask.IsCompleted)
					{
						if (await HandleServerCrashed(ddPrimaryTask, true, cancellationToken).ConfigureAwait(false))
							return;
						++retries;
						continue;
					}

					var launchParameters = initialLaunchParameters;
					Task<int> ddSecondaryTask = null;
					CancellationTokenSource secondaryCts = null;
					try
					{
						do
						{
							if (ddSecondaryTask == null)
								//start the secondary server
								ddSecondaryTask = StartServer(initialLaunchParameters, null, accessToken, false, cancellationToken, out secondaryCts);

							var newDmbTask = dmbFactory.OnNewerDmb();

							//now we wait for something to happen
							await Task.WhenAny(ddSecondaryTask, ddPrimaryTask, newDmbTask).ConfigureAwait(false);

							//some helpers
							void PrimaryRestart()
							{
								primaryCts.Dispose();
								ddPrimaryTask = StartServer(initialLaunchParameters, null, accessToken, true, cancellationToken, out primaryCts);
							}
							void SecondaryRestart()
							{
								ddSecondaryTask = null;
								secondaryCts.Dispose();
							};
							Task<bool> PrimaryCrash() => HandleServerCrashed(ddPrimaryTask, interop.SecondaryIsOther, cancellationToken);
							Task<bool> SecondaryCrash() => HandleServerCrashed(ddSecondaryTask, !interop.SecondaryIsOther, cancellationToken);

							//update available
							if (newDmbTask.IsCompleted)
							{
								//restart the other server but don't treat it as an error
								launchParameters = await launchParametersFactory.GetLaunchParameters(cancellationToken).ConfigureAwait(false);
								//restart other server
								if (interop.SecondaryIsOther)
								{
									secondaryCts.Cancel();
									await ddSecondaryTask.ConfigureAwait(false);
									cancellationToken.ThrowIfCancellationRequested();
									SecondaryRestart();
								}
								else
								{
									primaryCts.Cancel();
									await ddPrimaryTask.ConfigureAwait(false);
									cancellationToken.ThrowIfCancellationRequested();
									PrimaryRestart();
								}
								continue;
							}

							//crash of both servers
							if (ddSecondaryTask.IsCompleted && ddPrimaryTask.IsCompleted)
							{
								//catastrophic, start over
								var t1 = PrimaryCrash();
								await Task.WhenAll(t1, SecondaryCrash()).ConfigureAwait(false);
								if (t1.Result)
									return;
								++retries;
								continue;
							}

							//below this point: crash of single server

							//activate the other server and load new launch params
							var otherServerActivation = interop.ActivateOtherServer(cancellationToken);
							launchParameters = await launchParametersFactory.GetLaunchParameters(cancellationToken).ConfigureAwait(false);
							await otherServerActivation.ConfigureAwait(false);

							//crash of secondary server
							if (ddSecondaryTask.IsCompleted)
							{
								if (await SecondaryCrash().ConfigureAwait(false))
									return;
								SecondaryRestart();
							}
							//crash of primary server
							else
							{
								if (await PrimaryCrash().ConfigureAwait(false))
									return;
								PrimaryRestart();
							}
						} while (true);
					}
					finally
					{
						secondaryCts?.Dispose();
					}
				}
				finally
				{
					primaryCts.Dispose();
				}
			} while (true);
		}

		/// <inheritdoc />
		public async Task Start(ILaunchParametersFactory launchParametersFactory, CancellationToken cancellationToken)
		{
			TaskCompletionSource<object> taskCompletionSource;
			lock (this)
			{
				if (watchdogTask != null)
					throw new InvalidOperationException("Watchdog already running!");
				watchdogCancellationTokenSource?.Dispose();
				watchdogCancellationTokenSource = new CancellationTokenSource();
				taskCompletionSource = new TaskCompletionSource<object>();
				watchdogTask = Run(launchParametersFactory, taskCompletionSource, watchdogCancellationTokenSource.Token);
			}

			using (cancellationToken.Register(() => watchdogCancellationTokenSource.Cancel()))
				await taskCompletionSource.Task.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task Stop()
		{
			lock(this)
			{
				if (watchdogTask == null)
					throw new InvalidOperationException("Watchdog not running!");
				watchdogCancellationTokenSource.Cancel();
				var task = watchdogTask;
				watchdogTask = null;
				return task;
			}
		}
	}
}
