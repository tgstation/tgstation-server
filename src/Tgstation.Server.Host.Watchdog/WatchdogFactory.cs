using System;

using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.Watchdog
{
	/// <inheritdoc />
	public sealed class WatchdogFactory : IWatchdogFactory
	{
		/// <inheritdoc />
		public IWatchdog CreateWatchdog(ILoggerFactory loggerFactory) => new Watchdog(loggerFactory?.CreateLogger<Watchdog>() ?? throw new ArgumentNullException(nameof(loggerFactory)));
	}
}
