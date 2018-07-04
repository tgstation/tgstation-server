using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Watchdog
{
	interface IWatchdog : IDisposable
	{
		DreamDaemonLaunchParameters LaunchParameters { get; set; }

		Task<WatchdogLaunchResult> Launch(CancellationToken cancellationToken);
	}
}
