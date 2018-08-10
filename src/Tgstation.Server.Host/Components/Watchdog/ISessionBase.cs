using System.Threading.Tasks;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
    interface ISessionBase : IProcessBase
	{
		/// <summary>
		/// A <see cref="Task"/> that completes when DreamDaemon starts pumping the windows message queue after loading a .dmb or when it crashes
		/// </summary>
		Task<LaunchResult> LaunchResult { get; }
	}
}
