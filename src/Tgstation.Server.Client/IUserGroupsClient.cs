using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For managing <see cref="UserGroup"/>s.
	/// </summary>
	public interface IUserGroupsClient
	{
		/// <summary>
		/// Get a specific <paramref name="group"/>.
		/// </summary>
		/// <param name="group">The <see cref="EntityId"/> of the <see cref="UserGroup"/> to get.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the requested <paramref name="group"/>.</returns>
		Task<UserGroup> GetId(EntityId group, CancellationToken cancellationToken);

		/// <summary>
		/// List all <see cref="UserGroup"/>s.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of all <see cref="UserGroup"/>s.</returns>
		Task<IReadOnlyList<UserGroup>> List(CancellationToken cancellationToken);

		/// <summary>
		/// Create a new <paramref name="group"/>.
		/// </summary>
		/// <param name="group">The <see cref="UserGroup"/> to create.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The new <see cref="User"/>.</returns>
		Task<UserGroup> Create(UserGroup group, CancellationToken cancellationToken);

		/// <summary>
		/// Update a <paramref name="group"/>.
		/// </summary>
		/// <param name="group">The updated <see cref="UserGroup"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The updated <see cref="User"/>.</returns>
		Task<UserGroup> Update(UserGroup group, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes a <paramref name="group"/>.
		/// </summary>
		/// <param name="group">The <see cref="EntityId"/> of the <see cref="UserGroup"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task Delete(EntityId group, CancellationToken cancellationToken);
	}
}
