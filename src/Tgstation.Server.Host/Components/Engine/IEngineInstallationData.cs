using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Wraps data containing an engine installation.
	/// </summary>
	interface IEngineInstallationData : IAsyncDisposable
	{
		/// <summary>
		/// Extracts the installation to a given path.
		/// </summary>
		/// <param name="path">The full path to extract to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask ExtractToPath(string path, CancellationToken cancellationToken);
	}
}
