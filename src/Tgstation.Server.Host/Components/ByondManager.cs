using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class ByondManager : IByondManager
	{
		const string VersionFileName = "Version.txt";

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
			using (var zipBytes = await downloadTask.ConfigureAwait(false))
			using (var archive = new ZipArchive(zipBytes))
				await Task.Factory.StartNew(() => archive.ExtractToDirectory(resolvedPath), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

			await byondInstaller.InstallByond(resolvedPath, cancellationToken).ConfigureAwait(false);

			//make sure to do this last because this is what tells us we have a valid version
			await ioManager.WriteAllBytes(ioManager.ConcatPath(versionKey, VersionFileName), Encoding.UTF8.GetBytes(version.ToString()), cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task ChangeVersion(Version version, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task ClearCache(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<Version> GetVersion(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public IByondExecutableLock UseExecutables(Version requiredVersion)
		{
			throw new NotImplementedException();
		}
	}
}
