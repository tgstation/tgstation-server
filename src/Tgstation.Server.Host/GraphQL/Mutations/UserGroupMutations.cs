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
		public ValueTask<UserGroup> CreateUserGroup(
			string name,
			PermissionSetInput permissionSet,
			[Service] IUserGroupAuthority userGroupAuthority,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public ValueTask<UserGroup> UpdateUserGroup(
			[ID(nameof(UserGroup))] long id,
			string? newName,
			PermissionSetInput newPermissionSet,
			[Service] IUserGroupAuthority userGroupAuthority,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public ValueTask DeleteEmptyUserGroup(
			[ID(nameof(UserGroup))] long id,
			[Service] IUserGroupAuthority userGroupAuthority,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
