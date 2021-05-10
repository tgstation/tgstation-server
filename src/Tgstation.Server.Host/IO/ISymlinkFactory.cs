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
		/// Create a symbolic link.
		/// </summary>
		/// <param name="targetPath">The path to the hard target.</param>
		/// <param name="linkPath">The path to the link.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CreateSymbolicLink(string targetPath, string linkPath, CancellationToken cancellationToken);
	}
}
