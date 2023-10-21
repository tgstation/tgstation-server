using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// A <see cref="IDmbProvider"/> that uses symlinks.
	/// </summary>
	class SwappableDmbProvider : IDmbProvider
	{
		/// <summary>
		/// The directory where the <see cref="baseProvider"/> is symlinked to.
		/// </summary>
		public const string LiveGameDirectory = "Live";

		/// <inheritdoc />
		public string DmbName => baseProvider.DmbName;

		/// <inheritdoc />
		public string Directory => IOManager.ResolvePath(LiveGameDirectory);

		/// <inheritdoc />
		public CompileJob CompileJob => baseProvider.CompileJob;

		/// <summary>
		/// If <see cref="MakeActive(CancellationToken)"/> has been run.
		/// </summary>
		public bool Swapped => swapped != 0;

		/// <summary>
		/// The <see cref="IIOManager"/> to use.
		/// </summary>
		protected IIOManager IOManager { get; }

		/// <summary>
		/// The <see cref="ISymlinkFactory"/> to use.
		/// </summary>
		protected ISymlinkFactory SymlinkFactory { get; }

		/// <summary>
		/// The <see cref="IDmbProvider"/> we are swapping for.
		/// </summary>
		readonly IDmbProvider baseProvider;

		/// <summary>
		/// Backing field for <see cref="Swapped"/>.
		/// </summary>
		volatile int swapped;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwappableDmbProvider"/> class.
		/// </summary>
		/// <param name="baseProvider">The value of <see cref="baseProvider"/>.</param>
		/// <param name="ioManager">The value of <see cref="IOManager"/>.</param>
		/// <param name="symlinkFactory">The value of <see cref="SymlinkFactory"/>.</param>
		public SwappableDmbProvider(IDmbProvider baseProvider, IIOManager ioManager, ISymlinkFactory symlinkFactory)
		{
			this.baseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
			IOManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			SymlinkFactory = symlinkFactory ?? throw new ArgumentNullException(nameof(symlinkFactory));
		}

		/// <inheritdoc />
		public virtual ValueTask DisposeAsync() => baseProvider.DisposeAsync();

		/// <inheritdoc />
		public void KeepAlive() => baseProvider.KeepAlive();

		/// <summary>
		/// Make the <see cref="SwappableDmbProvider"/> active by replacing the live link with our <see cref="CompileJob"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		public Task MakeActive(CancellationToken cancellationToken)
		{
			if (Interlocked.Exchange(ref swapped, 1) != 0)
				throw new InvalidOperationException("Already swapped!");

			return DoSwap(cancellationToken);
		}

		/// <summary>
		/// Should be <see langword="await"/>. before calling <see cref="MakeActive(CancellationToken)"/> to ensure the <see cref="SwappableDmbProvider"/> is ready to instantly swap. Can be called multiple times.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the preparation process.</returns>
		public virtual Task FinishActivationPreparation(CancellationToken cancellationToken)
			=> Task.CompletedTask;

		/// <summary>
		/// Perform the swapping action.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected virtual async Task DoSwap(CancellationToken cancellationToken)
		{
			if (SymlinkFactory.SymlinkedDirectoriesAreDeletedAsFiles)
				await IOManager.DeleteFile(LiveGameDirectory, cancellationToken);
			else
				await IOManager.DeleteDirectory(LiveGameDirectory, cancellationToken);

			await SymlinkFactory.CreateSymbolicLink(
				IOManager.ResolvePath(baseProvider.Directory),
				IOManager.ResolvePath(LiveGameDirectory),
				cancellationToken);
		}
	}
}
