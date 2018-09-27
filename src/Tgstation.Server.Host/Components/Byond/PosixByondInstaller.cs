using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// <see cref="IByondInstaller"/> for Posix systems
	/// </summary>
	sealed class PosixByondInstaller : IByondInstaller
	{
		/// <summary>
		/// The URL format string for getting BYOND linux version {0}.{1} zipfile
		/// </summary>
		const string ByondRevisionsURL = "https://secure.byond.com/download/build/{0}/{0}.{1}_byond_linux.zip";
		/// <summary>
		/// Path to the BYOND cache
		/// </summary>
		const string ByondCachePath = "~/.byond/cache";

		/// <inheritdoc />
		public string DreamDaemonName => "DreamDaemon.sh";

		/// <inheritdoc />
		public string DreamMakerName => "DreamMaker.sh";

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="PosixByondInstaller"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IPostWriteHandler"/> for the <see cref="PosixByondInstaller"/>
		/// </summary>
		readonly IPostWriteHandler postWriteHandler;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="PosixByondInstaller"/>
		/// </summary>
		readonly ILogger<PosixByondInstaller> logger;

		/// <summary>
		/// Construct a <see cref="WindowsByondInstaller"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="postWriteHandler">The value of <see cref="postWriteHandler"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public PosixByondInstaller(IIOManager ioManager, IPostWriteHandler postWriteHandler, ILogger<PosixByondInstaller> logger)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.postWriteHandler = postWriteHandler ?? throw new ArgumentNullException(nameof(postWriteHandler));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task CleanCache(CancellationToken cancellationToken)
		{
			try
			{
				await ioManager.DeleteDirectory(ByondCachePath, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				logger.LogWarning("Error deleting BYOND cache! Exception: {0}", e);
			}
		}

		/// <inheritdoc />
		public async Task<byte[]> DownloadVersion(Version version, CancellationToken cancellationToken)
		{
			var ourVersion = version;
			//lummox is annoying and doesn't like to post linux versions if nothing changed in DreamDaemon/DM
			//if this for's exit condition ever triggers, i get to say I told you so
			Exception lastException = null;
			for (var I = 0; I < 5 && ourVersion.Minor >= 1; ++I, ourVersion = new Version(ourVersion.Major, ourVersion.Minor))
			{
				try
				{
					var url = String.Format(CultureInfo.InvariantCulture, ByondRevisionsURL, ourVersion.Major, ourVersion.Minor);

					return await ioManager.DownloadFile(new Uri(url), cancellationToken).ConfigureAwait(false);
				}
				catch (WebException e)
				{
					if (!(e.Status == WebExceptionStatus.ProtocolError && e.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.NotFound))
						throw;
					lastException = e;
				}
			}
			throw lastException;
		}

		/// <inheritdoc />
		public Task InstallByond(string path, Version version, CancellationToken cancellationToken)
		{
			//write the scripts for running the ting
			//need to add $ORIGIN to LD_LIBRARY_PATH
			const string StandardScript = "#!/bin/sh\nexport LD_LIBRARY_PATH=\"\\$ORIGIN:$LD_LIBRARY_PATH\"\nBASEDIR=$(dirname \"$0\")\nexec \"$BASEDIR/{0}\" \"$@\"\n";

			const string DreamDaemonExecutableName = "DreamDaemon";
			const string DreamMakerExecutableName = "DreamMaker";

			var dreamDaemonScript = String.Format(CultureInfo.InvariantCulture, StandardScript, DreamDaemonExecutableName);
			var dreamMakerScript = String.Format(CultureInfo.InvariantCulture, StandardScript, DreamMakerExecutableName);

			async Task WriteAndMakeExecutable(string fullPath, string script)
			{
				await ioManager.WriteAllBytes(fullPath, Encoding.ASCII.GetBytes(script), cancellationToken).ConfigureAwait(false);
				postWriteHandler.HandleWrite(fullPath);
			}

			var basePath = ioManager.ConcatPath(path, ByondManager.BinPath);

			var task = Task.WhenAll(WriteAndMakeExecutable(ioManager.ConcatPath(basePath, DreamDaemonName), dreamDaemonScript), WriteAndMakeExecutable(ioManager.ConcatPath(basePath, DreamMakerName), dreamMakerScript));

			postWriteHandler.HandleWrite(ioManager.ConcatPath(basePath, DreamDaemonExecutableName));
			postWriteHandler.HandleWrite(ioManager.ConcatPath(basePath, DreamMakerExecutableName));

			return task;
		}
	}
}
