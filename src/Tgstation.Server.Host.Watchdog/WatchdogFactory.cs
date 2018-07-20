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
        public IWatchdog CreateWatchdog(ILoggerFactory loggerFactory) => new Watchdog(new ServerFactory(), RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? (IActiveAssemblyDeleter)new WindowsActiveAssemblyDeleter() : new PosixActiveAssemblyDeleter(), new IsolatedAssemblyContextFactory(), loggerFactory?.CreateLogger<Watchdog>() ?? throw new ArgumentNullException(nameof(loggerFactory)));
    }
}
