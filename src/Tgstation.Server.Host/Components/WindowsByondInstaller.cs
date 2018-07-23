using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// <see cref="IByondInstaller"/> for windows systems
	/// </summary>
	sealed class WindowsByondInstaller : IByondInstaller
	{
		/// <summary>
		/// The URL format string for getting BYOND windows version {0}.{1} zipfile
		/// </summary>
		const string ByondRevisionsURL = "https://secure.byond.com/download/build/{0}/{0}.{1}_byond.zip";

		/// <inheritdoc />
		public string DreamDaemonName => "dreamdaemon.exe";

		/// <inheritdoc />
		public string DreamMakerName => "dm.exe";

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="WindowsByondInstaller"/>
		/// </summary>
		readonly IIOManager ioManager;

		public Task CleanCache(CancellationToken cancellationToken) => ioManager.DeleteDirectory(ioManager.ConcatPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "byond/cache"), cancellationToken);

		public Task<byte[]> DownloadVersion(Version version, CancellationToken cancellationToken)
		{
			var url = String.Format(CultureInfo.InvariantCulture, ByondRevisionsURL, version.Major, version.Major);

			return ioManager.DownloadFile(new Uri(url), cancellationToken);
		}

		public Task InstallByond(string path, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
