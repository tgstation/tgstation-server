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
	sealed class SwappableDmbProvider : IDmbProvider
	{
		/// <summary>
		/// The directory where the <see cref="baseProvider"/> is symlinked to.
		/// </summary>
		public const string LiveGameDirectory = "Live";

		/// <inheritdoc />
		public string DmbName => baseProvider.DmbName;

		/// <inheritdoc />
		public string Directory => ioManager.ResolvePath(LiveGameDirectory);

		/// <inheritdoc />
		public CompileJob CompileJob => baseProvider.CompileJob;

		/// <summary>
		/// The <see cref="IDmbProvider"/> we are swapping for.
		/// </summary>
		readonly IDmbProvider baseProvider;

		/// <summary>
		/// The <see cref="IIOManager"/> to use.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ISymlinkFactory"/> to use.
		/// </summary>
		readonly ISymlinkFactory symlinkFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwappableDmbProvider"/> class.
		/// </summary>
		/// <param name="baseProvider">The value of <see cref="baseProvider"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="symlinkFactory">The value of <see cref="symlinkFactory"/>.</param>
		public SwappableDmbProvider(IDmbProvider baseProvider, IIOManager ioManager, ISymlinkFactory symlinkFactory)
		{
			this.baseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.symlinkFactory = symlinkFactory ?? throw new ArgumentNullException(nameof(symlinkFactory));
		}

		/// <inheritdoc />
		public void Dispose() => baseProvider.Dispose();

		/// <inheritdoc />
		public void KeepAlive() => baseProvider.KeepAlive();

		/// <summary>
		/// Make the <see cref="SwappableDmbProvider"/> active by replacing the live link with our <see cref="CompileJob"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		public async Task MakeActive(CancellationToken cancellationToken)
		{
			// Note this comment from TGS3:
			// These next two lines should be atomic but this is the best we can do
			await ioManager.DeleteDirectory(LiveGameDirectory, cancellationToken).ConfigureAwait(false);
			await symlinkFactory.CreateSymbolicLink(
				ioManager.ResolvePath(baseProvider.Directory),
				ioManager.ResolvePath(LiveGameDirectory),
				cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
