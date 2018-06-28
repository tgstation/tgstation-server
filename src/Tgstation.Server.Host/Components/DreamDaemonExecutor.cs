using System;
using System.Diagnostics;
using System.Globalization;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

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

		/// <inheritdoc />
		public IDreamDaemonSession AttachToDreamDaemon(int processId) => new DreamDaemonSession(Process.GetProcessById(processId));

		/// <inheritdoc />
		public IDreamDaemonSession RunDreamDaemon(DreamDaemonLaunchParameters launchParameters, string dreamDaemonPath, IDmbProvider dmbProvider, string parameters, bool useSecondaryPort, bool useSecondaryDirectory)
		{
			if (launchParameters == null)
				throw new ArgumentNullException(nameof(launchParameters));
			if (dreamDaemonPath == null)
				throw new ArgumentNullException(nameof(dreamDaemonPath));
			if (dmbProvider == null)
				throw new ArgumentNullException(nameof(dmbProvider));
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			var proc = new Process();

			try
			{
				proc.StartInfo.FileName = dreamDaemonPath;
				proc.StartInfo.WorkingDirectory = useSecondaryDirectory ? dmbProvider.SecondaryDirectory : dmbProvider.PrimaryDirectory;

				proc.StartInfo.Arguments = String.Format(CultureInfo.InvariantCulture, "{0} -port {1} {2}-close -{3} -verbose -public -params \"{4}\"",
					dmbProvider.DmbName,
					useSecondaryPort ? launchParameters.SecondaryPort : launchParameters.PrimaryPort,
					launchParameters.AllowWebClient.Value ? "-webclient " : String.Empty,
					SecurityWord(launchParameters.SecurityLevel.Value),
					parameters);

				proc.Start();

				return new DreamDaemonSession(proc);
			}
			catch
			{
				proc.Dispose();
				throw;
			}
		}
	}
}
