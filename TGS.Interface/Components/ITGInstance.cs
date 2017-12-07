using System.Threading.Tasks;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Metadata for a server instance
	/// </summary>
	public interface ITGInstance : ITGComponent
	{
		/// <summary>
		/// Return the directory of the server on the host machine
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in the path to the directory on success, null on failure</returns>
		Task<string> ServerDirectory();

		/// <summary>
		/// Retrieve's the service's version
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in the service's version</returns>
		Task<string> Version();
	}
}
