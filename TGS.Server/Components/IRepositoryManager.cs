using System.Collections.Generic;
using System.Threading.Tasks;
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
		/// <returns>A <see cref="Task"/> that results in <see langword="null"/> on success, error message on failure</returns>
		Task<string> CopyTo(string destination, IEnumerable<string> ignorePaths);

		/// <summary>
		/// Copy only root level items in <paramref name="onlyPaths"/> to to the target <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">The directory to copy to</param>
		/// <param name="onlyPaths">Root level paths to copy</param>
		/// <returns>A <see cref="Task"/> that results in <see langword="null"/> on success, error message on failure</returns>
		Task<string> CopyToRestricted(string destination, IEnumerable<string> onlyPaths);

		/// <summary>
		/// Creates a date and timestamped tag of the current HEAD
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string CreateBackup();

		/// <summary>
		/// Updates the live tracking branch with the staged <paramref name="newSha"/>
		/// </summary>
		/// <param name="newSha">The commit SHA that was staged</param>
		void UpdateLiveSha(string newSha);

		/// <summary>
		/// Fetches the origin and merges it into the current branch
		/// </summary>
		/// <param name="reset">If <see langword="true"/>, the operation will perform a hard reset instead of a merge</param>
		/// <param name="successOnUpToDate">If <see langword="true"/>, a return value of <see cref="RepositoryManager.RepoErrorUpToDate"/> will be changed to <see langword="null"/></param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string UpdateImpl(bool reset, bool successOnUpToDate);
	}
}
