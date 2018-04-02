using System.Collections.Generic;
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
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="IInstanceClient"/>s</returns>
		Task<IReadOnlyList<IInstanceClient>> GetInstanceClients();

		/// <summary>
		/// Create an <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> to create. <see cref="Instance.Id"/> will be ignored</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see cref="IInstanceClient"/> for the created <see cref="Instance"/></returns>
		Task<IInstanceClient> CreateInstance(Instance instance);

		/// <summary>
		/// Relocates, renamed, and/or on/offlines an <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> to update</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task UpdateInstance(Instance instance);

		/// <summary>
		/// Deletes an <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> to delete</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DeleteInstance(Instance instance);
	}
}
