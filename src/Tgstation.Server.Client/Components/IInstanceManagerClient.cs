using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// Manager for <see cref="Instance"/>s
	/// </summary>
    public interface IInstanceManagerClient : IClient<InstanceRights>
	{
		/// <summary>
		/// Get all <see cref="IInstanceClient"/>s for <see cref="Instance"/>s that the <see cref="IClient{TRights}"/> can view
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="IInstanceClient"/>s</returns>
		Task<IReadOnlyList<IInstanceClient>> GetInstanceClients(CancellationToken cancellationToken);

		/// <summary>
		/// Create an <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> to create. <see cref="Instance.Id"/> will be ignored</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see cref="IInstanceClient"/> for the created <see cref="Instance"/></returns>
		Task<IInstanceClient> CreateInstance(Instance instance, CancellationToken cancellationToken);

		/// <summary>
		/// Relocates, renamed, and/or on/offlines an <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task UpdateInstance(Instance instance, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes an <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DeleteInstance(Instance instance, CancellationToken cancellationToken);
	}
}
