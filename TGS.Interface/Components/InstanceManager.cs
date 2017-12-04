using System.Threading.Tasks;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Used for managing <see cref="ITGInstance"/>s
	/// </summary>
	public interface ITGInstanceManager : ITGComponent
	{
		/// <summary>
		/// Creates a new <see cref="ITGInstance"/>
		/// </summary>
		/// <param name="Name">The name of the instance</param>
		/// <param name="path">The path to the instance</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		Task<string> CreateInstance(string Name, string path);

		/// <summary>
		/// Registers an existing server instance
		/// </summary>
		/// <param name="path">The path to the instance</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		Task<string> ImportInstance(string path);

		/// <summary>
		/// Checks if an instance is online
		/// </summary>
		/// <param name="Name">The name of the instance</param>
		/// <returns><see langword="true"/> if the Instance exists and is online, <see langword="false"/> otherwise</returns>
		Task<bool> InstanceEnabled(string Name);

		/// <summary>
		/// Sets an instance's enabled status
		/// </summary>
		/// <param name="Name">The instance whom's status should be changed</param>
		/// <param name="enabled"><see langword="true"/> to enable the instance, <see langword="false"/> to disable it</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		Task<string> SetInstanceEnabled(string Name, bool enabled);

		/// <summary>
		/// Renames an instance, this will restart the instance if it is enabled
		/// </summary>
		/// <param name="name">The current name of the instance</param>
		/// <param name="new_name">The new name of the instance</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		Task<string> RenameInstance(string name, string new_name);

		/// <summary>
		/// Disables and unregisters an instance, allowing the folder and data to be manipulated manually
		/// </summary>
		/// <param name="name">The instance to detach</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		Task<string> DetachInstance(string name);
	}
}
