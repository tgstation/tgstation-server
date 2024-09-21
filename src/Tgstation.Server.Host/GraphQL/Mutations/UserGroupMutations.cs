using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.GraphQL.Mutations.Payloads;
using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL.Mutations
{
	/// <summary>
	/// <see cref="IUserGroupAuthority"/> related <see cref="Mutation"/>s.
	/// </summary>
	[ExtendObjectType(typeof(Mutation))]
	public sealed class UserGroupMutations
	{
		/// <summary>
		/// Creates a <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="name">The <see cref="NamedEntity.Name"/> of the <see cref="UserGroup"/>.</param>
		/// <param name="permissionSet">The initial permission set for the <see cref="UserGroup"/>.</param>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The created <see cref="UserGroup"/>.</returns>
		public ValueTask<UserGroup> CreateUserGroup(
			string name,
			PermissionSetInput? permissionSet,
			[Service] IUserGroupAuthority userGroupAuthority,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Updates a <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> of the <see cref="UserGroup"/> to update.</param>
		/// <param name="newName">Optional new <see cref="NamedEntity.Name"/> for the <see cref="UserGroup"/>.</param>
		/// <param name="newPermissionSet">Optional new permission set for the <see cref="UserGroup"/>.</param>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The updated <see cref="UserGroup"/>.</returns>
		public ValueTask<UserGroup> UpdateUserGroup(
			[ID(nameof(UserGroup))] long id,
			string? newName,
			PermissionSetInput? newPermissionSet,
			[Service] IUserGroupAuthority userGroupAuthority,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Deletes a <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> of the <see cref="UserGroup"/> to update.</param>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="Query"/> root.</returns>
		public ValueTask<Query> DeleteEmptyUserGroup(
			[ID(nameof(UserGroup))] long id,
			[Service] IUserGroupAuthority userGroupAuthority,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
