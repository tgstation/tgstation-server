using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// For creating filesystem symbolic links.
	/// </summary>
	interface ISymlinkFactory
	{
		/// <summary>
		/// If directory symlinks must be deleted as files would in the current environment.
		/// </summary>
		/// <remarks>This is because Linux symlinked directories must be deleted with <see cref="global::System.IO.File.Delete(string)"/>.</remarks>
		bool SymlinkedDirectoriesAreDeletedAsFiles { get; }

		/// <summary>
		/// Create a symbolic link.
		/// </summary>
		/// <param name="targetPath">The path to the hard target.</param>
		/// <param name="linkPath">The path to the link.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CreateSymbolicLink(string targetPath, string linkPath, CancellationToken cancellationToken);
	}
}
