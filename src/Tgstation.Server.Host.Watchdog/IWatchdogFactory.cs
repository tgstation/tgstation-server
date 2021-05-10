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
		/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for error reporting.</param>
		/// <returns>A new <see cref="IWatchdog"/>.</returns>
		IWatchdog CreateWatchdog(ILoggerFactory loggerFactory);
	}
}
