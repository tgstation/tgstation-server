using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// A <see cref="IDmbProvider"/> that uses symlinks.
	/// </summary>
	[SupportedOSPlatform("windows")]
	sealed class SymlinkDmbProvider : SwappableDmbProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SymlinkDmbProvider"/> class.
		/// </summary>
		/// <param name="baseProvider">The <see cref="IDmbProvider"/> for the <see cref="SwappableDmbProvider"/>.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="SwappableDmbProvider"/>.</param>
		/// <param name="linkFactory">The <see cref="IFilesystemLinkFactory"/> for the <see cref="SwappableDmbProvider"/>.</param>
		public SymlinkDmbProvider(
			IDmbProvider baseProvider,
			IIOManager ioManager,
			IFilesystemLinkFactory linkFactory)
			: base(baseProvider, ioManager, linkFactory)
		{
		}

		/// <inheritdoc />
		public override Task FinishActivationPreparation(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		protected override async Task DoSwap(CancellationToken cancellationToken)
		{
			if (LinkFactory.SymlinkedDirectoriesAreDeletedAsFiles)
				await IOManager.DeleteFile(LiveGameDirectory, cancellationToken);
			else
				await IOManager.DeleteDirectory(LiveGameDirectory, cancellationToken);

			await LinkFactory.CreateSymbolicLink(
				IOManager.ResolvePath(BaseProvider.Directory),
				IOManager.ResolvePath(LiveGameDirectory),
				cancellationToken);
		}
	}
}
