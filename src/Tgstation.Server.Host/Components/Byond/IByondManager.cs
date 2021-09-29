using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// For managing the BYOND installation.
	/// </summary>
	public interface IByondManager : IHostedService, IDisposable
	{
		/// <summary>
		/// The currently active BYOND version.
		/// </summary>
		Version? ActiveVersion { get; }

		/// <summary>
		/// The installed BYOND versions.
		/// </summary>
		IReadOnlyList<Version> InstalledVersions { get; }

		/// <summary>
		/// Change the active BYOND version.
		/// </summary>
		/// <param name="version">The new <see cref="Version"/>.</param>
		/// <param name="customVersionStream">Optional <see cref="Stream"/> of a custom BYOND version zip file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task ChangeVersion(Version version, Stream? customVersionStream, CancellationToken cancellationToken);

		/// <summary>
		/// Lock the current installation's location and return a <see cref="IByondExecutableLock"/>.
		/// </summary>
		/// <param name="requiredVersion">The BYOND <see cref="Version"/> required.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the requested <see cref="IByondExecutableLock"/>.</returns>
		Task<IByondExecutableLock> UseExecutables(Version? requiredVersion, CancellationToken cancellationToken);
	}
}
