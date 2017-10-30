using System.Collections.Generic;
using System.ServiceModel;

namespace TGServiceInterface.Components
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
		/// Retrieve's the service's version
		/// </summary>
		/// <returns>The service's version</returns>
		[OperationContract]
		string Version();

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
		/// List instances 
		/// </summary>
		/// <returns>A <see cref="IDictionary{TKey, TValue}"/> of instance names relating to their paths</returns>
		[OperationContract]
		IDictionary<string, string> ListInstances();

		/// <summary>
		/// Creates a new server instance
		/// </summary>
		/// <param name="Name">The name of the instance</param>
		/// <param name="path">The path to the instance</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string CreateInstance(string Name, string path);

		/// <summary>
		/// Registers an existing server instance
		/// </summary>
		/// <param name="path">The path to the instance</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string ImportInstance(string path);

		/// <summary>
		/// Checks if an instance is online
		/// </summary>
		/// <param name="Name">The name of the instance</param>
		/// <returns><see langword="true"/> if the Instance exists and is online, <see langword="false"/> otherwise</returns>
		[OperationContract]
		bool InstanceEnabled(string Name);

		/// <summary>
		/// Sets an instance's enabled status
		/// </summary>
		/// <param name="Name">The instance whom's status should be changed</param>
		/// <param name="enabled"><see langword="true"/> to enable the instance, <see langword="false"/> to disable it</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string SetInstanceEnabled(string Name, bool enabled);

		/// <summary>
		/// Renames an instance, this will restart the instance if it is enabled
		/// </summary>
		/// <param name="name">The current name of the instance</param>
		/// <param name="new_name">The new name of the instance</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string RenameInstance(string name, string new_name);

		/// <summary>
		/// Disables and unregisters an instance, allowing the folder and data to be manipulated manually
		/// </summary>
		/// <param name="name">The instance to detach</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string DetachInstance(string name);

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
