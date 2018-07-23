using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.IO;

using MemoryStream = System.IO.MemoryStream;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class ByondManager : IByondManager
	{
		const string VersionFileName = "Version.txt";
		const string ActiveVersionFileName = "ActiveVersion.txt";

		const string BinPath = "byond/bin";

		/// <inheritdoc />
		public Version ActiveVersion { get; private set; }

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
		}

		static string VersionKey(Version version) => new Version(version.Major, version.Minor).ToString();

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
					return;
				}

			var downloadTask = byondInstaller.DownloadVersion(version, cancellationToken);

			//okay up to us to install it then
			await ioManager.DeleteDirectory(versionKey, cancellationToken).ConfigureAwait(false);
			await ioManager.CreateDirectory(versionKey, cancellationToken).ConfigureAwait(false);

			var resolvedPath = ioManager.ResolvePath(versionKey);
			using (var zipBytes = new MemoryStream(await downloadTask.ConfigureAwait(false)))
			using (var archive = new ZipArchive(zipBytes))
				await Task.Factory.StartNew(() => archive.ExtractToDirectory(resolvedPath), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

			await byondInstaller.InstallByond(resolvedPath, version, cancellationToken).ConfigureAwait(false);

			//make sure to do this last because this is what tells us we have a valid version
			await ioManager.WriteAllBytes(ioManager.ConcatPath(versionKey, VersionFileName), Encoding.UTF8.GetBytes(version.ToString()), cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ChangeVersion(Version version, CancellationToken cancellationToken)
		{
			await InstallVersion(version, cancellationToken).ConfigureAwait(false);
			await ioManager.WriteAllBytes(ActiveVersionFileName, Encoding.UTF8.GetBytes(version.ToString()), cancellationToken).ConfigureAwait(false);
			ActiveVersion = version;
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
				DreamDaemonPath = ioManager.ResolvePath(ioManager.ConcatPath(versionKey, byondInstaller.DreamDaemonName)),
				DreamMakerPath = ioManager.ResolvePath(ioManager.ConcatPath(versionKey, byondInstaller.DreamMakerName)),
				Version = versionToUse
			};
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var cacheCleanTask = byondInstaller.CleanCache(cancellationToken);

			var activeVersionBytesTask = ioManager.ReadAllBytes(ActiveVersionFileName, cancellationToken);

			var directories = await ioManager.GetDirectories(".", cancellationToken).ConfigureAwait(false);

			async Task ReadVersion(string path)
			{
				var bytes = await ioManager.ReadAllBytes(ioManager.ConcatPath(path, VersionFileName), cancellationToken).ConfigureAwait(false);
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

			var activeVersionString = Encoding.UTF8.GetString(await activeVersionBytesTask.ConfigureAwait(false));
			if (Version.TryParse(activeVersionString, out var activeVersion))
				ActiveVersion = activeVersion;
			else
				await ioManager.DeleteFile(ActiveVersionFileName, cancellationToken).ConfigureAwait(false);

			await cacheCleanTask.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
