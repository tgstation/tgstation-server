namespace Tgstation.Server.Host.Watchdog
{
    /// <inheritdoc />
    public sealed class WatchdogFactory : IWatchdogFactory
    {
        /// <inheritdoc />
        public IWatchdog CreateWatchdog() => new Watchdog();
    }
}
