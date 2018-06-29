using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For managing <see cref="IInstance"/>s
	/// </summary>
	public interface IInstanceManager : IInstanceShutdownHandler
	{
		/// <summary>
		/// Get the <see cref="IInstance"/> associated with given <paramref name="metadata"/>
		/// </summary>
		/// <param name="metadata">The <see cref="Host.Models.Instance"/> of the desired <see cref="IInstance"/></param>
		/// <returns>The <see cref="IInstance"/> associated with the given <paramref name="metadata"/></returns>
		IInstance GetInstance(Host.Models.Instance metadata);

		/// <summary>
		/// Online an <see cref="IInstance"/>
		/// </summary>
		/// <param name="metadata">The <see cref="Host.Models.Instance"/> of the desired <see cref="IInstance"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task OnlineInstance(Host.Models.Instance metadata, CancellationToken cancellationToken);

		/// <summary>
		/// Offline an <see cref="IInstance"/>
		/// </summary>
		/// <param name="metadata">The <see cref="Host.Models.Instance"/> of the desired <see cref="IInstance"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task OfflineInstance(Host.Models.Instance metadata, CancellationToken cancellationToken);

		/// <summary>
		/// Move an <see cref="IInstance"/>
		/// </summary>
		/// <param name="metadata">The <see cref="Host.Models.Instance"/> of the desired <see cref="IInstance"/></param>
		/// <param name="newPath">The new path of the <see cref="IInstance"/>. <paramref name="metadata"/> will have this set on <see cref="Api.Models.Instance.Path"/> if the operation completes successfully</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task MoveInstance(Host.Models.Instance metadata, string newPath, CancellationToken cancellationToken);

		/// <summary>
		/// Handle a GET via world/Export
		/// </summary>
		/// <param name="query">The request query</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="object"/> graph that can be jsonified</returns>
		Task<object> HandleWorldExport(IQueryCollection query, CancellationToken cancellationToken);
	}
}
