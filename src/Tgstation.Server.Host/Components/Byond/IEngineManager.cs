using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// For managing the engine installations.
	/// </summary>
	/// <remarks>When passing in <see cref="ByondVersion.Version"/>s for <see cref="EngineType.Byond"/>, ensure they are BYOND format versions unless referring to a custom version. This means <see cref="Version.Build"/> should NEVER be 0.</remarks>
	public interface IEngineManager : IComponentService, IDisposable
	{
		/// <summary>
		/// The currently active <see cref="ByondVersion"/>.
		/// </summary>
		ByondVersion ActiveVersion { get; }

		/// <summary>
		/// The installed <see cref="ByondVersion"/>s.
		/// </summary>
		IReadOnlyList<ByondVersion> InstalledVersions { get; }

		/// <summary>
		/// Ensure that the given <paramref name="source"/> is registered for the given <paramref name="engine"/>.
		/// </summary>
		/// <param name="source">The <see cref="Uri"/> source of the <paramref name="engine"/>.</param>
		/// <param name="engine">The <see cref="EngineType"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask EnsureEngineSource(Uri source, EngineType engine, CancellationToken cancellationToken);

		/// <summary>
		/// Change the active <see cref="ByondVersion"/>.
		/// </summary>
		/// <param name="progressReporter">The optional <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="version">The new <see cref="ByondVersion"/>.</param>
		/// <param name="customVersionStream">Optional <see cref="Stream"/> of a custom BYOND version zip file.</param>
		/// <param name="allowInstallation">If an installation should be performed if the <paramref name="version"/> is not installed. If <see langword="false"/> and an installation is required an <see cref="InvalidOperationException"/> will be thrown.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask ChangeVersion(
			JobProgressReporter progressReporter,
			ByondVersion version,
			Stream customVersionStream,
			bool allowInstallation,
			CancellationToken cancellationToken);

		/// <summary>
		/// Deletes a given <paramref name="version"/> from the disk.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="version">The <see cref="ByondVersion"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		ValueTask DeleteVersion(JobProgressReporter progressReporter, ByondVersion version, CancellationToken cancellationToken);

		/// <summary>
		/// Lock the current installation's location and return a <see cref="IEngineExecutableLock"/>.
		/// </summary>
		/// <param name="requiredVersion">The <see cref="ByondVersion"/> required.</param>
		/// <param name="trustDmbFullPath">The optional full path to .dmb to trust while using the executables.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the requested <see cref="IEngineExecutableLock"/>.</returns>
		ValueTask<IEngineExecutableLock> UseExecutables(
			ByondVersion requiredVersion,
			string trustDmbFullPath,
			CancellationToken cancellationToken);
	}
}
