using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// Factory for creating <see cref="IWatchdog"/>s.
	/// </summary>
	public interface IWatchdogFactory
	{
		/// <summary>
		/// Create a <see cref="IWatchdog"/>.
		/// </summary>
		/// <param name="signalChecker">The <see cref="ISignalChecker"/> to use for relaying signals.</param>
		/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for error reporting.</param>
		/// <returns>A new <see cref="IWatchdog"/>.</returns>
		IWatchdog CreateWatchdog(ISignalChecker signalChecker, ILoggerFactory loggerFactory);
	}
}
