namespace Tgstation.Server.Host.Watchdog
{
    /// <inheritdoc />
    public sealed class WatchdogFactory : IWatchdogFactory
    {
		internal static IOSIdentifier osIdentifier { get; set; } = new OSIdentifier();

        /// <inheritdoc />
        public IWatchdog CreateWatchdog() => new Watchdog(new ServerFactory(), osIdentifier.IsWindows ? (IActiveAssemblyDeleter)new WindowsActiveAssemblyDeleter() : new PosixActiveAssemblyDeleter(), new IsolatedAssemblyContextFactory());
    }
}
