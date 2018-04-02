using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the BYOND installation
	/// </summary>
	public interface IByondClient : IClient<ByondRights>
	{
		/// <summary>
		/// Gets the current status of any BYOND updates
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ByondStatus"/> updater</returns>
		Task<ByondStatus> CurrentStatus(CancellationToken cancellationToken);

		/// <summary>
		/// Updates the installed BYOND <see cref="Version"/>
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to update to. Only considers the <see cref="Version.Major"/> and <see cref="Version.Minor"/> numbers</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task UpdateToVersion(Version version, CancellationToken cancellationToken);

		/// <summary>
		/// Get the currently installed BYOND <see cref="Version"/>
		/// </summary>
		/// <param name="staged">Read the staged <see cref="Version"/> instead if applicable</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the installed BYOND version or <see langword="null"/> if none is installed</returns>
		Task<Version> GetVersion(bool staged, CancellationToken cancellationToken);
	}
}
