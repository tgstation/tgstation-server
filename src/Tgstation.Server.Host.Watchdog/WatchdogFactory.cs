using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.Watchdog
{
    /// <inheritdoc />
    public sealed class WatchdogFactory : IWatchdogFactory
    {
        /// <inheritdoc />
        public IWatchdog CreateWatchdog() => new Watchdog(new ServerFactory(), RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? (IActiveAssemblyDeleter)new WindowsActiveAssemblyDeleter() : new PosixActiveAssemblyDeleter());
    }
}
