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
		/// Called from /world/ExportService(command). Can only be used by DreamDaemon
		/// </summary>
		/// <param name="command">The command to run</param>
		/// <returns><see langword="true"/> on success, <see langword="false"/> on failure</returns>
		[OperationContract]
		bool InteropMessage(string command);

		/// <summary>
		/// Sends a message to everyone on the server
		/// </summary>
		/// <param name="msg">The message to send</param>
		[OperationContract]
		void WorldAnnounce(string msg);

		/// <summary>
		/// Returns the number of connected players
		/// </summary>
		/// <returns>The number of connected players or -1 on error</returns>
		[OperationContract]
		int PlayerCount();
	}
}
