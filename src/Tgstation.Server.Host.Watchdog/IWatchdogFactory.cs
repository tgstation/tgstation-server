namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// Factory for creating <see cref="IWatchdog"/>s
	/// </summary>
	public interface IWatchdogFactory
	{
		/// <summary>
		/// Create a <see cref="IWatchdog"/>
		/// </summary>
		/// <returns>A new <see cref="IWatchdog"/></returns>
		IWatchdog CreateWatchdog();
	}
}
