using System.ServiceModel;

namespace TGServiceInterface.Components
{
	/// <summary>
	/// Used by DD to access the interop API with call()()
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
