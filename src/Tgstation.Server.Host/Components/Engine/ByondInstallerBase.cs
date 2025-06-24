using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Base implementation of <see cref="IEngineInstaller"/> for <see cref="EngineType.Byond"/>.
	/// </summary>
	abstract class ByondInstallerBase : EngineInstallerBase
	{
		/// <summary>
		/// The path to the BYOND bin folder.
		/// </summary>
		protected const string ByondBinPath = "byond/bin";

		/// <summary>
		/// The path to the cfg directory.
		/// </summary>
		protected const string CfgDirectoryName = "cfg";

		/// <summary>
		/// The name of BYOND's cache directory.
		/// </summary>
		const string CacheDirectoryName = "cache";

		/// <summary>
		/// The first <see cref="Version"/> of BYOND that supports the '-map-threads' parameter on DreamDaemon.
		/// </summary>
		static readonly Version MapThreadsVersion = new(515, 1609);

		/// <inheritdoc />
		protected override EngineType TargetEngineType => EngineType.Byond;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> <see cref="IOptionsMonitor{TOptions}"/> for the <see cref="ByondInstallerBase"/>.
		/// </summary>
		protected IOptionsMonitor<GeneralConfiguration> GeneralConfigurationOptions { get; }

		/// <summary>
		/// Path to the system user's local BYOND folder.
		/// </summary>
		protected abstract string PathToUserFolder { get; }

		/// <summary>
		/// Path to the DreamMaker executable.
		/// </summary>
		protected abstract string DreamMakerName { get; }

		/// <summary>
		/// Template to do ${Marker:xxx} replacements in <see cref="GeneralConfiguration.ByondZipDownloadTemplate"/>.
		/// </summary>
		protected abstract string OSMarkerTemplate { get; }

		/// <summary>
		/// The <see cref="IFileDownloader"/> for the <see cref="ByondInstallerBase"/>.
		/// </summary>
		readonly IFileDownloader fileDownloader;

		/// <summary>
		/// Format a given <paramref name="byondZipDownloadTemplate"/>.
		/// </summary>
		/// <param name="semver">The BYOND version to download.</param>
		/// <param name="byondZipDownloadTemplate">The template.</param>
		/// <param name="osMarkerTemplate">The <see cref="OSMarkerTemplate"/>.</param>
		/// <returns>The formatted byond download <see cref="Uri"/>.</returns>
		/// <remarks>Exposed only for testability.</remarks>
		internal static Uri GetDownloadZipUrl(Version semver, string byondZipDownloadTemplate, string osMarkerTemplate)
		{
			// god forbid
			var guardGuid = Guid.NewGuid();

			var url = byondZipDownloadTemplate
				.Replace("$$", guardGuid.ToString(), StringComparison.Ordinal)
				.Replace("${Major}", semver.Major.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
				.Replace("${Minor}", semver.Minor.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

			var osMarkerPrefix = $"${{{osMarkerTemplate}:";
			var osMarkerIndex = url.IndexOf(osMarkerPrefix);
			while (osMarkerIndex != -1)
			{
				var start = osMarkerIndex + osMarkerPrefix.Length;
				var end = url.IndexOf('}', start);
				if (end == -1)
					break;

				var substitution = url.Substring(start, end - start);
				url = url.Replace($"{osMarkerPrefix}{substitution}}}", substitution, StringComparison.Ordinal);

				osMarkerIndex = url.IndexOf(osMarkerPrefix);
			}

			// at this point, any other substitution attempts should be removed
			var otherMarkerPrefix = "${";
			var otherMarkerIndex = url.IndexOf(otherMarkerPrefix);
			while (otherMarkerIndex != -1)
			{
				var start = otherMarkerIndex + otherMarkerPrefix.Length;
				var end = url.IndexOf('}', start);
				if (end == -1)
					break;

				var substitution = url.Substring(start, end - start);
				url = url.Replace($"{otherMarkerPrefix}{substitution}}}", String.Empty, StringComparison.Ordinal);

				otherMarkerIndex = url.IndexOf(otherMarkerPrefix);
			}

			url = url.Replace(guardGuid.ToString(), "$", StringComparison.Ordinal);

			return new Uri(url);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondInstallerBase"/> class.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="EngineInstallerBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="EngineInstallerBase"/>.</param>
		/// <param name="fileDownloader">The value of <see cref="fileDownloader"/>.</param>
		/// <param name="generalConfigurationOptions">The value of <see cref="GeneralConfigurationOptions"/>.</param>
		protected ByondInstallerBase(
			IIOManager ioManager,
			ILogger<ByondInstallerBase> logger,
			IFileDownloader fileDownloader,
			IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions)
			: base(ioManager, logger)
		{
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			GeneralConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <inheritdoc />
		public override ValueTask<IEngineInstallation> CreateInstallation(EngineVersion version, string path, Task installationTask, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);

			var installationIOManager = new ResolvingIOManager(IOManager, path);
			var supportsMapThreads = version.Version >= MapThreadsVersion;

			return ValueTask.FromResult<IEngineInstallation>(
				new ByondInstallation(
					installationIOManager,
					installationTask,
					version,
					installationIOManager.ResolvePath(
						installationIOManager.ConcatPath(
							ByondBinPath,
							GetDreamDaemonName(
								version.Version!,
								out var supportsCli))),
					installationIOManager.ResolvePath(
						installationIOManager.ConcatPath(
							ByondBinPath,
							DreamMakerName)),
					supportsCli,
					supportsMapThreads));
		}

		/// <inheritdoc />
		public override async Task CleanCache(CancellationToken cancellationToken)
		{
			try
			{
				var byondDir = PathToUserFolder;

				Logger.LogDebug("Cleaning BYOND cache...");
				async Task CleanDirectorySafe()
				{
					try
					{
						await IOManager.DeleteDirectory(
							IOManager.ConcatPath(
								byondDir,
								CacheDirectoryName),
							cancellationToken);
					}
					catch (Exception ex)
					{
						Logger.LogWarning(ex, "Failed to clean BYOND cache!");
					}
				}

				var cacheCleanTask = CleanDirectorySafe();

				// Create local cfg directory in case it doesn't exist
				var localCfgDirectory = IOManager.ConcatPath(
					byondDir,
					CfgDirectoryName);

				var cfgCreateTask = IOManager.CreateDirectory(
					localCfgDirectory,
					cancellationToken);

				var additionalCleanTasks = AdditionalCacheCleanFilePaths(localCfgDirectory)
					.Select(path => IOManager.DeleteFile(path, cancellationToken));

				await Task.WhenAll(cacheCleanTask, cfgCreateTask, Task.WhenAll(additionalCleanTasks));
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Logger.LogWarning(ex, "Error cleaning BYOND cache!");
			}
		}

		/// <inheritdoc />
		public override async ValueTask<IEngineInstallationData> DownloadVersion(EngineVersion version, JobProgressReporter progressReporter, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);

			var url = GetDownloadZipUrl(version);
			Logger.LogTrace("Downloading {engineType} version {version} from {url}...", TargetEngineType, version, url);

			await using var download = fileDownloader.DownloadFile(url, null);
			await using var buffer = new BufferedFileStreamProvider(
				await download.GetResult(cancellationToken));

			var stream = await buffer.GetOwnedResult(cancellationToken);
			try
			{
				return new ZipStreamEngineInstallationData(
					IOManager,
					stream);
			}
			catch
			{
				await stream.DisposeAsync();
				throw;
			}
		}

		/// <summary>
		/// Get the file name of the DreamDaemon executable.
		/// </summary>
		/// <param name="byondVersion">The <see cref="Version"/> of BYOND to select the executable name for.</param>
		/// <param name="supportsCli">Whether or not the returned path supports being run as a command-line application.</param>
		/// <returns>The file name of the DreamDaemon executable.</returns>
		protected abstract string GetDreamDaemonName(Version byondVersion, out bool supportsCli);

		/// <summary>
		/// List off additional file paths in the <paramref name="configDirectory"/> to delete.
		/// </summary>
		/// <param name="configDirectory">The full path to the relevant <see cref="CfgDirectoryName"/>.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of paths in <paramref name="configDirectory"/> to clean.</returns>
		protected virtual IEnumerable<string> AdditionalCacheCleanFilePaths(string configDirectory) => Enumerable.Empty<string>();

		/// <summary>
		/// Create a <see cref="Uri"/> pointing to the location of the download for a given <paramref name="version"/>.
		/// </summary>
		/// <param name="version">The <see cref="EngineVersion"/> to create a <see cref="Uri"/> for.</param>
		/// <returns>A <see cref="Uri"/> pointing to the version download location.</returns>
		Uri GetDownloadZipUrl(EngineVersion version)
		{
			CheckVersionValidity(version);

			var guardGuid = Guid.NewGuid();

			var semver = version.Version!;
			var template = GeneralConfigurationOptions.CurrentValue.ByondZipDownloadTemplate;
			return GetDownloadZipUrl(semver, template, OSMarkerTemplate);
		}
	}
}
