using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For managing <see cref="UserGroupResponse"/>s.
	/// </summary>
	public interface IUserGroupsClient
	{
		/// <summary>
		/// Get a specific <paramref name="group"/>.
		/// </summary>
		/// <param name="group">The <see cref="EntityId"/> of the user group to get.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the requested <paramref name="group"/>.</returns>
		ValueTask<UserGroupResponse> GetId(EntityId group, CancellationToken cancellationToken);

		/// <summary>
		/// List all <see cref="UserGroupResponse"/>s.
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="List{T}"/> of all <see cref="UserGroupResponse"/>s.</returns>
		ValueTask<List<UserGroupResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Create a new <paramref name="group"/>.
		/// </summary>
		/// <param name="group">The <see cref="UserGroupCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the new <see cref="UserGroupResponse"/>.</returns>
		ValueTask<UserGroupResponse> Create(UserGroupCreateRequest group, CancellationToken cancellationToken);

		/// <summary>
		/// Update a <paramref name="group"/>.
		/// </summary>
		/// <param name="group">The <see cref="UserGroupUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the updated <see cref="UserGroupResponse"/>.</returns>
		ValueTask<UserGroupResponse> Update(UserGroupUpdateRequest group, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes a <paramref name="group"/>.
		/// </summary>
		/// <param name="group">The <see cref="EntityId"/> of the user group to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Delete(EntityId group, CancellationToken cancellationToken);
	}
}
