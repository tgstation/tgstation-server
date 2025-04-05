﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <inheritdoc />
	sealed class EngineManager : IEngineManager
	{
		/// <summary>
		/// The file in which we store the <see cref="Version"/> for installations.
		/// </summary>
		const string VersionFileName = "Version.txt";

		/// <summary>
		/// The file in which we store the <see cref="ActiveVersion"/>.
		/// </summary>
		const string ActiveVersionFileName = "ActiveVersion.txt";

		/// <inheritdoc />
		public EngineVersion? ActiveVersion { get; private set; }

		/// <inheritdoc />
		public IReadOnlyList<EngineVersion> InstalledVersions
		{
			get
			{
				lock (installedVersions)
					return installedVersions.Keys.ToList();
			}
		}

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="EngineManager"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IEngineInstaller"/> for the <see cref="EngineManager"/>.
		/// </summary>
		readonly IEngineInstaller engineInstaller;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="EngineManager"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="EngineManager"/>.
		/// </summary>
		readonly ILogger<EngineManager> logger;

		/// <summary>
		/// Map of byond <see cref="EngineVersion"/>s to <see cref="Task"/>s that complete when they are installed.
		/// </summary>
		readonly Dictionary<EngineVersion, ReferenceCountingContainer<IEngineInstallation, EngineExecutableLock>> installedVersions;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for changing or deleting the active BYOND version.
		/// </summary>
		readonly SemaphoreSlim changeDeleteSemaphore;

		/// <summary>
		/// <see cref="TaskCompletionSource"/> that notifes when the <see cref="ActiveVersion"/> changes.
		/// </summary>
		volatile TaskCompletionSource activeVersionChanged;

		/// <summary>
		/// Validates a given <paramref name="version"/> parameter.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to validate.</param>
		static void CheckVersionParameter(EngineVersion version)
		{
			ArgumentNullException.ThrowIfNull(version);

			if (!version.Engine.HasValue)
				throw new InvalidOperationException("version.Engine cannot be null!");

			if (version.CustomIteration == 0)
				throw new InvalidOperationException("version.CustomIteration cannot be 0!");
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EngineManager"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="engineInstaller">The value of <see cref="engineInstaller"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public EngineManager(IIOManager ioManager, IEngineInstaller engineInstaller, IEventConsumer eventConsumer, ILogger<EngineManager> logger)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.engineInstaller = engineInstaller ?? throw new ArgumentNullException(nameof(engineInstaller));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			installedVersions = new Dictionary<EngineVersion, ReferenceCountingContainer<IEngineInstallation, EngineExecutableLock>>();
			changeDeleteSemaphore = new SemaphoreSlim(1);
			activeVersionChanged = new TaskCompletionSource();
		}

		/// <inheritdoc />
		public void Dispose() => changeDeleteSemaphore.Dispose();

		/// <inheritdoc />
		public async ValueTask ChangeVersion(
			JobProgressReporter progressReporter,
			EngineVersion version,
			Stream? customVersionStream,
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
				version = new EngineVersion(installLock.Version);

				var stringVersion = version.ToString();
				await ioManager.WriteAllBytes(ActiveVersionFileName, Encoding.UTF8.GetBytes(stringVersion), cancellationToken);
				await eventConsumer.HandleEvent(
					EventType.EngineActiveVersionChange,
					new List<string?>
					{
						ActiveVersion?.ToString(),
						stringVersion,
					},
					false,
					cancellationToken);

				ActiveVersion = version;

				logger.LogInformation("Active version changed to {version}", version);
				var oldTcs = Interlocked.Exchange(ref activeVersionChanged, new TaskCompletionSource());
				oldTcs.SetResult();
			}
		}

		/// <inheritdoc />
		public async ValueTask<IEngineExecutableLock> UseExecutables(EngineVersion? requiredVersion, string? trustDmbFullPath, CancellationToken cancellationToken)
		{
			logger.LogTrace(
				"Acquiring lock on BYOND version {version}...",
				requiredVersion?.ToString() ?? $"{ActiveVersion} (active)");
			var versionToUse = requiredVersion ?? ActiveVersion ?? throw new JobException(ErrorCode.EngineNoVersionsInstalled);

			using var progressReporter = new JobProgressReporter();

			var installLock = await AssertAndLockVersion(
				progressReporter,
				versionToUse,
				null,
				requiredVersion != null,
				true,
				cancellationToken);
			try
			{
				if (trustDmbFullPath != null)
					await engineInstaller.TrustDmbPath(installLock.Version, trustDmbFullPath, cancellationToken);

				return installLock;
			}
			catch
			{
				installLock.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public async ValueTask DeleteVersion(JobProgressReporter progressReporter, EngineVersion version, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(progressReporter);

			CheckVersionParameter(version);

			logger.LogTrace("DeleteVersion {version}", version);

			var activeVersion = ActiveVersion;
			if (activeVersion != null && version.Equals(activeVersion))
				throw new JobException(ErrorCode.EngineCannotDeleteActiveVersion);

			ReferenceCountingContainer<IEngineInstallation, EngineExecutableLock> container;
			logger.LogTrace("Waiting to acquire installedVersions lock...");
			lock (installedVersions)
			{
				if (!installedVersions.TryGetValue(version, out var containerNullable))
				{
					logger.LogTrace("Version {version} already deleted.", version);
					return;
				}

				container = containerNullable;
				logger.LogTrace("Installation container acquired for deletion");
			}

			progressReporter.StageName = "Waiting for version to not be in use...";
			while (true)
			{
				var containerTask = container.OnZeroReferences;

				// We also want to check when the active version changes in case we need to fail the job because of that.
				Task activeVersionUpdate;
				using (await SemaphoreSlimContext.Lock(changeDeleteSemaphore, cancellationToken))
					activeVersionUpdate = activeVersionChanged.Task;

				logger.LogTrace("Waiting for container.OnZeroReferences or switch of active version...");
				await Task.WhenAny(
					containerTask,
					activeVersionUpdate)
					.WaitAsync(cancellationToken);

				if (containerTask.IsCompleted)
					logger.LogTrace("All locks for version {version} are gone", version);
				else
					logger.LogTrace("activeVersion changed, we may have to wait again. Acquiring semaphore...");

				using (await SemaphoreSlimContext.Lock(changeDeleteSemaphore, cancellationToken))
				{
					// check again because it could have become the active version.
					activeVersion = ActiveVersion;
					if (activeVersion != null && version.Equals(activeVersion))
						throw new JobException(ErrorCode.EngineCannotDeleteActiveVersion);

					bool proceed;
					logger.LogTrace("Locking installedVersions...");
					lock (installedVersions)
					{
						proceed = container.OnZeroReferences.IsCompleted;
						if (proceed)
							if (!installedVersions.TryGetValue(version, out var newerContainer))
								logger.LogWarning("Unable to remove engine installation {version} from list! Is there a duplicate job running?", version);
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
								{
									logger.LogTrace("Proceeding with installation deletion...");
									installedVersions.Remove(version);
								}
							}
					}

					if (proceed)
					{
						logger.LogInformation("Deleting version {version}...", version);
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
					else
						logger.LogTrace("Not proceeding for some reason or another");
				}
			}
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			async ValueTask<Memory<byte>?> GetActiveVersion()
			{
				var activeVersionFileExists = await ioManager.FileExists(ActiveVersionFileName, cancellationToken);
				return !activeVersionFileExists ? (Memory<byte>?)null : await ioManager.ReadAllBytes(ActiveVersionFileName, cancellationToken);
			}

			var activeVersionBytesTask = GetActiveVersion();

			await ioManager.CreateDirectory(DefaultIOManager.CurrentDirectory, cancellationToken);
			var directories = await ioManager.GetDirectories(DefaultIOManager.CurrentDirectory, cancellationToken);

			var installedVersionPaths = new Dictionary<string, EngineVersion>();

			async ValueTask ReadVersion(string path)
			{
				var versionFile = ioManager.ConcatPath(path, VersionFileName);
				if (!await ioManager.FileExists(versionFile, cancellationToken))
				{
					logger.LogWarning("Cleaning path with no version file: {versionPath}", ioManager.ResolvePath(path));
					await ioManager.DeleteDirectory(path, cancellationToken); // cleanup
					return;
				}

				var bytes = await ioManager.ReadAllBytes(versionFile, cancellationToken);
				var text = Encoding.UTF8.GetString(bytes.Span);
				EngineVersion version;
				if (!EngineVersion.TryParse(text, out var versionNullable))
				{
					logger.LogWarning("Cleaning path with unparsable version file: {versionPath}", ioManager.ResolvePath(path));
					await ioManager.DeleteDirectory(path, cancellationToken); // cleanup
					return;
				}
				else
					version = versionNullable!;

				try
				{
					var installation = await engineInstaller.CreateInstallation(version, path, Task.CompletedTask, cancellationToken);
					AddInstallationContainer(installation);
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

			await ValueTaskExtensions.WhenAll(
				directories
					.Select(ReadVersion));

			logger.LogTrace("Upgrading BYOND installations...");
			await ValueTaskExtensions.WhenAll(
				installedVersionPaths
					.Select(kvp => engineInstaller.UpgradeInstallation(kvp.Value, kvp.Key, cancellationToken)));

			var activeVersionBytes = await activeVersionBytesTask;
			if (activeVersionBytes.HasValue)
			{
				var activeVersionString = Encoding.UTF8.GetString(activeVersionBytes.Value.Span);

				EngineVersion? activeVersion;
				bool hasRequestedActiveVersion;
				lock (installedVersions)
					hasRequestedActiveVersion = EngineVersion.TryParse(activeVersionString, out activeVersion)
						&& installedVersions.ContainsKey(activeVersion!);

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
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="version">The <see cref="EngineVersion"/> to install.</param>
		/// <param name="customVersionStream">Optional custom zip file <see cref="Stream"/> to use. Will cause a <see cref="Version.Build"/> number to be added.</param>
		/// <param name="neededForLock">If this BYOND version is required as part of a locking operation.</param>
		/// <param name="allowInstallation">If an installation should be performed if the <paramref name="version"/> is not installed. If <see langword="false"/> and an installation is required an <see cref="InvalidOperationException"/> will be thrown.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="EngineExecutableLock"/>.</returns>
		async ValueTask<EngineExecutableLock> AssertAndLockVersion(
			JobProgressReporter progressReporter,
			EngineVersion version,
			Stream? customVersionStream,
			bool neededForLock,
			bool allowInstallation,
			CancellationToken cancellationToken)
		{
			var ourTcs = new TaskCompletionSource();
			IEngineInstallation installation;
			EngineExecutableLock installLock;
			bool installedOrInstalling;

			// loop is because of the race condition with potentialInstallation, installedVersions, and CustomIteration selection
			while (true)
			{
				lock (installedVersions)
				{
					if (customVersionStream != null)
					{
						var customInstallationNumber = 1;
						do
						{
							version.CustomIteration = customInstallationNumber++;
						}
						while (installedVersions.ContainsKey(version));
					}
				}

				var potentialInstallation = await engineInstaller.CreateInstallation(
					version,
					ioManager.ResolvePath(version.ToString()),
					ourTcs.Task,
					cancellationToken);

				lock (installedVersions)
				{
					if (customVersionStream != null && installedVersions.ContainsKey(version))
						continue;

					installedOrInstalling = installedVersions.TryGetValue(version, out var installationContainerNullable);
					ReferenceCountingContainer<IEngineInstallation, EngineExecutableLock> installationContainer;
					if (!installedOrInstalling)
					{
						if (!allowInstallation)
							throw new InvalidOperationException($"Engine version {version} not installed!");

						installationContainer = AddInstallationContainer(potentialInstallation);
					}
					else
						installationContainer = installationContainerNullable!;

					installation = installationContainer.Instance;
					installLock = installationContainer.AddReference();
				}

				var deploymentPipelineProcesses = !neededForLock;
				try
				{
					if (installedOrInstalling)
					{
						progressReporter.StageName = "Waiting for existing installation job...";

						if (neededForLock && !installation.InstallationTask.IsCompleted)
							logger.LogWarning("The required engine version ({version}) is not readily available! We will have to wait for it to install.", version);

						await installation.InstallationTask.WaitAsync(cancellationToken);
						return installLock;
					}

					// okay up to us to install it then
					string? installPath = null;
					try
					{
						if (customVersionStream != null)
							logger.LogInformation("Installing custom engine version as {version}...", version);
						else if (neededForLock)
						{
							if (version.CustomIteration.HasValue)
								throw new JobException(ErrorCode.EngineNonExistentCustomVersion);

							logger.LogWarning("The required engine version ({version}) is not readily available! We will have to install it.", version);
						}
						else
							logger.LogInformation("Requested engine version {version} not currently installed. Doing so now...", version);

						progressReporter.StageName = "Running event";

						var versionString = version.ToString();
						await eventConsumer.HandleEvent(EventType.EngineInstallStart, new List<string> { versionString }, deploymentPipelineProcesses, cancellationToken);

						installPath = await InstallVersionFiles(progressReporter, version, customVersionStream, deploymentPipelineProcesses, cancellationToken);
						await eventConsumer.HandleEvent(EventType.EngineInstallComplete, new List<string> { versionString }, deploymentPipelineProcesses, cancellationToken);

						ourTcs.SetResult();
					}
					catch (Exception ex)
					{
						if (installPath != null)
						{
							try
							{
								logger.LogDebug("Cleaning up failed installation at {path}...", installPath);
								await ioManager.DeleteDirectory(installPath, cancellationToken);
							}
							catch (Exception ex2)
							{
								logger.LogError(ex2, "Error cleaning up failed installation!");
							}
						}
						else if (ex is not OperationCanceledException)
							await eventConsumer.HandleEvent(EventType.EngineInstallFail, new List<string> { ex.Message }, deploymentPipelineProcesses, cancellationToken);

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
		}

		/// <summary>
		/// Installs the files for a given BYOND <paramref name="version"/>.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="version">The <see cref="EngineVersion"/> being installed with the <see cref="Version.Build"/> number set if appropriate.</param>
		/// <param name="customVersionStream">Custom zip file <see cref="Stream"/> to use. Will cause a <see cref="Version.Build"/> number to be added.</param>
		/// <param name="deploymentPipelineProcesses">If processes should be launched as part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the directory the engine was installed to.</returns>
		async ValueTask<string> InstallVersionFiles(
			JobProgressReporter progressReporter,
			EngineVersion version,
			Stream? customVersionStream,
			bool deploymentPipelineProcesses,
			CancellationToken cancellationToken)
		{
			var installFullPath = ioManager.ResolvePath(version.ToString());
			async ValueTask DirectoryCleanup()
			{
				await ioManager.DeleteDirectory(installFullPath, cancellationToken);
				await ioManager.CreateDirectory(installFullPath, cancellationToken);
			}

			var directoryCleanupTask = DirectoryCleanup();
			try
			{
				IEngineInstallationData engineInstallationData;
				var remainingProgress = 1.0;
				if (customVersionStream == null)
				{
					using var subReporter = progressReporter.CreateSection("Downloading Version", 0.5);
					remainingProgress -= 0.5;
					engineInstallationData = await engineInstaller.DownloadVersion(version, subReporter, cancellationToken);
				}
				else
#pragma warning disable CA2000 // Dispose objects before losing scope, false positive
					engineInstallationData = new ZipStreamEngineInstallationData(
						ioManager,
						customVersionStream);
#pragma warning restore CA2000 // Dispose objects before losing scope

				JobProgressReporter remainingReporter;
				try
				{
					remainingReporter = progressReporter.CreateSection(null, remainingProgress);
				}
				catch
				{
					await engineInstallationData.DisposeAsync();
					throw;
				}

				using (remainingReporter)
				{
					await using (engineInstallationData)
					{
						remainingReporter.StageName = "Cleaning target directory";

						await directoryCleanupTask;
						remainingReporter.ReportProgress(0.1);
						remainingReporter.StageName = "Extracting data";

						logger.LogTrace("Extracting engine to {extractPath}...", installFullPath);
						await engineInstallationData.ExtractToPath(installFullPath, cancellationToken);
						remainingReporter.ReportProgress(0.3);
					}

					remainingReporter.StageName = "Running installation actions";

					await engineInstaller.Install(version, installFullPath, deploymentPipelineProcesses, cancellationToken);

					remainingReporter.ReportProgress(0.9);
					remainingReporter.StageName = "Writing version file";

					// make sure to do this last because this is what tells us we have a valid version in the future
					await ioManager.WriteAllBytes(
						ioManager.ConcatPath(installFullPath, VersionFileName),
						Encoding.UTF8.GetBytes(version.ToString()),
						cancellationToken);
				}
			}
			catch (HttpRequestException ex)
			{
				// since the user can easily provide non-exitent version numbers, we'll turn this into a JobException
				throw new JobException(ErrorCode.EngineDownloadFail, ex);
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

			return installFullPath;
		}

		/// <summary>
		/// Create and add a new <see cref="IEngineInstallation"/> to <see cref="installedVersions"/>.
		/// </summary>
		/// <param name="installation">The <see cref="IEngineInstallation"/> being added.</param>
		/// <returns>A new <see cref="ReferenceCountingContainer{TWrapped, TReference}"/> for the <see cref="IEngineInstallation"/>/<see cref="EngineExecutableLock"/>.</returns>
		ReferenceCountingContainer<IEngineInstallation, EngineExecutableLock> AddInstallationContainer(IEngineInstallation installation)
		{
			var installationContainer = new ReferenceCountingContainer<IEngineInstallation, EngineExecutableLock>(installation);

			lock (installedVersions)
				installedVersions.Add(installation.Version, installationContainer);

			return installationContainer;
		}
	}
}
