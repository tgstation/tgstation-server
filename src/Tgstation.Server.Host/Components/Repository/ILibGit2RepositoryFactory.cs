using LibGit2Sharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Factory for creating <see cref="LibGit2Sharp.IRepository"/>s.
	/// </summary>
	interface ILibGit2RepositoryFactory : ICredentialsProvider
	{
		/// <summary>
		/// Create an in-memeory <see cref="LibGit2Sharp.IRepository"/>.
		/// </summary>
		/// <returns>A new in-memory <see cref="LibGit2Sharp.IRepository"/>.</returns>
		LibGit2Sharp.IRepository CreateInMemory();

		/// <summary>
		/// Load a <see cref="LibGit2Sharp.IRepository"/> from a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The full path to the <see cref="LibGit2Sharp.IRepository"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the loaded <see cref="LibGit2Sharp.IRepository"/>.</returns>
		Task<LibGit2Sharp.IRepository> CreateFromPath(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Clone a remote <see cref="LibGit2Sharp.IRepository"/>.
		/// </summary>
		/// <param name="url">The <see cref="Uri"/> of the remote.</param>
		/// <param name="cloneOptions">The <see cref="CloneOptions"/>.</param>
		/// <param name="path">The full path to the cloned <see cref="LibGit2Sharp.IRepository"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task Clone(Uri url, CloneOptions cloneOptions, string path, CancellationToken cancellationToken);
	}
}
