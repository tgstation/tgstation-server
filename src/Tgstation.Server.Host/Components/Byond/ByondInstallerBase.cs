using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
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

		/// <inheritdoc />
		public abstract string DreamDaemonName { get; }

		/// <inheritdoc />
		public abstract string DreamMakerName { get; }

		/// <inheritdoc />
		public abstract string PathToUserByondFolder { get; }

		/// <summary>
		/// Gets the URL formatter string for downloading a byond version of {0:Major} {1:Minor}.
		/// </summary>
		protected abstract string ByondRevisionsURLTemplate { get; }

		/// <summary>
		/// Gets the <see cref="IIOManager"/> for the <see cref="ByondInstallerBase"/>.
		/// </summary>
		protected IIOManager IOManager { get; }

		/// <summary>
		/// Gets the <see cref="ILogger"/> for the <see cref="ByondInstallerBase"/>.
		/// </summary>
		protected ILogger<ByondInstallerBase> Logger { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondInstallerBase"/> <see langword="class"/>.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="IOManager"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		protected ByondInstallerBase(IIOManager ioManager, ILogger<ByondInstallerBase> logger)
		{
			IOManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task CleanCache(CancellationToken cancellationToken)
		{
			try
			{
				Logger.LogDebug("Cleaning BYOND cache...");
				await IOManager.DeleteDirectory(
					IOManager.ConcatPath(
						PathToUserByondFolder,
						CacheDirectoryName),
					cancellationToken)
					.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				Logger.LogWarning(e, "Error deleting BYOND cache!");
			}
		}

		/// <inheritdoc />
		public abstract Task InstallByond(string path, Version version, CancellationToken cancellationToken);

		/// <inheritdoc />
		public Task<byte[]> DownloadVersion(Version version, CancellationToken cancellationToken)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			var url = String.Format(CultureInfo.InvariantCulture, ByondRevisionsURLTemplate, version.Major, version.Minor);

			Logger.LogTrace("Downloading from: {0}", url);

			return IOManager.DownloadFile(new Uri(url), cancellationToken);
		}
	}
}
