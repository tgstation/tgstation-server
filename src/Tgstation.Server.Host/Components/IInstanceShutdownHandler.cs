using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For handling <see cref="IInstance"/> shutdowns
	/// </summary>
    public interface IInstanceShutdownHandler
	{
		//TODO
		/// <summary>
		/// OMG 
		/// </summary>
		/// <param name="launchParameters"></param>
		/// <param name="accessToken"></param>
		/// <param name="pid"></param>
		/// <param name="primary"></param>
		/// <returns></returns>
		Task<bool> PreserveActiveExecutablesIfNecessary(DreamDaemonLaunchParameters launchParameters, string accessToken, int pid, bool primary);
	}
}
