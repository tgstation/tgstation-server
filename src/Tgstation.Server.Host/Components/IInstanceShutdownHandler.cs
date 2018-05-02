using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components
{
    interface IInstanceShutdownHandler
	{
		Task<bool> PreserveActiveExecutablesIfNecessary(DreamDaemonLaunchParameters launchParameters, string accessToken, int pid, bool primary);
	}
}
