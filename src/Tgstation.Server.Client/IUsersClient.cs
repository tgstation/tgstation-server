using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For managing <see cref="UserResponse"/>s
	/// </summary>
	public interface IUsersClient
	{
		/// <summary>
		/// Read the current user's information and general rights
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the current <see cref="UserResponse"/></returns>
		Task<UserResponse> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Get a specific <paramref name="user"/>
		/// </summary>
		/// <param name="user">The <see cref="EntityId"/> of the <see cref="UserResponse"/> to get.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the requested <paramref name="user"/></returns>
		Task<UserResponse> GetId(EntityId user, CancellationToken cancellationToken);

		/// <summary>
		/// List all <see cref="UserResponse"/>s
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of all <see cref="UserResponse"/>s</returns>
		Task<IReadOnlyList<UserResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Create a new <paramref name="user"/>
		/// </summary>
		/// <param name="user">The <see cref="UserCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>The new <see cref="UserResponse"/></returns>
		Task<UserResponse> Create(UserCreateRequest user, CancellationToken cancellationToken);

		/// <summary>
		/// Update a <paramref name="user"/>
		/// </summary>
		/// <param name="user">The <see cref="UserUpdateRequest"/> used to update the <see cref="UserResponse"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>The updated <see cref="UserResponse"/></returns>
		Task<UserResponse> Update(UserUpdateRequest user, CancellationToken cancellationToken);
	}
}
