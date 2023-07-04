using System;

using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.Watchdog
{
	/// <inheritdoc />
	public sealed class WatchdogFactory : IWatchdogFactory
	{
		/// <inheritdoc />
		public IWatchdog CreateWatchdog(
			ISignalChecker signalChecker,
			ILoggerFactory loggerFactory) => new Watchdog(
				signalChecker ?? throw new ArgumentNullException(nameof(signalChecker)),
				loggerFactory?.CreateLogger<Watchdog>() ?? throw new ArgumentNullException(nameof(loggerFactory)));
	}
}
