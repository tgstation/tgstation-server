using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Core;
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
		readonly IIOManager ioManager;

		readonly bool autoStart;

		readonly SemaphoreSlim semaphore;

		DreamDaemonLaunchParameters currentLaunchParameters;

		CancellationTokenSource watchdogCancellationTokenSource;
		Task watchdogTask;

		public DreamDaemon(IEventConsumer eventConsumer, IByond byond, ICryptographySuite cryptographySuite, IInterop interop, IInstanceShutdownMethod instanceShutdownMethod, DreamDaemonSettings initialSettings)
		{
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.interop = interop ?? throw new ArgumentNullException(nameof(interop));
			this.instanceShutdownMethod = instanceShutdownMethod ?? throw new ArgumentNullException(nameof(instanceShutdownMethod));

			interop.SetServerControlHandler(OnServerControl);

			currentLaunchParameters = initialSettings ?? throw new ArgumentNullException(nameof(initialSettings));
			autoStart = initialSettings.AutoStart;

			semaphore = new SemaphoreSlim(1);
		}

		public void Dispose()
		{
			if (watchdogCancellationTokenSource != null)
				watchdogCancellationTokenSource.Dispose();
			semaphore.Dispose();
		}

		Task OnServerControl(ServerControlEventArgs serverControlEventArgs, CancellationToken cancellationToken)
		{
			lock (this)
			{
				CancelGracefulActions();
				if (serverControlEventArgs.ProcessRestart)
					return Restart(serverControlEventArgs.Graceful, cancellationToken);
				if(!serverControlEventArgs.ServerReboot)
					return Terminate(serverControlEventArgs.Graceful, cancellationToken);
				return Task.CompletedTask;
			}
		}

		static string SecurityWord(DreamDaemonSecurity securityLevel)
		{
			switch (securityLevel)
			{
				case DreamDaemonSecurity.Safe:
					return "safe";
				case DreamDaemonSecurity.Trusted:
					return "trusted";
				case DreamDaemonSecurity.Ultrasafe:
					return "ultrasafe";
				default:
					throw new ArgumentOutOfRangeException(nameof(securityLevel), securityLevel, String.Format(CultureInfo.InvariantCulture, "Bad DreamDaemon security level: {0}", securityLevel));
			}
		}

		async Task Watchdog(DreamDaemonLaunchParameters launchParameters, TaskCompletionSource<object> onSuccessfulStartup, CancellationToken cancellationToken)
		{
			async Task RunOnce(string executablePath)
			{
				if (await byond.GetVersion(cancellationToken).ConfigureAwait(false) == null)
					throw new InvalidOperationException("No byond version installed!");

				await byond.ClearCache(cancellationToken).ConfigureAwait(false);
				using (var proc = new Process())
				{
					var accessToken = cryptographySuite.GetSecureString();
					proc.StartInfo.FileName = executablePath;
					proc.StartInfo.Arguments = String.Format(CultureInfo.InvariantCulture, "{0} -port {1} {5}-close -verbose -params \"server_service={3}&server_service_version={4}&{6}={7}\" -{2} -public", DMB, launchParameters.PrimaryPort, SecurityWord(launchParameters.SecurityLevel), accessToken, Application.Version, launchParameters.AllowWebClient ? "-webclient " : String.Empty);

					proc.EnableRaisingEvents = true;
					var tcs = new TaskCompletionSource<object>();
					proc.Exited += (a, b) => tcs.SetResult(null);

					try
					{
						proc.Start();

						await Task.Factory.StartNew(() => proc.WaitForInputIdle(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

						if (onSuccessfulStartup != null)
						{
							onSuccessfulStartup.SetResult(null);
							onSuccessfulStartup = null;
						}

						try
						{
							using (cancellationToken.Register(() => tcs.SetCanceled()))
								await tcs.Task.ConfigureAwait(false);
						}
						catch (OperationCanceledException)
						{
							return;
						}
						finally
						{
							if (!instanceShutdownMethod.GracefulShutdown)
							{
								proc.Kill();
								proc.WaitForExit();
							}
						}
					}
					catch (Exception e)
					{
						onSuccessfulStartup?.SetException(e);
						throw;
					}
					await eventConsumer.HandleEvent(proc.ExitCode != 0 ? EventType.DDCrash : EventType.DDExit, null, cancellationToken).ConfigureAwait(false);
				}
			};

			do
			{
				await byond.UseExecutable(RunOnce, false, true).ConfigureAwait(false);
			} while (!cancellationToken.IsCancellationRequested);
		}

		public void CancelGracefulActions()
		{
			lock (this)
			{
				SoftRebooting = false;
				SoftStopping = false;
			}
		}

		public async Task ChangeSettings(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			Task launchTask;
			lock (this)
			{
				currentLaunchParameters = launchParameters;
				if (!Running)
					launchTask = Launch(launchParameters, cancellationToken);
				else
					launchTask = Restart(true, cancellationToken);
			}
			await launchTask.ConfigureAwait(false);
		}

		public async Task Launch(DreamDaemonLaunchParameters launchParameters, CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (Running)
					throw new InvalidOperationException("DreamDaemon already running!");
				Running = true;
				await eventConsumer.HandleEvent(EventType.DDLaunched, null, cancellationToken).ConfigureAwait(false);
				watchdogCancellationTokenSource?.Dispose();
				watchdogCancellationTokenSource = new CancellationTokenSource();
				var startupTcs = new TaskCompletionSource<object>();
				watchdogTask = Watchdog(currentLaunchParameters, startupTcs, watchdogCancellationTokenSource.Token);
				await startupTcs.Task.ConfigureAwait(false);
			}
			finally
			{
				semaphore.Release();
			}
		}


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

		public Task StartAsync(CancellationToken cancellationToken) => autoStart ? Launch(currentLaunchParameters, cancellationToken) : Task.CompletedTask;

		public Task StopAsync(CancellationToken cancellationToken) => Terminate(false, cancellationToken);

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
