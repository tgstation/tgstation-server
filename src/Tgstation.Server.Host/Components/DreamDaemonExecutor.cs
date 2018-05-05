using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Models;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class DreamDaemonExecutor : IDreamDaemonExecutor
	{
		/// <summary>
		/// Change a given <paramref name="securityLevel"/> into the appropriate DreamDaemon command line word
		/// </summary>
		/// <param name="securityLevel">The <see cref="DreamDaemonSecurity"/> level to change</param>
		/// <returns>A <see cref="string"/> representation of the command line parameter</returns>
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

		/// <summary>
		/// The <see cref="IInstanceShutdownHandler"/> for the <see cref="DreamDaemonExecutor"/>
		/// </summary>
		readonly IInstanceShutdownHandler instanceShutdownMethod;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DreamDaemonExecutor"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// Construct a <see cref="DreamDaemonExecutor"/>
		/// </summary>
		/// <param name="instanceShutdownMethod">The value of <see cref="instanceShutdownMethod"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		public DreamDaemonExecutor(IInstanceShutdownHandler instanceShutdownMethod, IIOManager ioManager)
		{
			this.instanceShutdownMethod = instanceShutdownMethod ?? throw new ArgumentNullException(nameof(instanceShutdownMethod));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
		}

		/// <inheritdoc />
		public async Task<int> RunDreamDaemon(DreamDaemonLaunchParameters launchParameters, TaskCompletionSource<object> onSuccessfulStartup, string dreamDaemonPath, IDmbProvider dmbProvider, InteropInfo interopInfo, bool alwaysKill, CancellationToken cancellationToken)
		{
			if (launchParameters == null)
				throw new ArgumentNullException(nameof(launchParameters));
			if (dreamDaemonPath == null)
				throw new ArgumentNullException(nameof(dreamDaemonPath));
			if (dmbProvider == null)
				throw new ArgumentNullException(nameof(dmbProvider));
			if (interopInfo == null)
				throw new ArgumentNullException(nameof(interopInfo));

			if (interopInfo.NextPort != launchParameters.PrimaryPort && interopInfo.NextPort != launchParameters.SecondaryPort)
				throw new ArgumentOutOfRangeException(nameof(interopInfo), interopInfo, "interopInfo.NextPort must match primary or secondary ports!");

			var isPrimary = interopInfo.NextPort == launchParameters.SecondaryPort;

			//serialize the interop info to the json
			var jsonPath = String.Concat(Guid.NewGuid(), ".tgs.json");

			var json = JsonConvert.SerializeObject(interopInfo);


			using (var proc = new Process())
			{
				proc.StartInfo.FileName = dreamDaemonPath;
				proc.StartInfo.WorkingDirectory = isPrimary ? dmbProvider.PrimaryDirectory : dmbProvider.SecondaryDirectory;
				await ioManager.WriteAllBytes(ioManager.ConcatPath(proc.StartInfo.WorkingDirectory, jsonPath), Encoding.UTF8.GetBytes(json), cancellationToken).ConfigureAwait(false);

				proc.StartInfo.Arguments = String.Format(CultureInfo.InvariantCulture, "{0} -port {1} {2}-close -{3} -verbose -public -params \"{4}={5}&{6}={7}\"", 
					dmbProvider.DmbName,
					isPrimary ? launchParameters.PrimaryPort : launchParameters.SecondaryPort,
					launchParameters.AllowWebClient ? "-webclient " : String.Empty,
					SecurityWord(launchParameters.SecurityLevel),
					DreamDaemonParameters.HostVersion, Application.Version,
					DreamDaemonParameters.InfoJsonPath, jsonPath);

				proc.EnableRaisingEvents = true;
				var tcs = new TaskCompletionSource<object>();
				proc.Exited += (a, b) => tcs.SetResult(null);

				try
				{
					proc.Start();

					await Task.Factory.StartNew(() => proc.WaitForInputIdle(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

					onSuccessfulStartup?.SetResult(null);

					try
					{
						using (cancellationToken.Register(() => tcs.SetCanceled()))
							await tcs.Task.ConfigureAwait(false);
					}
					finally
					{
						if (!alwaysKill && !await instanceShutdownMethod.PreserveActiveExecutablesIfNecessary(launchParameters, interopInfo.AccessToken, proc.Id, isPrimary).ConfigureAwait(false))
						{
							proc.Kill();
							proc.WaitForExit();
						}
					}

					return proc.ExitCode;
				}
				catch (Exception e)
				{
					onSuccessfulStartup?.SetException(e);
					throw;
				}
			}
		}

	}
}
