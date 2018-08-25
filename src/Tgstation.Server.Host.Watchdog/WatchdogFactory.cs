using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.Watchdog
{
	/// <inheritdoc />
	public sealed class WatchdogFactory : IWatchdogFactory
	{
		/// <inheritdoc />
		[ExcludeFromCodeCoverage]
		public IWatchdog CreateWatchdog(ILoggerFactory loggerFactory) => new Watchdog(loggerFactory?.CreateLogger<Watchdog>() ?? throw new ArgumentNullException(nameof(loggerFactory)));
	}
}
