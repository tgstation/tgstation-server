using System.ServiceModel;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Metadata for a server instance
	/// </summary>
	[ServiceContract]
	public interface ITGInstance
	{
		/// <summary>
		/// Return the directory of the server on the host machine
		/// </summary>
		/// <returns>The path to the directory on success, null on failure</returns>
		[OperationContract]
		string ServerDirectory();

		/// <summary>
		/// Retrieve's the service's version
		/// </summary>
		/// <returns>The service's version</returns>
		[OperationContract]
		string Version();
	}
}
