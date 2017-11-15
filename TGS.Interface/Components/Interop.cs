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

		/// <summary>
		/// Sends a message to everyone on the server
		/// </summary>
		/// <param name="msg">The message to send</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string WorldAnnounce(string msg);

		/// <summary>
		/// Returns the number of connected players. Requires game to use API version >= 3.1.0.1
		/// </summary>
		/// <returns>The number of connected players or -1 on error</returns>
		[OperationContract]
		int PlayerCount();
	}
}
