using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Watchdog
{
    interface ISessionBase : IDisposable
	{
		/// <summary>
		/// A <see cref="Task"/> that completes when DreamDaemon starts pumping the windows message queue after loading a .dmb or when it crashes
		/// </summary>
		Task<LaunchResult> LaunchResult { get; }

		/// <summary>
		/// A <see cref="Task"/> representing the lifetime of the <see cref="System.Diagnostics.Process"/> and resulting in the <see cref="System.Diagnostics.Process.ExitCode"/>
		/// </summary>
		Task<int> Lifetime { get; }
	}
}
