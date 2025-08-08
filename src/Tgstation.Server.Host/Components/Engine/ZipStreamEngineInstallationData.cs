using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Implementation of <see cref="IEngineInstallationData"/> for a zip file in a <see cref="Stream"/>.
	/// </summary>
	sealed class ZipStreamEngineInstallationData : IEngineInstallationData
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ZipStreamEngineInstallationData"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="Stream"/> containing the zip data of the engine.
		/// </summary>
		readonly Stream zipStream;

		/// <summary>
		/// Initializes a new instance of the <see cref="ZipStreamEngineInstallationData"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="zipStream">The value of <see cref="zipStream"/>.</param>
		public ZipStreamEngineInstallationData(IIOManager ioManager, Stream zipStream)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.zipStream = zipStream ?? throw new ArgumentNullException(nameof(zipStream));
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync() => zipStream.DisposeAsync();

		/// <inheritdoc />
		public ValueTask ExtractToPath(string path, CancellationToken cancellationToken)
			=> ioManager.ZipToDirectory(path, zipStream, cancellationToken);
	}
}
