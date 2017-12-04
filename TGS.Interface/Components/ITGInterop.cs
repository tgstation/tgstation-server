using System.Threading.Tasks;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Used by DreamDaemon to access the interop API with call()(). Restrictions are in place so that only a DreamDaemon instance launched by the service can use this API
	/// </summary>
	public interface ITGInterop : ITGComponent
	{
		/// <summary>
		/// Called from /world/ExportService(command)
		/// </summary>
		/// <param name="command">The command to run</param>
		/// <returns>A <see cref="Task"/> representing the operation</returns>
		Task InteropMessage(string command);
	}
}
