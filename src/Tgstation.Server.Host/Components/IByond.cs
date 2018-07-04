using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For managing the BYOND installation
	/// </summary>
	public interface IByond
	{
		/// <summary>
		/// Change the current BYOND version
		/// </summary>
		/// <param name="version">The new <see cref="Version"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		Task ChangeVersion(Version version, CancellationToken cancellationToken);

		/// <summary>
		/// Get the currently installed BYOND version
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>The current BYOND version</returns>
		Task<Version> GetVersion(CancellationToken cancellationToken);

		/// <summary>
		/// Lock the current installation's location and return a <see cref="IByondExecutableLock"/>
		/// </summary>
		/// <param name="requiredVersion">The BYOND <see cref="Version"/> required</param>
		IByondExecutableLock UseExecutables(Version requiredVersion);

		/// <summary>
		/// Clears the cache folder
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ClearCache(CancellationToken cancellationToken);
	}
}