using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For managing the BYOND installation
	/// </summary>
	public interface IByondManager : IHostedService
	{
		/// <summary>
		/// The currently active BYOND version
		/// </summary>
		Version ActiveVersion { get; }

		/// <summary>
		/// Change the active BYOND version
		/// </summary>
		/// <param name="version">The new <see cref="Version"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		Task ChangeVersion(Version version, CancellationToken cancellationToken);

		/// <summary>
		/// Lock the current installation's location and return a <see cref="IByondExecutableLock"/>
		/// </summary>
		/// <param name="requiredVersion">The BYOND <see cref="Version"/> required</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the requested <see cref="IByondExecutableLock"/></returns>
		Task<IByondExecutableLock> UseExecutables(Version requiredVersion, CancellationToken cancellationToken);
	}
}