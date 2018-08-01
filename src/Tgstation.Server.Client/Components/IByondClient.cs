using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the <see cref="Byond"/> installation
	/// </summary>
	public interface IByondClient : IRightsClient<ByondRights>
	{
		/// <summary>
		/// Get the <see cref="Byond"/> represented by the <see cref="IByondClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Byond"/> represented by the <see cref="IByondClient"/></returns>
		Task<Byond> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Updates the installed BYOND <see cref="Version"/>
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to set to active</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetActiveVersion(Version version, CancellationToken cancellationToken);
	}
}
