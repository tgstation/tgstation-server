using System.ServiceModel;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Used by DreamDaemon to access the interop API with call()(). Restrictions are in place so that only a DreamDaemon instance launched by the service can use this API
	/// </summary>
	[ServiceContract]
	public interface ITGInterop
	{
		/// <summary>
		/// Called from /world/ExportService(command)
		/// </summary>
		/// <param name="command">The command to run</param>
		/// <returns><see langword="true"/> on success, <see langword="false"/> on failure</returns>
		[OperationContract]
		bool InteropMessage(string command);
	}
}
