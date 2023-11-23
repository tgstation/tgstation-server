using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.IO;

#nullable disable

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// A <see cref="IDmbProvider"/> that uses filesystem links to change directory structure underneath the server process.
	/// </summary>
	abstract class SwappableDmbProvider : IDmbProvider
	{
		/// <summary>
		/// The directory where the <see cref="BaseProvider"/> is symlinked to.
		/// </summary>
		public const string LiveGameDirectory = "Live";

		/// <inheritdoc />
		public string DmbName => BaseProvider.DmbName;

		/// <inheritdoc />
		public string Directory => IOManager.ResolvePath(LiveGameDirectory);

		/// <inheritdoc />
		public Models.CompileJob CompileJob => BaseProvider.CompileJob;

		/// <inheritdoc />
		public EngineVersion EngineVersion => BaseProvider.EngineVersion;

		/// <summary>
		/// If <see cref="MakeActive(CancellationToken)"/> has been run.
		/// </summary>
		public bool Swapped => swapped != 0;

		/// <summary>
		/// The <see cref="IDmbProvider"/> we are swapping for.
		/// </summary>
		protected IDmbProvider BaseProvider { get; }

		/// <summary>
		/// The <see cref="IIOManager"/> to use.
		/// </summary>
		protected IIOManager IOManager { get; }

		/// <summary>
		/// The <see cref="IFilesystemLinkFactory"/> to use.
		/// </summary>
		protected IFilesystemLinkFactory LinkFactory { get; }

		/// <summary>
		/// Backing field for <see cref="Swapped"/>.
		/// </summary>
		volatile int swapped;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwappableDmbProvider"/> class.
		/// </summary>
		/// <param name="baseProvider">The value of <see cref="BaseProvider"/>.</param>
		/// <param name="ioManager">The value of <see cref="IOManager"/>.</param>
		/// <param name="symlinkFactory">The value of <see cref="LinkFactory"/>.</param>
		public SwappableDmbProvider(IDmbProvider baseProvider, IIOManager ioManager, IFilesystemLinkFactory symlinkFactory)
		{
			BaseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
			IOManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			LinkFactory = symlinkFactory ?? throw new ArgumentNullException(nameof(symlinkFactory));
		}

		/// <inheritdoc />
		public virtual ValueTask DisposeAsync() => BaseProvider.DisposeAsync();

		/// <inheritdoc />
		public void KeepAlive() => BaseProvider.KeepAlive();

		/// <summary>
		/// Make the <see cref="SwappableDmbProvider"/> active by replacing the live link with our <see cref="CompileJob"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		public ValueTask MakeActive(CancellationToken cancellationToken)
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
		public abstract Task FinishActivationPreparation(CancellationToken cancellationToken);

		/// <summary>
		/// Perform the swapping action.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected abstract ValueTask DoSwap(CancellationToken cancellationToken);
	}
}
