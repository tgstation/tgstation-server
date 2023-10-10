using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <inheritdoc />
	abstract class EngineInstallerBase : IEngineInstaller
	{
		/// <summary>
		/// The <see cref="EngineType"/> the installer supports.
		/// </summary>
		protected abstract EngineType TargetEngineType { get; }

		/// <summary>
		/// Gets the <see cref="IIOManager"/> for the <see cref="EngineInstallerBase"/>.
		/// </summary>
		protected IIOManager IOManager { get; }

		/// <summary>
		/// Gets the <see cref="ILogger"/> for the <see cref="EngineInstallerBase"/>.
		/// </summary>
		protected ILogger<EngineInstallerBase> Logger { get; }

		/// <summary>
		/// The <see cref="IFileDownloader"/> for the <see cref="EngineInstallerBase"/>.
		/// </summary>
		readonly IFileDownloader fileDownloader;

		/// <summary>
		/// Initializes a new instance of the <see cref="EngineInstallerBase"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="IOManager"/>.</param>
		/// <param name="fileDownloader">The value of <see cref="fileDownloader"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		protected EngineInstallerBase(IIOManager ioManager, IFileDownloader fileDownloader, ILogger<EngineInstallerBase> logger)
		{
			IOManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public abstract IEngineInstallation CreateInstallation(ByondVersion version, Task installationTask);

		/// <inheritdoc />
		public abstract Task CleanCache(CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract ValueTask Install(ByondVersion version, string path, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract ValueTask UpgradeInstallation(ByondVersion version, string path, CancellationToken cancellationToken);

		/// <inheritdoc />
		public async ValueTask<MemoryStream> DownloadVersion(ByondVersion version, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);

			var url = await GetDownloadZipUrl(version, cancellationToken);
			Logger.LogTrace("Downloading {engineType} version {version} from {url}...", TargetEngineType, version, url);

			await using var download = fileDownloader.DownloadFile(url, null);
			await using var buffer = new BufferedFileStreamProvider(
				await download.GetResult(cancellationToken));
			return await buffer.GetOwnedResult(cancellationToken);
		}

		/// <inheritdoc />
		public abstract ValueTask TrustDmbPath(string fullDmbPath, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <see cref="Uri"/> pointing to the location of the download for a given <paramref name="version"/>.
		/// </summary>
		/// <param name="version">The <see cref="ByondVersion"/> to create a <see cref="Uri"/> for.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="Uri"/> pointing to the version download location.</returns>
		protected abstract ValueTask<Uri> GetDownloadZipUrl(ByondVersion version, CancellationToken cancellationToken);

		/// <summary>
		/// Check that a given <paramref name="version"/> is of type <see cref="EngineType.Byond"/>.
		/// </summary>
		/// <param name="version">The <see cref="ByondVersion"/> to check.</param>
		protected void CheckVersionValidity(ByondVersion version)
		{
			ArgumentNullException.ThrowIfNull(version);
			if (version.Engine.Value != TargetEngineType)
				throw new InvalidOperationException($"Non-{TargetEngineType} engine specified: {version.Engine.Value}");
		}
	}
}
