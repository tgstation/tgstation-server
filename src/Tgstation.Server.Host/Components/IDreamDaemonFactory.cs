using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Factory for <see cref="IDreamDaemonControl"/>s
	/// </summary>
    interface IDreamDaemonFactory
    {
		IDreamDaemonControl LaunchNew(DreamDaemonLaunchParameters launchParameters);

		Task<IDreamDaemonControl> Reattach(DreamDaemonReattachInformation reattachInformation, CancellationToken cancellationToken);
    }
}
