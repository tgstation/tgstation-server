using System.Collections.Generic;
using System.ServiceModel;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Interface for managing the service
	/// </summary>
	[ServiceContract]
	public interface ITGSService
	{

		/// <summary>
		/// Next stop of the service will not close DD and sets a flag for it to reattach once it restarts
		/// </summary>
		[OperationContract]
		void PrepareForUpdate();

		/// <summary>
		/// Get the port used for remote operation
		/// </summary>
		/// <returns>The port used for remote operation</returns>
		[OperationContract]
		ushort RemoteAccessPort();

		/// <summary>
		/// Set the port used for remote operation
		/// Requires a service restart to take effect
		/// </summary>
		/// <param name="port">The new port to use for remote operation</param>
		/// <returns>null on success, error message on failure</returns>
		[OperationContract]
		string SetRemoteAccessPort(ushort port);

		/// <summary>
		/// Retrieve's the service's version
		/// </summary>
		/// <returns>The service's version</returns>
		[OperationContract]
		string Version();

		/// <summary>
		/// Sets the path to the python 2.7 installation
		/// </summary>
		/// <param name="path">The new path</param>
		/// <returns><see langword="true"/> if the path exists, <see langword="false"/> otherwise</returns>
		[OperationContract]
		bool SetPythonPath(string path);

		/// <summary>
		/// Gets the path to the python 2.7 installation
		/// </summary>
		/// <returns>The path to the python 2.7 installation</returns>
		[OperationContract]
		string PythonPath();
	}
}
