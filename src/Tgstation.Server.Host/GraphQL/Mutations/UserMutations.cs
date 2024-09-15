﻿using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL.Mutations
{
	/// <summary>
	/// <see cref="IUserAuthority"/> related <see cref="Mutation"/>s.
	/// </summary>
	[ExtendObjectType(typeof(Mutation))]
	public sealed class UserMutations
	{
		/// <summary>
		/// Creates a TGS user specifying a personal <see cref="PermissionSet"/>.
		/// </summary>
		/// <param name="name">The <see cref="NamedEntity.Name"/> of the <see cref="User"/>.</param>
		/// <param name="password">The password of the <see cref="User"/>.</param>
		/// <param name="enabled">If the <see cref="User"/> is <see cref="User.Enabled"/>.</param>
		/// <param name="permissionSet">The owned <see cref="PermissionSet"/> of the user.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the created <see cref="User"/>.</returns>
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> CreateUserByPasswordAndPermissionSet(
			string name,
			string password,
			bool enabled,
			PermissionSet permissionSet,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			ArgumentException.ThrowIfNullOrEmpty(password);
			ArgumentNullException.ThrowIfNull(permissionSet);
			ArgumentNullException.ThrowIfNull(userAuthority);

			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a system user specifying a personal <see cref="PermissionSet"/>.
		/// </summary>
		/// <param name="systemIdentifier">The <see cref="User.SystemIdentifier"/> of the <see cref="User"/>.</param>
		/// <param name="enabled">If the <see cref="User"/> is <see cref="User.Enabled"/>.</param>
		/// <param name="permissionSet">The owned <see cref="PermissionSet"/> of the user.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the created <see cref="User"/>.</returns>
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> CreateUserBySystemIDAndPermissionSet(
			string systemIdentifier,
			bool enabled,
			PermissionSet permissionSet,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(systemIdentifier);
			ArgumentNullException.ThrowIfNull(permissionSet);
			ArgumentNullException.ThrowIfNull(userAuthority);

			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a TGS user specifying the <see cref="UserGroup"/> they will belong to.
		/// </summary>
		/// <param name="name">The <see cref="NamedEntity.Name"/> of the <see cref="User"/>.</param>
		/// <param name="password">The password of the <see cref="User"/>.</param>
		/// <param name="enabled">If the <see cref="User"/> is <see cref="User.Enabled"/>.</param>
		/// <param name="groupId">The <see cref="Entity.Id"/> of the <see cref="UserGroup"/> the <see cref="User"/> will belong to.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the created <see cref="User"/>.</returns>
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> CreateUserByPasswordAndGroup(
			string name,
			string password,
			bool enabled,
			[ID(nameof(UserGroup))] long groupId,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			ArgumentException.ThrowIfNullOrEmpty(password);
			ArgumentNullException.ThrowIfNull(userAuthority);

			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a system user specifying the <see cref="UserGroup"/> they will belong to.
		/// </summary>
		/// <param name="systemIdentifier">The <see cref="User.SystemIdentifier"/> of the <see cref="User"/>.</param>
		/// <param name="enabled">If the <see cref="User"/> is <see cref="User.Enabled"/>.</param>
		/// <param name="groupId">The <see cref="Entity.Id"/> of the <see cref="UserGroup"/> the <see cref="User"/> will belong to.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the created <see cref="User"/>.</returns>
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> CreateUserBySystemIDAndGroup(
			string systemIdentifier,
			bool enabled,
			[ID(nameof(UserGroup))] long groupId,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(systemIdentifier);
			ArgumentNullException.ThrowIfNull(userAuthority);

			throw new NotImplementedException();
		}
	}
}