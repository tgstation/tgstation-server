using System.Threading.Tasks;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Interface for managing the service
	/// </summary>
	public interface ITGServer : ITGComponent
	{
		/// <summary>
		/// Next stop of the service will not close DD and sets a flag for it to reattach once it restarts
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the operation</returns>
		Task PrepareForUpdate();

		/// <summary>
		/// Get the port used for remote operation
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in the port used for remote operation</returns>
		Task<ushort> RemoteAccessPort();

		/// <summary>
		/// Set the port used for remote operation
		/// Requires a service restart to take effect
		/// </summary>
		/// <param name="port">The new port to use for remote operation</param>
		/// <returns>A <see cref="Task"/> that results in <see langword="null"/> on success, error message on failure</returns>
		Task<string> SetRemoteAccessPort(ushort port);

		/// <summary>
		/// Retrieve's the service's version
		/// </summary>
		/// <returns>A <see cref="Task"/> that results in the server's version <see cref="string"/></returns>
		Task<string> Version();

		/// <summary>
		/// Sets the path to the python 2.7 installation
		/// </summary>
		/// <param name="path">The new path</param>
		/// <returns>A <see cref="Task"/> that results in <see langword="true"/> if the path exists, <see langword="false"/> otherwise</returns>
		Task<bool> SetPythonPath(string path);

		/// <summary>
		/// Gets the path to the python 2.7 installation
		/// </summary>
		/// <returns>A <see cref="Task"/> that results in the path to the python 2.7 installation</returns>
		Task<string> PythonPath();
	}
}
