using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace TGServiceInterface
{
	/// <summary>
	/// For modifying the in game config
	/// Most if not all of these will not apply until the next server reboot
	/// </summary>
	[ServiceContract]
	public interface ITGConfig
	{
		/// <summary>
		/// Return the directory of the server on the host machine
		/// </summary>
		/// <returns>The path to the directory on success, null on failure</returns>
		[OperationContract]
		string ServerDirectory();

		/// <summary>
		/// Moves the entire server installation, requires no operations to be running
		/// </summary>
		/// <param name="new_location">The new path to place the server</param>
		/// <returns>null on success, error message on failure</returns>
		[OperationContract]
		string MoveServer(string new_location);

		/// <summary>
		/// For when you really just need to see the raw data of the config
		/// </summary>
		/// <param name="staticRelativePath">The path from the Static dir. E.g. config/config.txt</param>
		/// <param name="repo">if true, the file will be read from the repository instead of the static dir</param>
		/// <param name="error">null on success, error message on failure</param>
		/// <returns>The full text of the file on success, null on failure</returns>
		[OperationContract]
		string ReadText(string staticRelativePath, bool repo, out string error);

		/// <summary>
		/// For when you really just need to set the raw data of the config
		/// </summary>
		/// <param name="configRelativePath">The path from the configDir. E.g. config.txt</param>
		/// <param name="data">The full text of the config file</param>
		/// <returns>null on success, error message on failure</returns>
		[OperationContract]
		string WriteText(string staticRelativePath, string data);
	}
}
