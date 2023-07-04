using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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
		/// The file in which we store the <see cref="Version"/> for installations.
		/// </summary>
		const string VersionFileName = "Version.txt";

		/// <summary>
		/// The file in which we store the <see cref="ActiveVersion"/>.
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
					return installedVersions.Keys.ToList();
			}
		}

		/// <summary>
		/// <see cref="SemaphoreSlim"/> for writing to files in the user's BYOND directory.
		/// </summary>
		static readonly SemaphoreSlim UserFilesSemaphore = new (1);

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
		readonly Dictionary<Version, ReferenceCountingContainer<ByondInstallation, ByondExecutableLock>> installedVersions;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for changing or deleting the active BYOND version.
		/// </summary>
		readonly SemaphoreSlim changeDeleteSemaphore;

		/// <summary>
		/// <see cref="TaskCompletionSource"/> that notifes when the <see cref="ActiveVersion"/> changes.
		/// </summary>
		TaskCompletionSource activeVersionChanged;

		/// <summary>
		/// Validates a given <paramref name="version"/> parameter.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to validate.</param>
		static void CheckVersionParameter(Version version)
		{
			ArgumentNullException.ThrowIfNull(version);

			if (version.Build == 0)
				throw new ArgumentException("version.Build cannot be 0!", nameof(version));
		}

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

			installedVersions = new Dictionary<Version, ReferenceCountingContainer<ByondInstallation, ByondExecutableLock>>();
			changeDeleteSemaphore = new SemaphoreSlim(1);
			activeVersionChanged = new TaskCompletionSource();
		}

		/// <inheritdoc />
		public void Dispose() => changeDeleteSemaphore.Dispose();

		/// <inheritdoc />
		public async Task ChangeVersion(
			JobProgressReporter progressReporter,
			Version version,
			Stream customVersionStream,
			bool allowInstallation,
			CancellationToken cancellationToken)
		{
			CheckVersionParameter(version);

			using (await SemaphoreSlimContext.Lock(changeDeleteSemaphore, cancellationToken))
			{
				using var installLock = await AssertAndLockVersion(
				progressReporter,
				version,
				customVersionStream,
				false,
				allowInstallation,
				cancellationToken);

				// We reparse the version because it could be changed after a custom install.
				version = installLock.Version;

				var stringVersion = version.ToString();
				await ioManager.WriteAllBytes(ActiveVersionFileName, Encoding.UTF8.GetBytes(stringVersion), cancellationToken);
				await eventConsumer.HandleEvent(
					EventType.ByondActiveVersionChange,
					new List<string>
					{
						ActiveVersion?.ToString(),
						stringVersion,
					},
					false,
					cancellationToken);

				ActiveVersion = version;
				activeVersionChanged.SetResult();
				activeVersionChanged = new TaskCompletionSource();
			}

			logger.LogInformation("Active version changed to {version}", version);
		}

		/// <inheritdoc />
		public async Task<IByondExecutableLock> UseExecutables(Version requiredVersion, string trustDmbFullPath, CancellationToken cancellationToken)
		{
			logger.LogTrace(
				"Acquiring lock on BYOND version {version}...",
				requiredVersion?.ToString() ?? $"{ActiveVersion} (active)");
			var versionToUse = requiredVersion ?? ActiveVersion ?? throw new JobException(ErrorCode.ByondNoVersionsInstalled);
			var installLock = await AssertAndLockVersion(
				null,
				versionToUse,
				null,
				requiredVersion != null,
				true,
				cancellationToken);
			try
			{
				if (trustDmbFullPath != null)
					await TrustDmbPath(trustDmbFullPath, cancellationToken);

				return installLock;
			}
			catch
			{
				installLock.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public async Task DeleteVersion(JobProgressReporter progressReporter, Version version, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(progressReporter);

			CheckVersionParameter(version);

			logger.LogTrace("DeleteVersion {version}", version);

			if (version == ActiveVersion)
				throw new JobException(ErrorCode.ByondCannotDeleteActiveVersion);

			ReferenceCountingContainer<ByondInstallation, ByondExecutableLock> container;
			lock (installedVersions)
				if (!installedVersions.TryGetValue(version, out container))
					return; // already "deleted"

			logger.LogInformation("Deleting BYOND version {version}...", version);
			progressReporter.StageName = "Waiting for version to not be in use...";
			while (true)
			{
				var containerTask = container.OnZeroReferences;

				// We also want to check when the active version changes in case we need to fail the job because of that.
				Task activeVersionUpdate;
				using (await SemaphoreSlimContext.Lock(changeDeleteSemaphore, cancellationToken))
					activeVersionUpdate = activeVersionChanged.Task;

				await Task.WhenAny(
					containerTask,
					activeVersionUpdate)
					.WaitAsync(cancellationToken);

				if (containerTask.IsCompleted)
					logger.LogTrace("All BYOND locks for {version} are gone", version);

				using (await SemaphoreSlimContext.Lock(changeDeleteSemaphore, cancellationToken))
				{
					// check again because it could have become the active version.
					if (version == ActiveVersion)
						throw new JobException(ErrorCode.ByondCannotDeleteActiveVersion);

					bool proceed;
					lock (installedVersions)
					{
						proceed = container.OnZeroReferences.IsCompleted;
						if (proceed)
							if (!installedVersions.TryGetValue(version, out var newerContainer))
								logger.LogWarning("Unable to remove BYOND installation {version} from list! Is there a duplicate job running?", version);
							else
							{
								if (container != newerContainer)
								{
									// Okay let me get this straight, there was a duplicate delete job, it ran before us after we grabbed the container, AND another installation of the same version completed?
									// I know realistically this is practically impossible, but god damn that small possiblility
									// best thing to do is check we exclusively own the newer container
									logger.LogDebug("Extreme race condition encountered, applying concentrated copium...");
									container = newerContainer;
									proceed = container.OnZeroReferences.IsCompleted;
								}

								if (proceed)
									installedVersions.Remove(version);
							}
					}

					if (proceed)
					{
						progressReporter.StageName = "Deleting installation...";

						// delete the version file first, because we will know not to re-discover the installation if it's not present and it will get cleaned on reboot
						var installPath = version.ToString();
						await ioManager.DeleteFile(
							ioManager.ConcatPath(installPath, VersionFileName),
							cancellationToken);
						await ioManager.DeleteDirectory(installPath, cancellationToken);
						return;
					}

					if (containerTask.IsCompleted)
						logger.LogDebug(
							"Another lock was acquired before we could remove version {version} from the list. We will have to wait again.",
							version);
				}
			}
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

			var byondDir = byondInstaller.PathToUserByondFolder;
			if (byondDir != null)
				using (await SemaphoreSlimContext.Lock(UserFilesSemaphore, cancellationToken))
				{
					// Create local cfg directory in case it doesn't exist
					var localCfgDirectory = ioManager.ConcatPath(
							byondDir,
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
				}

			await ioManager.CreateDirectory(DefaultIOManager.CurrentDirectory, cancellationToken);
			var directories = await ioManager.GetDirectories(DefaultIOManager.CurrentDirectory, cancellationToken);

			var installedVersionPaths = new Dictionary<string, Version>();

			async Task ReadVersion(string path)
			{
				var versionFile = ioManager.ConcatPath(path, VersionFileName);
				if (!await ioManager.FileExists(versionFile, cancellationToken))
				{
					logger.LogWarning("Cleaning path with no version file: {versionPath}", ioManager.ResolvePath(path));
					await ioManager.DeleteDirectory(path, cancellationToken); // cleanup
					return;
				}

				var bytes = await ioManager.ReadAllBytes(versionFile, cancellationToken);
				var text = Encoding.UTF8.GetString(bytes);
				if (!Version.TryParse(text, out var version))
				{
					logger.LogWarning("Cleaning path with unparsable version file: {versionPath}", ioManager.ResolvePath(path));
					await ioManager.DeleteDirectory(path, cancellationToken); // cleanup
					return;
				}

				try
				{
					AddInstallationContainer(version, Task.CompletedTask);
					logger.LogDebug("Added detected BYOND version {versionKey}...", version);
				}
				catch (Exception ex)
				{
					logger.LogWarning(
						ex,
						"It seems that there are multiple directories that say they contain BYOND version {version}. We're ignoring and cleaning the duplicate: {duplicatePath}",
						version,
						ioManager.ResolvePath(path));
					await ioManager.DeleteDirectory(path, cancellationToken);
					return;
				}

				lock (installedVersionPaths)
					installedVersionPaths.Add(ioManager.ResolvePath(version.ToString()), version);
			}

			await Task.WhenAll(directories.Select(ReadVersion));

			logger.LogTrace("Upgrading BYOND installations...");
			await Task.WhenAll(installedVersionPaths.Select(kvp => byondInstaller.UpgradeInstallation(kvp.Value, kvp.Key, cancellationToken)));

			var activeVersionBytes = await activeVersionBytesTask;
			if (activeVersionBytes != null)
			{
				var activeVersionString = Encoding.UTF8.GetString(activeVersionBytes);

				Version activeVersion;
				bool hasRequestedActiveVersion;
				lock (installedVersions)
					hasRequestedActiveVersion = Version.TryParse(activeVersionString, out activeVersion)
						&& installedVersions.ContainsKey(activeVersion);

				if (hasRequestedActiveVersion)
					ActiveVersion = activeVersion; // not setting TCS because there's no need during init
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
		/// Ensures a BYOND <paramref name="version"/> is installed if it isn't already.
		/// </summary>
		/// <param name="progressReporter">The optional <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="version">The BYOND <see cref="Version"/> to install.</param>
		/// <param name="customVersionStream">Custom zip file <see cref="Stream"/> to use. Will cause a <see cref="Version.Build"/> number to be added.</param>
		/// <param name="neededForLock">If this BYOND version is required as part of a locking operation.</param>
		/// <param name="allowInstallation">If an installation should be performed if the <paramref name="version"/> is not installed. If <see langword="false"/> and an installation is required an <see cref="InvalidOperationException"/> will be thrown.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ByondExecutableLock"/>.</returns>
		async Task<ByondExecutableLock> AssertAndLockVersion(
			JobProgressReporter progressReporter,
			Version version,
			Stream customVersionStream,
			bool neededForLock,
			bool allowInstallation,
			CancellationToken cancellationToken)
		{
			var ourTcs = new TaskCompletionSource();
			ByondInstallation installation;
			ByondExecutableLock installLock;
			bool installedOrInstalling;
			lock (installedVersions)
			{
				if (customVersionStream != null)
				{
					var customInstallationNumber = 1;
					do
					{
						version = new Version(version.Major, version.Minor, customInstallationNumber++);
					}
					while (installedVersions.ContainsKey(version));
				}

				installedOrInstalling = installedVersions.TryGetValue(version, out var installationContainer);
				if (!installedOrInstalling)
				{
					if (!allowInstallation)
						throw new InvalidOperationException($"BYOND version {version} not installed!");

					installationContainer = AddInstallationContainer(version, ourTcs.Task);
				}

				installation = installationContainer.Instance;
				installLock = installationContainer.AddReference();
			}

			try
			{
				if (installedOrInstalling)
				{
					if (progressReporter != null)
						progressReporter.StageName = "Waiting for existing installation job...";

					if (neededForLock && !installation.InstallationTask.IsCompleted)
						logger.LogWarning("The required BYOND version ({version}) is not readily available! We will have to wait for it to install.", version);

					await installation.InstallationTask.WaitAsync(cancellationToken);
					return installLock;
				}

				// okay up to us to install it then
				try
				{
					if (customVersionStream != null)
						logger.LogInformation("Installing custom BYOND version as {version}...", version);
					else if (neededForLock)
					{
						if (version.Build > 0)
							throw new JobException(ErrorCode.ByondNonExistentCustomVersion);

						logger.LogWarning("The required BYOND version ({version}) is not readily available! We will have to install it.", version);
					}
					else
						logger.LogDebug("Requested BYOND version {version} not currently installed. Doing so now...", version);

					if (progressReporter != null)
						progressReporter.StageName = "Running event";

					var versionString = version.ToString();
					await eventConsumer.HandleEvent(EventType.ByondInstallStart, new List<string> { versionString }, false, cancellationToken);

					await InstallVersionFiles(progressReporter, version, customVersionStream, cancellationToken);

					ourTcs.SetResult();
				}
				catch (Exception ex)
				{
					if (ex is not OperationCanceledException)
						await eventConsumer.HandleEvent(EventType.ByondInstallFail, new List<string> { ex.Message }, false, cancellationToken);

					lock (installedVersions)
						installedVersions.Remove(version);

					ourTcs.SetException(ex);
					throw;
				}

				return installLock;
			}
			catch
			{
				installLock.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Installs the files for a given BYOND <paramref name="version"/>.
		/// </summary>
		/// <param name="progressReporter">The optional <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="version">The BYOND <see cref="Version"/> being installed with the <see cref="Version.Build"/> number set if appropriate.</param>
		/// <param name="customVersionStream">Custom zip file <see cref="Stream"/> to use. Will cause a <see cref="Version.Build"/> number to be added.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task InstallVersionFiles(JobProgressReporter progressReporter, Version version, Stream customVersionStream, CancellationToken cancellationToken)
		{
			var installFullPath = ioManager.ResolvePath(version.ToString());
			async Task DirectoryCleanup()
			{
				await ioManager.DeleteDirectory(installFullPath, cancellationToken);
				await ioManager.CreateDirectory(installFullPath, cancellationToken);
			}

			var directoryCleanupTask = DirectoryCleanup();
			try
			{
				Stream versionZipStream;
				if (customVersionStream == null)
				{
					if (progressReporter != null)
						progressReporter.StageName = "Downloading version";

					versionZipStream = await byondInstaller.DownloadVersion(version, cancellationToken);
				}
				else
					versionZipStream = customVersionStream;

				await using (versionZipStream)
				{
					if (progressReporter != null)
						progressReporter.StageName = "Cleaning target directory";

					await directoryCleanupTask;

					if (progressReporter != null)
						progressReporter.StageName = "Extracting zip";

					logger.LogTrace("Extracting downloaded BYOND zip to {extractPath}...", installFullPath);
					await ioManager.ZipToDirectory(installFullPath, versionZipStream, cancellationToken);
				}

				if (progressReporter != null)
					progressReporter.StageName = "Running installation actions";

				await byondInstaller.InstallByond(version, installFullPath, cancellationToken);

				if (progressReporter != null)
					progressReporter.StageName = "Writing version file";

				// make sure to do this last because this is what tells us we have a valid version in the future
				await ioManager.WriteAllBytes(
					ioManager.ConcatPath(installFullPath, VersionFileName),
					Encoding.UTF8.GetBytes(version.ToString()),
					cancellationToken);
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
				await ioManager.DeleteDirectory(installFullPath, cancellationToken);
				throw;
			}
		}

		/// <summary>
		/// Create and add a new <see cref="ByondInstallation"/> to <see cref="installedVersions"/>.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> being added.</param>
		/// <param name="installationTask">The <see cref="Task"/> representing the installation process.</param>
		/// <returns>The new <see cref="ReferenceCountingContainer{TWrapped, TReference}"/> containing the new <see cref="ByondInstallation"/>.</returns>
		ReferenceCountingContainer<ByondInstallation, ByondExecutableLock> AddInstallationContainer(Version version, Task installationTask)
		{
			var binPathForVersion = ioManager.ConcatPath(version.ToString(), BinPath);
			var installation = new ByondInstallation(
				installationTask,
				version,
				ioManager.ResolvePath(
					ioManager.ConcatPath(
						binPathForVersion,
						byondInstaller.GetDreamDaemonName(version, out var supportsCli))),
				ioManager.ResolvePath(
					ioManager.ConcatPath(
						binPathForVersion,
						byondInstaller.DreamMakerName)),
				supportsCli);

			var installationContainer = new ReferenceCountingContainer<ByondInstallation, ByondExecutableLock>(installation);

			lock (installedVersions)
				installedVersions.Add(version, installationContainer);

			return installationContainer;
		}

		/// <summary>
		/// Add a given <paramref name="fullDmbPath"/> to the trusted DMBs list in BYOND's config.
		/// </summary>
		/// <param name="fullDmbPath">Full path to the .dmb that should be trusted.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task TrustDmbPath(string fullDmbPath, CancellationToken cancellationToken)
		{
			var byondDir = byondInstaller.PathToUserByondFolder;
			if (String.IsNullOrWhiteSpace(byondDir))
			{
				logger.LogTrace("No relevant user BYOND directory to install a \"{fileName}\" in", TrustedDmbFileName);
				return;
			}

			var trustedFilePath = ioManager.ConcatPath(
				byondDir,
				CfgDirectoryName,
				TrustedDmbFileName);

			logger.LogDebug("Adding .dmb ({dmbPath}) to {trustedFilePath}", fullDmbPath, trustedFilePath);

			using (await SemaphoreSlimContext.Lock(UserFilesSemaphore, cancellationToken))
			{
				string trustedFileText;
				if (await ioManager.FileExists(trustedFilePath, cancellationToken))
				{
					var trustedFileBytes = await ioManager.ReadAllBytes(trustedFilePath, cancellationToken);
					trustedFileText = Encoding.UTF8.GetString(trustedFileBytes);
					trustedFileText = $"{trustedFileText.Trim()}{Environment.NewLine}";
				}
				else
					trustedFileText = String.Empty;

				if (trustedFileText.Contains(fullDmbPath, StringComparison.Ordinal))
					return;

				trustedFileText = $"{trustedFileText}{fullDmbPath}{Environment.NewLine}";

				var newTrustedFileBytes = Encoding.UTF8.GetBytes(trustedFileText);
				await ioManager.WriteAllBytes(trustedFilePath, newTrustedFileBytes, cancellationToken);
			}
		}
	}
}
