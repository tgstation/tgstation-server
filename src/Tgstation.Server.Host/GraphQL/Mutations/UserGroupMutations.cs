using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.GraphQL.Mutations.Payloads;
using Tgstation.Server.Host.GraphQL.Types;
using Tgstation.Server.Host.Models.Transformers;

namespace Tgstation.Server.Host.GraphQL.Mutations
{
	/// <summary>
	/// <see cref="IUserGroupAuthority"/> related <see cref="Mutation"/>s.
	/// </summary>
	[ExtendObjectType(typeof(Mutation))]
	[GraphQLDescription(Mutation.GraphQLDescription)]
	public sealed class UserGroupMutations
	{
		/// <summary>
		/// Transform a <see cref="Api.Models.PermissionSet"/> into a <see cref="PermissionSet"/>.
		/// </summary>
		/// <param name="permissionSet">The <see cref="Api.Models.PermissionSet"/> to transform.</param>
		/// <returns>The transformed <paramref name="permissionSet"/>.</returns>
		static Models.PermissionSet? TransformApiPermissionSet(PermissionSetInput? permissionSet)
			=> permissionSet != null
				? new Models.PermissionSet
				{
					InstanceManagerRights = permissionSet?.InstanceManagerRights,
					AdministrationRights = permissionSet?.AdministrationRights,
				}
				: null;

		/// <summary>
		/// Creates a <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="name">The <see cref="NamedEntity.Name"/> of the <see cref="UserGroup"/>.</param>
		/// <param name="permissionSet">The initial permission set for the <see cref="UserGroup"/>.</param>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The created <see cref="UserGroup"/>.</returns>
		[Error(typeof(ErrorMessageException))]
		public ValueTask<UserGroup> CreateUserGroup(
			string name,
			PermissionSetInput? permissionSet,
			[Service] IGraphQLAuthorityInvoker<IUserGroupAuthority> userGroupAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(name);
			ArgumentNullException.ThrowIfNull(userGroupAuthority);

			return userGroupAuthority.InvokeTransformable<Models.UserGroup, UserGroup, UserGroupGraphQLTransformer>(
				authority => authority.Create(name, TransformApiPermissionSet(permissionSet), cancellationToken));
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
		[Error(typeof(ErrorMessageException))]
		public ValueTask<UserGroup> UpdateUserGroup(
			[ID(nameof(UserGroup))] long id,
			string? newName,
			PermissionSetInput? newPermissionSet,
			[Service] IGraphQLAuthorityInvoker<IUserGroupAuthority> userGroupAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userGroupAuthority);
			return userGroupAuthority.InvokeTransformable<Models.UserGroup, UserGroup, UserGroupGraphQLTransformer>(
				authority => authority.Update(id, newName, TransformApiPermissionSet(newPermissionSet), cancellationToken));
		}

		/// <summary>
		/// Deletes a <see cref="UserGroup"/>.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> of the <see cref="UserGroup"/> to update.</param>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="Query"/> root.</returns>
		[Error(typeof(ErrorMessageException))]
		public async ValueTask<Query> DeleteEmptyUserGroup(
			[ID(nameof(UserGroup))] long id,
			[Service] IGraphQLAuthorityInvoker<IUserGroupAuthority> userGroupAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userGroupAuthority);
			await userGroupAuthority.Invoke(
				authority => authority.DeleteEmpty(id, cancellationToken));

			return new Query();
		}
	}
}
