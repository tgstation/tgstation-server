using System.Collections.Generic;

namespace TGS.Interface.Components
{
	/// <summary>
	/// For modifying the in game config
	/// Most if not all of these will not apply until the next server reboot
	/// </summary>
	public interface ITGConfig
	{
		/// <summary>
		/// Returns the file contents of the specified server directory
		/// Subdirectories will be prefixed with '/'
		/// </summary>
		/// <param name="subpath">Subdirectory to enumerate, enumerates the root directory if null</param>
		/// <param name="error">null on success, error message on failure</param>
		/// <param name="unauthorized">This will be true if error is set to a message that indicates the current user does not have access to the specified file</param>
		/// <returns>A list of files in the enumerated static directory on success, null on failure</returns>
		IList<string> ListStaticDirectory(string subpath, out string error, out bool unauthorized);

		/// <summary>
		/// Read from a static file
		/// </summary>
		/// <param name="staticRelativePath">The path from the Static dir. E.g. config/config.txt</param>
		/// <param name="repo">if true, the file will be read from the repository instead of the static dir</param>
		/// <param name="error">null on success, error message on failure</param>
		/// <param name="unauthorized">This will be true if error is set to a message that indicates the current user does not have access to the specified file</param>
		/// <returns>The full text of the file on success, null on failure</returns>
		/// <exception cref="CommunicationException">Along with implied disconnect exceptions, if the file exceeds transfer limits</exception>
		string ReadText(string staticRelativePath, bool repo, out string error, out bool unauthorized);

		/// <summary>
		/// Write to a static file
		/// </summary>
		/// <param name="staticRelativePath">The path from the Static dir. E.g. config/config.txt</param>
		/// <param name="data">The full text of the config file</param>
		/// <param name="unauthorized">This will be true if error is set to a message that indicates the current user does not have access to the specified file</param>
		/// <returns>null on success, error message on failure</returns>
		/// <exception cref="CommunicationException">Along with implied disconnect exceptions, if the file exceeds transfer limits</exception>
		string WriteText(string staticRelativePath, string data, out bool unauthorized);

		/// <summary>
		/// Deletes the target static file
		/// </summary>
		/// <param name="staticRelativePath">The path from the Static dir. E.g. config/config.txt</param>
		/// <param name="unauthorized">This will be true if error is set to a message that indicates the current user does not have access to the specified file</param>
		/// <returns>null on success, error message on failure</returns>
		string DeleteFile(string staticRelativePath, out bool unauthorized);
	}
}
