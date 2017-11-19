using System.Collections.Generic;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	interface IRepositoryManager : ITGRepository, IRepoConfigProvider
	{
		/// <summary>
		/// Copy the repository (without the .git folder) to the target <paramref name="destination"/> while excluding <paramref name="ignorePaths"/>
		/// </summary>
		/// <param name="destination">The directory to copy to</param>
		/// <param name="ignorePaths">Root level paths to ignore</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string CopyTo(string destination, IEnumerable<string> ignorePaths);

		/// <summary>
		/// Copy only root level items in <paramref name="onlyPaths"/> to to the target <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">The directory to copy to</param>
		/// <param name="onlyPaths">Root level paths to copy</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string CopyToRestricted(string destination, IEnumerable<string> onlyPaths);
	}
}
