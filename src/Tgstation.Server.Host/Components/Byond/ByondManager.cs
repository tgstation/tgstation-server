using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <inheritdoc />
	sealed class ByondManager : IByondManager
	{
		/// <summary>
		/// The path to the BYOND bin folder.
		/// </summary>
		public const string BinPath = "byond/bin";

		/// <summary>
		/// The path to the cfg directory.
		/// </summary>
		const string CfgDirectoryName = "cfg";

		/// <summary>
		/// The name of the list of trusted .dmb files in the user's BYOND cfg directory.
		/// </summary>
		const string TrustedDmbFileName = "trusted.txt";

		/// <summary>
		/// The file in which we store the <see cref="VersionKey(Version, bool)"/> for installations.
		/// </summary>
		const string VersionFileName = "Version.txt";

		/// <summary>
		/// The file in which we store the <see cref="VersionKey(Version, bool)"/> for the active installation.
		/// </summary>
		const string ActiveVersionFileName = "ActiveVersion.txt";

		/// <inheritdoc />
		public Version ActiveVersion { get; private set; }

		/// <inheritdoc />
		public IReadOnlyList<Version> InstalledVersions
		{
			get
			{
				lock (installedVersions)
					return installedVersions.Select(x => Version.Parse(x.Key).Semver()).ToList();
			}
		}

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ByondManager"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IByondInstaller"/> for the <see cref="ByondManager"/>.
		/// </summary>
		readonly IByondInstaller byondInstaller;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="ByondManager"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ByondManager"/>.
		/// </summary>
		readonly ILogger<ByondManager> logger;

		/// <summary>
		/// Map of byond <see cref="Version"/>s to <see cref="Task"/>s that complete when they are installed.
		/// </summary>
		readonly Dictionary<string, Task> installedVersions;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="ByondManager"/>.
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// Converts a BYOND <paramref name="version"/> to a <see cref="string"/>.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to convert.</param>
		/// <param name="allowPatch">If the <see cref="Version.Build"/> property of <paramref name="version"/> should be kept.</param>
		/// <returns>The <see cref="string"/> representation of <paramref name="version"/>.</returns>
		static string VersionKey(Version version, bool allowPatch) => (allowPatch && version.Build > 0
			? new Version(version.Major, version.Minor, version.Build)
			: new Version(version.Major, version.Minor)).ToString();

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondManager"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="byondInstaller">The value of <see cref="byondInstaller"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public ByondManager(IIOManager ioManager, IByondInstaller byondInstaller, IEventConsumer eventConsumer, ILogger<ByondManager> logger)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.byondInstaller = byondInstaller ?? throw new ArgumentNullException(nameof(byondInstaller));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			installedVersions = new Dictionary<string, Task>();
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
		public async Task ChangeVersion(Version version, Stream customVersionStream, CancellationToken cancellationToken)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			var versionKey = await InstallVersion(version, customVersionStream, cancellationToken);
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken))
			{
				await ioManager.WriteAllBytes(ActiveVersionFileName, Encoding.UTF8.GetBytes(versionKey), cancellationToken);
				await eventConsumer.HandleEvent(
					EventType.ByondActiveVersionChange,
					new List<string>
					{
						ActiveVersion != null
							? VersionKey(ActiveVersion, true)
							: null,
						versionKey,
					},
					cancellationToken)
					;

				// We reparse the version key because it could be changed after a custom install.
				ActiveVersion = Version.Parse(versionKey);
			}
		}

		/// <inheritdoc />
		public async Task<IByondExecutableLock> UseExecutables(Version requiredVersion, CancellationToken cancellationToken)
		{
			var versionToUse = requiredVersion ?? ActiveVersion ?? throw new JobException(ErrorCode.ByondNoVersionsInstalled);
			await InstallVersion(versionToUse, null, cancellationToken);

			var versionKey = VersionKey(versionToUse, true);
			var binPathForVersion = ioManager.ConcatPath(versionKey, BinPath);

			logger.LogTrace("Creating ByondExecutableLock lock for version {versionToUse}", versionToUse);
			return new ByondExecutableLock(
				ioManager,
				semaphore,
				versionToUse,
				ioManager.ResolvePath(
					ioManager.ConcatPath(
						binPathForVersion,
						byondInstaller.GetDreamDaemonName(versionToUse, out var supportsCli))),
				ioManager.ResolvePath(
					ioManager.ConcatPath(
						binPathForVersion,
						byondInstaller.DreamMakerName)),
				ioManager.ResolvePath(
					ioManager.ConcatPath(
						byondInstaller.PathToUserByondFolder,
						CfgDirectoryName,
						TrustedDmbFileName)),
				supportsCli);
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			async Task<byte[]> GetActiveVersion()
			{
				var activeVersionFileExists = await ioManager.FileExists(ActiveVersionFileName, cancellationToken);
				return !activeVersionFileExists ? null : await ioManager.ReadAllBytes(ActiveVersionFileName, cancellationToken);
			}

			var activeVersionBytesTask = GetActiveVersion();

			// Create local cfg directory in case it doesn't exist
			var localCfgDirectory = ioManager.ConcatPath(
					byondInstaller.PathToUserByondFolder,
					CfgDirectoryName);
			await ioManager.CreateDirectory(
				localCfgDirectory,
				cancellationToken);

			// Delete trusted.txt so it doesn't grow too large
			var trustedFilePath =
				ioManager.ConcatPath(
					localCfgDirectory,
					TrustedDmbFileName);
			logger.LogTrace("Deleting trusted .dmbs file {trustedFilePath}", trustedFilePath);
			await ioManager.DeleteFile(
				trustedFilePath,
				cancellationToken);

			var byondDirectory = ioManager.ResolvePath();
			await ioManager.CreateDirectory(byondDirectory, cancellationToken);
			var directories = await ioManager.GetDirectories(byondDirectory, cancellationToken);

			var installedVersionPaths = new Dictionary<string, Version>();

			async Task ReadVersion(string path)
			{
				var versionFile = ioManager.ConcatPath(path, VersionFileName);
				if (!await ioManager.FileExists(versionFile, cancellationToken))
				{
					logger.LogInformation("Cleaning unparsable version path: {versionPath}", ioManager.ResolvePath(path));
					await ioManager.DeleteDirectory(path, cancellationToken); // cleanup
					return;
				}

				var bytes = await ioManager.ReadAllBytes(versionFile, cancellationToken);
				var text = Encoding.UTF8.GetString(bytes);
				if (Version.TryParse(text, out var version))
				{
					var key = VersionKey(version, true);
					lock (installedVersions)
						if (!installedVersions.ContainsKey(key))
						{
							logger.LogDebug("Adding detected BYOND version {versionKey}...", key);
							installedVersions.Add(key, Task.CompletedTask);
							installedVersionPaths.Add(ioManager.ResolvePath(key), version);
							return;
						}
				}

				await ioManager.DeleteDirectory(path, cancellationToken);
			}

			await Task.WhenAll(directories.Select(ReadVersion));

			logger.LogTrace("Upgrading BYOND installations...");
			await Task.WhenAll(installedVersionPaths.Select(kvp => byondInstaller.UpgradeInstallation(kvp.Value, kvp.Key, cancellationToken)));

			var activeVersionBytes = await activeVersionBytesTask;
			if (activeVersionBytes != null)
			{
				var activeVersionString = Encoding.UTF8.GetString(activeVersionBytes);
				bool hasRequestedActiveVersion;
				lock (installedVersions)
					hasRequestedActiveVersion = installedVersions.ContainsKey(activeVersionString);
				if (hasRequestedActiveVersion && Version.TryParse(activeVersionString, out var activeVersion))
					ActiveVersion = activeVersion;
				else
				{
					logger.LogWarning("Failed to load saved active version {activeVersion}!", activeVersionString);
					await ioManager.DeleteFile(ActiveVersionFileName, cancellationToken);
				}
			}
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <summary>
		/// Installs a BYOND <paramref name="version"/> if it isn't already.
		/// </summary>
		/// <param name="version">The BYOND <see cref="Version"/> to install.</param>
		/// <param name="customVersionStream">Custom zip file <see cref="Stream"/> to use. Will cause a <see cref="Version.Build"/> number to be added.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task<string> InstallVersion(Version version, Stream customVersionStream, CancellationToken cancellationToken)
		{
			var ourTcs = new TaskCompletionSource();
			Task inProgressTask;
			string versionKey;
			bool installed;
			lock (installedVersions)
			{
				if (customVersionStream != null)
				{
					int customInstallationNumber = 1;
					do
					{
						versionKey = $"{VersionKey(version, false)}.{customInstallationNumber++}";
					}
					while (installedVersions.ContainsKey(versionKey));
				}
				else
					versionKey = VersionKey(version, true);

				installed = installedVersions.TryGetValue(versionKey, out inProgressTask);
				if (!installed)
					installedVersions.Add(versionKey, ourTcs.Task);
			}

			if (installed)
				using (cancellationToken.Register(() => ourTcs.SetCanceled()))
				{
					await Task.WhenAny(ourTcs.Task, inProgressTask);
					cancellationToken.ThrowIfCancellationRequested();
					return versionKey;
				}

			if (customVersionStream != null)
				logger.LogInformation("Installing custom BYOND version as {versionKey}...", versionKey);
			else if (version.Build > 0)
				throw new JobException(ErrorCode.ByondNonExistentCustomVersion);
			else
				logger.LogDebug("Requested BYOND version {versionKey} not currently installed. Doing so now...", versionKey);

			// okay up to us to install it then
			try
			{
				await eventConsumer.HandleEvent(EventType.ByondInstallStart, new List<string> { versionKey }, cancellationToken);

				var extractPath = ioManager.ResolvePath(versionKey);
				async Task DirectoryCleanup()
				{
					await ioManager.DeleteDirectory(extractPath, cancellationToken);
					await ioManager.CreateDirectory(extractPath, cancellationToken);
				}

				var directoryCleanupTask = DirectoryCleanup();
				try
				{
					Stream versionZipStream;
					Stream downloadedStream = null;
					if (customVersionStream == null)
					{
						downloadedStream = await byondInstaller.DownloadVersion(version, cancellationToken);
						versionZipStream = downloadedStream;
					}
					else
						versionZipStream = customVersionStream;

					using (downloadedStream)
					{
						await directoryCleanupTask;
						logger.LogTrace("Extracting downloaded BYOND zip to {extractPath}...", extractPath);
						await ioManager.ZipToDirectory(extractPath, versionZipStream, cancellationToken);
					}

					await byondInstaller.InstallByond(version, extractPath, cancellationToken);

					// make sure to do this last because this is what tells us we have a valid version in the future
					await ioManager.WriteAllBytes(
						ioManager.ConcatPath(versionKey, VersionFileName),
						Encoding.UTF8.GetBytes(versionKey),
						cancellationToken)
						;
				}
				catch (HttpRequestException e)
				{
					// since the user can easily provide non-exitent version numbers, we'll turn this into a JobException
					throw new JobException(ErrorCode.ByondDownloadFail, e);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch
				{
					await ioManager.DeleteDirectory(versionKey, cancellationToken);
					throw;
				}

				ourTcs.SetResult();
			}
			catch (Exception e)
			{
				if (e is not OperationCanceledException)
					await eventConsumer.HandleEvent(EventType.ByondInstallFail, new List<string> { e.Message }, cancellationToken);
				lock (installedVersions)
					installedVersions.Remove(versionKey);
				ourTcs.SetException(e);
				throw;
			}

			return versionKey;
		}
	}
}
