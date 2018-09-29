using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <inheritdoc />
	sealed class ByondManager : IByondManager
	{
		/// <summary>
		/// The path to the BYOND bin folder
		/// </summary>
		public const string BinPath = "byond/bin";

		/// <summary>
		/// The file in which we store the <see cref="VersionKey(Version)"/> for installations
		/// </summary>
		const string VersionFileName = "Version.txt";
		/// <summary>
		/// The file in which we store the <see cref="VersionKey(Version)"/> for the active installation
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
					return installedVersions.Select(x => Version.Parse(x.Key)).ToList();
			}
		}

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IByondInstaller"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IByondInstaller byondInstaller;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly ILogger<ByondManager> logger;

		/// <summary>
		/// Map of byond <see cref="Version"/>s to <see cref="Task"/>s that complete when they are installed
		/// </summary>
		readonly Dictionary<string, Task> installedVersions;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// Converts a BYOND <paramref name="version"/> to a <see cref="string"/>
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to convert</param>
		/// <returns>The <see cref="string"/> representation of <paramref name="version"/></returns>
		static string VersionKey(Version version) => new Version(version.Major, version.Minor).ToString();

		/// <summary>
		/// Construct a <see cref="ByondManager"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="byondInstaller">The value of <see cref="byondInstaller"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
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

		/// <summary>
		/// Installs a BYOND <paramref name="version"/> if it isn't already
		/// </summary>
		/// <param name="version">The BYOND <see cref="Version"/> to install</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task InstallVersion(Version version, CancellationToken cancellationToken)
		{
			var ourTcs = new TaskCompletionSource<object>();
			Task inProgressTask;

			var versionKey = VersionKey(version);
			bool installed;
			lock (installedVersions)
			{
				installed = installedVersions.TryGetValue(versionKey, out inProgressTask);
				if (!installed)
					installedVersions.Add(versionKey, ourTcs.Task);
			}
			if (installed)
				using (cancellationToken.Register(() => ourTcs.SetCanceled()))
				{
					await Task.WhenAny(ourTcs.Task, inProgressTask).ConfigureAwait(false);
					cancellationToken.ThrowIfCancellationRequested();
					return;
				}
			//okay up to us to install it then
			try
			{
				await eventConsumer.HandleEvent(EventType.ByondInstallStart, new List<string> { versionKey }, cancellationToken).ConfigureAwait(false);
				var downloadTask = byondInstaller.DownloadVersion(version, cancellationToken);

				await ioManager.DeleteDirectory(versionKey, cancellationToken).ConfigureAwait(false);
				await ioManager.CreateDirectory(versionKey, cancellationToken).ConfigureAwait(false);

				try
				{
					var download = await downloadTask.ConfigureAwait(false);
					await ioManager.ZipToDirectory(versionKey, download, cancellationToken).ConfigureAwait(false);
					await byondInstaller.InstallByond(ioManager.ResolvePath(versionKey), version, cancellationToken).ConfigureAwait(false);

					//make sure to do this last because this is what tells us we have a valid version in the future
					await ioManager.WriteAllBytes(ioManager.ConcatPath(versionKey, VersionFileName), Encoding.UTF8.GetBytes(version.ToString()), cancellationToken).ConfigureAwait(false);
				}
				catch (WebException e)
				{
					//since the user can easily provide non-exitent version numbers, we'll turn this into a JobException
					throw new JobException(String.Format(CultureInfo.InvariantCulture, "Error downloading BYOND version: {0}", e.Message));
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch
				{
					await ioManager.DeleteDirectory(versionKey, cancellationToken).ConfigureAwait(false);
					throw;
				}

				ourTcs.SetResult(null);
			}
			catch (Exception e)
			{
				if (!(e is OperationCanceledException))
					await eventConsumer.HandleEvent(EventType.ByondInstallFail, new List<string> { e.Message }, cancellationToken).ConfigureAwait(false);
				lock (installedVersions)
					installedVersions.Remove(versionKey);
				ourTcs.SetException(e);
				throw;
			}
		}

		/// <inheritdoc />
		public async Task ChangeVersion(Version version, CancellationToken cancellationToken)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));
			var versionKey = VersionKey(version);
			await InstallVersion(version, cancellationToken).ConfigureAwait(false);
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				await ioManager.WriteAllBytes(ActiveVersionFileName, Encoding.UTF8.GetBytes(versionKey), cancellationToken).ConfigureAwait(false);
				await eventConsumer.HandleEvent(EventType.ByondActiveVersionChange, new List<string> { ActiveVersion != null ? VersionKey(ActiveVersion) : null, versionKey }, cancellationToken).ConfigureAwait(false);
				ActiveVersion = version;
			}
		}

		/// <inheritdoc />
		public async Task<IByondExecutableLock> UseExecutables(Version requiredVersion, CancellationToken cancellationToken)
		{
			var versionToUse = requiredVersion ?? ActiveVersion;
			if (versionToUse == null)
				throw new JobException("No BYOND versions installed!");
			await InstallVersion(versionToUse, cancellationToken).ConfigureAwait(false);

			var versionKey = VersionKey(versionToUse);

			return new ByondExecutableLock
			{
				DreamDaemonPath = ioManager.ResolvePath(ioManager.ConcatPath(versionKey, BinPath, byondInstaller.DreamDaemonName)),
				DreamMakerPath = ioManager.ResolvePath(ioManager.ConcatPath(versionKey, BinPath, byondInstaller.DreamMakerName)),
				Version = versionToUse
			};
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			async Task<byte[]> GetActiveVersion()
			{
				if (!await ioManager.FileExists(ActiveVersionFileName, cancellationToken).ConfigureAwait(false))
					return null;
				return await ioManager.ReadAllBytes(ActiveVersionFileName, cancellationToken).ConfigureAwait(false);
			}

			var activeVersionBytesTask = GetActiveVersion();

			await ioManager.CreateDirectory(".", cancellationToken).ConfigureAwait(false);
			var directories = await ioManager.GetDirectories(".", cancellationToken).ConfigureAwait(false);

			async Task ReadVersion(string path)
			{
				var versionFile = ioManager.ConcatPath(path, VersionFileName);
				if (!await ioManager.FileExists(versionFile, cancellationToken).ConfigureAwait(false))
				{
					logger.LogInformation("Cleaning unparsable version path: {0}", ioManager.ResolvePath(path));
					await ioManager.DeleteDirectory(path, cancellationToken).ConfigureAwait(false); //cleanup
					return;
				}
				var bytes = await ioManager.ReadAllBytes(versionFile, cancellationToken).ConfigureAwait(false);
				var text = Encoding.UTF8.GetString(bytes);
				if (Version.TryParse(text, out var version))
				{
					var key = VersionKey(version);
					lock (installedVersions)
						if (!installedVersions.ContainsKey(key))
						{
							installedVersions.Add(key, Task.CompletedTask);
							return;
						}
				}
				await ioManager.DeleteDirectory(path, cancellationToken).ConfigureAwait(false);
			};

			await Task.WhenAll(directories.Select(x => ReadVersion(x))).ConfigureAwait(false);

			var activeVersionBytes = await activeVersionBytesTask.ConfigureAwait(false);
			if (activeVersionBytes != null)
			{
				var activeVersionString = Encoding.UTF8.GetString(activeVersionBytes);
				if (Version.TryParse(activeVersionString, out var activeVersion))
					ActiveVersion = activeVersion;
				else
					await ioManager.DeleteFile(ActiveVersionFileName, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
