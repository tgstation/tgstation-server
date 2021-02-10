using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For managing <see cref="Instance"/>s
	/// </summary>
	public interface IInstanceManagerClient
	{
		/// <summary>
		/// Get all <see cref="IInstanceClient"/>s for <see cref="Instance"/>s the user can view
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of all <see cref="Instance"/>s the user can view</returns>
		Task<IReadOnlyList<InstanceResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Create or attach an <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="InstanceCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the created or attached <see cref="Instance"/></returns>
		Task<InstanceResponse> CreateOrAttach(InstanceCreateRequest instance, CancellationToken cancellationToken);

		/// <summary>
		/// Relocates, renamed, and/or on/offlines an <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="InstanceUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated <see cref="Instance"/></returns>
		Task<InstanceResponse> Update(InstanceUpdateRequest instance, CancellationToken cancellationToken);

		/// <summary>
		/// Get a specific <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Instance"/></returns>
		Task<InstanceResponse> GetId(EntityId instance, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes an <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="EntityId"/> of the <see cref="Instance"/> to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Detach(EntityId instance, CancellationToken cancellationToken);

		/// <summary>
		/// Gives the user full permissions on an <paramref name="instance"/>.
		/// </summary>
		/// <param name="instance">The <see cref="EntityId"/> of the <see cref="Instance"/> to grant permissions on.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task GrantPermissions(EntityId instance, CancellationToken cancellationToken);

		/// <summary>
		/// Create an <see cref="IInstanceClient"/> for a given <see cref="Instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="EntityId"/> of the <see cref="Instance"/> to create an <see cref="IInstanceClient"/> for</param>
		/// <returns>A new <see cref="IInstanceClient"/></returns>
		IInstanceClient CreateClient(EntityId instance);
	}
}
