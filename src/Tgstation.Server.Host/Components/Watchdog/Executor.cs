using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class Executor : IExecutor
	{
		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="Executor"/>
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Executor"/>
		/// </summary>
		readonly ILogger<Executor> logger;

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
		/// Construct an <see cref="Executor"/>
		/// </summary>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Executor(IProcessExecutor processExecutor, ILogger<Executor> logger)
		{
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public ISession AttachToDreamDaemon(int processId, IByondExecutableLock byondLock) => new Session(processExecutor.GetProcess(processId), byondLock);

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

			var fileName = byondLock.DreamDaemonPath;
			var workingDirectory = useSecondaryDirectory ? dmbProvider.SecondaryDirectory : dmbProvider.PrimaryDirectory;

			var arguments = String.Format(CultureInfo.InvariantCulture, "{0} -port {1} {2}-close -{3} -verbose -public -params \"{4}\"",
				dmbProvider.DmbName,
				useSecondaryPort ? launchParameters.SecondaryPort : launchParameters.PrimaryPort,
				launchParameters.AllowWebClient.Value ? "-webclient " : String.Empty,
				SecurityWord(launchParameters.SecurityLevel.Value),
				parameters);

			logger.LogTrace("Running DreamDaemon in {0}: {1} {2}", workingDirectory, fileName, arguments);

			var proc = processExecutor.LaunchProcess(fileName, workingDirectory, arguments);

			return new Session(proc, byondLock);
		}
	}
}
