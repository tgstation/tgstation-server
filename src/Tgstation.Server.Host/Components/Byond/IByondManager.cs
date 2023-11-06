using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// For managing the BYOND installation.
	/// </summary>
	/// <remarks>When passing in <see cref="Version"/>s, ensure they are BYOND format versions unless referring to a custom version. This means <see cref="Version.Build"/> should NEVER be 0.</remarks>
	public interface IByondManager : IComponentService, IDisposable
	{
		/// <summary>
		/// The currently active BYOND version.
		/// </summary>
		Version ActiveVersion { get; }

		/// <summary>
		/// The installed BYOND versions.
		/// </summary>
		IReadOnlyList<Version> InstalledVersions { get; }

		/// <summary>
		/// Change the active BYOND version.
		/// </summary>
		/// <param name="progressReporter">The optional <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="version">The new <see cref="Version"/>.</param>
		/// <param name="customVersionStream">Optional <see cref="Stream"/> of a custom BYOND version zip file.</param>
		/// <param name="allowInstallation">If an installation should be performed if the <paramref name="version"/> is not installed. If <see langword="false"/> and an installation is required an <see cref="InvalidOperationException"/> will be thrown.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask ChangeVersion(JobProgressReporter progressReporter, Version version, Stream customVersionStream, bool allowInstallation, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes a given BYOND version from the disk.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="version">The <see cref="Version"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask DeleteVersion(JobProgressReporter progressReporter, Version version, CancellationToken cancellationToken);

		/// <summary>
		/// Lock the current installation's location and return a <see cref="IByondExecutableLock"/>.
		/// </summary>
		/// <param name="requiredVersion">The BYOND <see cref="Version"/> required.</param>
		/// <param name="trustDmbFullPath">The optional full path to .dmb to trust while using the executables.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the requested <see cref="IByondExecutableLock"/>.</returns>
		ValueTask<IByondExecutableLock> UseExecutables(
			Version requiredVersion,
			string trustDmbFullPath,
			CancellationToken cancellationToken);
	}
}
