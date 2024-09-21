using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// <see cref="IAuthority"/> for managing <see cref="UserGroup"/>s.
	/// </summary>
	public interface IUserGroupAuthority : IAuthority
	{
		/// <summary>
		/// Gets the current <see cref="UserGroup"/>.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="UserGroup"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<AuthorityResponse<UserGroup>> Read();

		/// <summary>
		/// Gets the <see cref="UserGroup"/> with a given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.EntityId.Id"/> of the <see cref="UserGroup"/>.</param>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="User"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		ValueTask<AuthorityResponse<UserGroup>> GetId(long id, bool includeJoins, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all registered <see cref="UserGroup"/>s.
		/// </summary>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of <see cref="UserGroup"/>s.</returns>
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		IQueryable<UserGroup> Queryable(bool includeJoins);

		/// <summary>
		/// Create a <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="name">The created <see cref="UserGroup"/>'s <see cref="Api.Models.NamedEntity.Name"/>.</param>
		/// <param name="permissionSet">The created <see cref="UserGroup"/>'s <see cref="UserGroup.PermissionSet"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="UserGroup"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		ValueTask<AuthorityResponse<UserGroup>> Create(string name, PermissionSet? permissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Updates a <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.EntityId.Id"/> of the <see cref="UserGroup"/> to update.</param>
		/// <param name="newName">The optional new <see cref="Api.Models.NamedEntity.Name"/> for the <see cref="UserGroup"/>.</param>
		/// <param name="newPermissionSet">The optional new <see cref="UserGroup.PermissionSet"/> for the <see cref="UserGroup"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="UserGroup"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		ValueTask<AuthorityResponse<UserGroup>> Update(long id, string? newName, PermissionSet? newPermissionSet, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes an empty <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.EntityId.Id"/> of the <see cref="UserGroup"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		ValueTask<AuthorityResponse> DeleteEmpty(long id, CancellationToken cancellationToken);
	}
}
