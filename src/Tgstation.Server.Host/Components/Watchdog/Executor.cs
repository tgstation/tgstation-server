using System;
using System.Diagnostics;
using System.Globalization;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class Executor : IExecutor
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

		/// <inheritdoc />
		public ISession AttachToDreamDaemon(int processId, IByondExecutableLock byondLock) => new Session(Process.GetProcessById(processId), byondLock);

		/// <inheritdoc />
		public ISession RunDreamDaemon(DreamDaemonLaunchParameters launchParameters, IByondExecutableLock byondLock, IDmbProvider dmbProvider, string parameters, bool useSecondaryPort, bool useSecondaryDirectory)
		{
			if (launchParameters == null)
				throw new ArgumentNullException(nameof(launchParameters));
			if (byondLock == null)
				throw new ArgumentNullException(nameof(byondLock));
			if (dmbProvider == null)
				throw new ArgumentNullException(nameof(dmbProvider));
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			var proc = new Process();

			try
			{
				proc.StartInfo.FileName = byondLock.DreamDaemonPath;
				proc.StartInfo.WorkingDirectory = useSecondaryDirectory ? dmbProvider.SecondaryDirectory : dmbProvider.PrimaryDirectory;

				proc.StartInfo.Arguments = String.Format(CultureInfo.InvariantCulture, "{0} -port {1} {2}-close -{3} -verbose -public -params \"{4}\"",
					dmbProvider.DmbName,
					useSecondaryPort ? launchParameters.SecondaryPort : launchParameters.PrimaryPort,
					launchParameters.AllowWebClient.Value ? "-webclient " : String.Empty,
					SecurityWord(launchParameters.SecurityLevel.Value),
					parameters);

				proc.Start();

				return new Session(proc, byondLock);
			}
			catch
			{
				proc.Dispose();
				throw;
			}
		}
	}
}
