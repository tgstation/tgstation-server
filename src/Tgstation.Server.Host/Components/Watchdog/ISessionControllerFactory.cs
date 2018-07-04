using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Factory for <see cref="ISessionController"/>s
	/// </summary>
    interface ISessionControllerFactory
    {
		Task<ISessionController> LaunchNew(DreamDaemonLaunchParameters launchParameters, IDmbProvider dmbProvider, bool primaryPort, bool primaryDirectory, bool apiValidate, CancellationToken cancellationToken);

		Task<ISessionController> Reattach(ReattachInformation reattachInformation, CancellationToken cancellationToken);
    }
}
