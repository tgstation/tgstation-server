using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Factory for <see cref="ISessionManager"/>s
	/// </summary>
    interface ISessionManagerFactory
    {
		ISessionManager LaunchNew(DreamDaemonLaunchParameters launchParameters);

		Task<ISessionManager> Reattach(ReattachInformation reattachInformation, CancellationToken cancellationToken);
    }
}
