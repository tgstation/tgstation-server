namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog
	{
		/// <summary>
		/// Construct a <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="byond"></param>
		/// <param name="chat"></param>
		/// <param name="sessionManagerFactory"></param>
		/// <param name="dmbFactory"></param>
		/// <param name="eventConsumer"></param>
		/// <param name="interopRegistrar"></param>
		public Watchdog(IByond byond, IChat chat, ISessionManagerFactory sessionManagerFactory, IDmbFactory dmbFactory, IEventConsumer eventConsumer, IInteropRegistrar interopRegistrar)
		{
		}
	}
}
