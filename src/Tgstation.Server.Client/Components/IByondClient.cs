using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the BYOND installation
	/// </summary>
	public interface IByondClient : IClient<ByondRights, Byond>
	{
		/// <summary>
		/// Updates the installed BYOND <see cref="Version"/>
		/// </summary>
		/// <param name="byond">The <see cref="Byond"/> to update</param>
		/// <param name="progressCallback">Optional <see cref="Action{T1}"/> taking a <see cref="ByondStatus"/> to run when it changes</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Update(Byond byond, Action<ByondStatus> progressCallback, CancellationToken cancellationToken);

		/// <summary>
		/// Get the currently installed BYOND <see cref="Version"/>
		/// </summary>
		/// <param name="staged">Read the staged <see cref="Version"/> instead if applicable</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the installed BYOND version or <see langword="null"/> if none is installed</returns>
		Task<Version> GetVersion(bool staged, CancellationToken cancellationToken);
	}
}
