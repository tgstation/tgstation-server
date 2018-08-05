using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
		const string VersionFileName = "Version.txt";
		const string ActiveVersionFileName = "ActiveVersion.txt";

		const string BinPath = "byond/bin";

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

		static string VersionKey(Version version) => new Version(version.Major, version.Minor).ToString();

		/// <summary>
		/// Construct a <see cref="ByondManager"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="byondInstaller">The value of <see cref="byondInstaller"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public ByondManager(IIOManager ioManager, IByondInstaller byondInstaller, ILogger<ByondManager> logger)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.byondInstaller = byondInstaller ?? throw new ArgumentNullException(nameof(byondInstaller));
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
			if(installed)
				using (cancellationToken.Register(() => ourTcs.SetCanceled()))
				{
					await Task.WhenAny(ourTcs.Task, inProgressTask).ConfigureAwait(false);
					cancellationToken.ThrowIfCancellationRequested();
					return;
				}
			try
			{
				var downloadTask = byondInstaller.DownloadVersion(version, cancellationToken);

				//okay up to us to install it then
				await ioManager.DeleteDirectory(versionKey, cancellationToken).ConfigureAwait(false);
				await ioManager.CreateDirectory(versionKey, cancellationToken).ConfigureAwait(false);

				//byond can just decide to corrupt the zip fnr
				//(or maybe our downloader is a shite)
				//either way try a few times
				for (var I = 0; I < 3; ++I)
				{
					var download = await downloadTask.ConfigureAwait(false);
					try
					{
						await ioManager.ZipToDirectory(versionKey, download, cancellationToken).ConfigureAwait(false);
						break;
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch
					{
						if (I == 2)
							throw;
						downloadTask = byondInstaller.DownloadVersion(version, cancellationToken);
					}
				}
				await byondInstaller.InstallByond(ioManager.ResolvePath(versionKey), version, cancellationToken).ConfigureAwait(false);

				//make sure to do this last because this is what tells us we have a valid version in the future
				await ioManager.WriteAllBytes(ioManager.ConcatPath(versionKey, VersionFileName), Encoding.UTF8.GetBytes(version.ToString()), cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				lock (installedVersions)
					installedVersions.Remove(versionKey);
				throw;
			}
		}

		/// <inheritdoc />
		public async Task ChangeVersion(Version version, CancellationToken cancellationToken)
		{
			await InstallVersion(version, cancellationToken).ConfigureAwait(false);
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				await ioManager.WriteAllBytes(ActiveVersionFileName, Encoding.UTF8.GetBytes(version.ToString()), cancellationToken).ConfigureAwait(false);
				ActiveVersion = version;
			}
		}

		/// <inheritdoc />
		public async Task<IByondExecutableLock> UseExecutables(Version requiredVersion, CancellationToken cancellationToken)
		{
			var versionToUse = requiredVersion ?? ActiveVersion;
			if (versionToUse == null)
				throw new InvalidOperationException("No BYOND versions installed!");
			await InstallVersion(requiredVersion, cancellationToken).ConfigureAwait(false);

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
			var cacheCleanTask = byondInstaller.CleanCache(cancellationToken);

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

				await cacheCleanTask.ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
