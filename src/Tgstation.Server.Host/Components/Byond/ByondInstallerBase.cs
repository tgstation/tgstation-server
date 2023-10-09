using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <inheritdoc />
	abstract class ByondInstallerBase : IByondInstaller
	{
		/// <summary>
		/// The name of BYOND's cache directory.
		/// </summary>
		const string CacheDirectoryName = "cache";

		/// <summary>
		/// The first <see cref="Version"/> of BYOND that supports the '-map-threads' parameter on DreamDaemon.
		/// </summary>
		public static Version MapThreadsVersion => new (515, 1609);

		/// <inheritdoc />
		public abstract string CompilerName { get; }

		/// <inheritdoc />
		public abstract string PathToUserFolder { get; }

		/// <summary>
		/// Gets the URL formatter string for downloading a byond version of {0:Major} {1:Minor}.
		/// </summary>
		protected abstract string ByondRevisionsUrlTemplate { get; }

		/// <summary>
		/// Gets the <see cref="IIOManager"/> for the <see cref="ByondInstallerBase"/>.
		/// </summary>
		protected IIOManager IOManager { get; }

		/// <summary>
		/// Gets the <see cref="ILogger"/> for the <see cref="ByondInstallerBase"/>.
		/// </summary>
		protected ILogger<ByondInstallerBase> Logger { get; }

		/// <summary>
		/// The <see cref="IFileDownloader"/> for the <see cref="ByondInstallerBase"/>.
		/// </summary>
		readonly IFileDownloader fileDownloader;

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondInstallerBase"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="IOManager"/>.</param>
		/// <param name="fileDownloader">The value of <see cref="fileDownloader"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		protected ByondInstallerBase(IIOManager ioManager, IFileDownloader fileDownloader, ILogger<ByondInstallerBase> logger)
		{
			IOManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public abstract string GetDreamDaemonName(ByondVersion version, out bool supportsCli, out bool supportsMapThreads);

		/// <inheritdoc />
		public async Task CleanCache(CancellationToken cancellationToken)
		{
			try
			{
				Logger.LogDebug("Cleaning BYOND cache...");
				await IOManager.DeleteDirectory(
					IOManager.ConcatPath(
						PathToUserFolder,
						CacheDirectoryName),
					cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Logger.LogWarning(ex, "Error deleting BYOND cache!");
			}
		}

		/// <inheritdoc />
		public abstract ValueTask InstallByond(ByondVersion version, string path, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract ValueTask UpgradeInstallation(ByondVersion version, string path, CancellationToken cancellationToken);

		/// <inheritdoc />
		public async ValueTask<MemoryStream> DownloadVersion(ByondVersion version, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(version);

			Logger.LogTrace("Downloading BYOND version {major}.{minor}...", version.Version.Major, version.Version.Minor);
			var url = String.Format(CultureInfo.InvariantCulture, ByondRevisionsUrlTemplate, version.Version.Major, version.Version.Minor);

			await using var download = fileDownloader.DownloadFile(new Uri(url), null);
			await using var buffer = new BufferedFileStreamProvider(
				await download.GetResult(cancellationToken));
			return await buffer.GetOwnedResult(cancellationToken);
		}
	}
}
