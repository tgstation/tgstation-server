using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// A <see cref="IDmbProvider"/> that uses symlinks.
	/// </summary>
	sealed class SymlinkDmbProvider : SwappableDmbProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SymlinkDmbProvider"/> class.
		/// </summary>
		/// <param name="baseProvider">The <see cref="IDmbProvider"/> for the <see cref="SwappableDmbProvider"/>.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="SwappableDmbProvider"/>.</param>
		/// <param name="symlinkFactory">The <see cref="ISymlinkFactory"/> for the <see cref="SwappableDmbProvider"/>.</param>
		public SymlinkDmbProvider(
			IDmbProvider baseProvider,
			IIOManager ioManager,
			ISymlinkFactory symlinkFactory)
			: base(baseProvider, ioManager, symlinkFactory)
		{
		}

		/// <inheritdoc />
		public override Task FinishActivationPreparation(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		protected override async Task DoSwap(CancellationToken cancellationToken)
		{
			if (SymlinkFactory.SymlinkedDirectoriesAreDeletedAsFiles)
				await IOManager.DeleteFile(LiveGameDirectory, cancellationToken);
			else
				await IOManager.DeleteDirectory(LiveGameDirectory, cancellationToken);

			await SymlinkFactory.CreateSymbolicLink(
				IOManager.ResolvePath(BaseProvider.Directory),
				IOManager.ResolvePath(LiveGameDirectory),
				cancellationToken);
		}
	}
}
