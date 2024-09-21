using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.GraphQL.Mutations.Payloads;
using Tgstation.Server.Host.GraphQL.Types;
using Tgstation.Server.Host.Models.Transformers;
using Tgstation.Server.Host.Security;

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
		/// <param name="oAuthConnections">The <see cref="OAuthConnection"/>s for the user.</param>
		/// <param name="permissionSet">The owned <see cref="PermissionSet"/> of the user.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The created <see cref="User"/>.</returns>
		[TgsGraphQLAuthorize<IUserAuthority>(nameof(IUserAuthority.Create))]
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> CreateUserByPasswordAndPermissionSet(
			string name,
			string password,
			bool? enabled,
			IEnumerable<OAuthConnection>? oAuthConnections,
			PermissionSetInput? permissionSet,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			ArgumentException.ThrowIfNullOrEmpty(password);
			ArgumentNullException.ThrowIfNull(userAuthority);

			return userAuthority.InvokeTransformable<Models.User, User, UserGraphQLTransformer>(
				authority => authority.Create(
					new UserCreateRequest
					{
						Name = name,
						Password = password,
						Enabled = enabled,
						PermissionSet = permissionSet != null
							? new Api.Models.PermissionSet
							{
								AdministrationRights = permissionSet.AdministrationRights,
								InstanceManagerRights = permissionSet.InstanceManagerRights,
							}
							: null,
						OAuthConnections = oAuthConnections
							?.Select(oAuthConnection => new Api.Models.OAuthConnection
							{
								ExternalUserId = oAuthConnection.ExternalUserId,
								Provider = oAuthConnection.Provider,
							})
							.ToList(),
					},
					cancellationToken));
		}

		/// <summary>
		/// Creates a system user specifying a personal <see cref="PermissionSet"/>.
		/// </summary>
		/// <param name="systemIdentifier">The <see cref="User.SystemIdentifier"/> of the <see cref="User"/>.</param>
		/// <param name="enabled">If the <see cref="User"/> is <see cref="User.Enabled"/>.</param>
		/// <param name="oAuthConnections">The <see cref="OAuthConnection"/>s for the user.</param>
		/// <param name="permissionSet">The owned <see cref="PermissionSet"/> of the user.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The created <see cref="User"/>.</returns>
		[TgsGraphQLAuthorize<IUserAuthority>(nameof(IUserAuthority.Create))]
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> CreateUserBySystemIDAndPermissionSet(
			string systemIdentifier,
			bool? enabled,
			IEnumerable<OAuthConnection>? oAuthConnections,
			PermissionSetInput permissionSet,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(systemIdentifier);
			ArgumentNullException.ThrowIfNull(userAuthority);

			return userAuthority.InvokeTransformable<Models.User, User, UserGraphQLTransformer>(
				authority => authority.Create(
					new UserCreateRequest
					{
						SystemIdentifier = systemIdentifier,
						Enabled = enabled,
						PermissionSet = permissionSet != null
							? new Api.Models.PermissionSet
							{
								AdministrationRights = permissionSet.AdministrationRights,
								InstanceManagerRights = permissionSet.InstanceManagerRights,
							}
							: null,
						OAuthConnections = oAuthConnections
							?.Select(oAuthConnection => new Api.Models.OAuthConnection
							{
								ExternalUserId = oAuthConnection.ExternalUserId,
								Provider = oAuthConnection.Provider,
							})
							.ToList(),
					},
					cancellationToken));
		}

		/// <summary>
		/// Creates a TGS user specifying the <see cref="UserGroup"/> they will belong to.
		/// </summary>
		/// <param name="name">The <see cref="NamedEntity.Name"/> of the <see cref="User"/>.</param>
		/// <param name="password">The password of the <see cref="User"/>.</param>
		/// <param name="enabled">If the <see cref="User"/> is <see cref="User.Enabled"/>.</param>
		/// <param name="oAuthConnections">The <see cref="OAuthConnection"/>s for the user.</param>
		/// <param name="groupId">The <see cref="Entity.Id"/> of the <see cref="UserGroup"/> the <see cref="User"/> will belong to.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The created <see cref="User"/>.</returns>
		[TgsGraphQLAuthorize<IUserAuthority>(nameof(IUserAuthority.Create))]
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> CreateUserByPasswordAndGroup(
			string name,
			string password,
			bool? enabled,
			IEnumerable<OAuthConnection>? oAuthConnections,
			[ID(nameof(UserGroup))] long groupId,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			ArgumentException.ThrowIfNullOrEmpty(password);
			ArgumentNullException.ThrowIfNull(userAuthority);

			return userAuthority.InvokeTransformable<Models.User, User, UserGraphQLTransformer>(
				authority => authority.Create(
					new UserCreateRequest
					{
						Name = name,
						Password = password,
						Enabled = enabled,
						Group = new Api.Models.Internal.UserGroup
						{
							Id = groupId,
						},
						OAuthConnections = oAuthConnections
							?.Select(oAuthConnection => new Api.Models.OAuthConnection
							{
								ExternalUserId = oAuthConnection.ExternalUserId,
								Provider = oAuthConnection.Provider,
							})
							.ToList(),
					},
					cancellationToken));
		}

		/// <summary>
		/// Creates a system user specifying the <see cref="UserGroup"/> they will belong to.
		/// </summary>
		/// <param name="systemIdentifier">The <see cref="User.SystemIdentifier"/> of the <see cref="User"/>.</param>
		/// <param name="enabled">If the <see cref="User"/> is <see cref="User.Enabled"/>.</param>
		/// <param name="oAuthConnections">The <see cref="OAuthConnection"/>s for the user.</param>
		/// <param name="groupId">The <see cref="Entity.Id"/> of the <see cref="UserGroup"/> the <see cref="User"/> will belong to.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The created <see cref="User"/>.</returns>
		[TgsGraphQLAuthorize<IUserAuthority>(nameof(IUserAuthority.Create))]
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> CreateUserBySystemIDAndGroup(
			string systemIdentifier,
			bool? enabled,
			IEnumerable<OAuthConnection>? oAuthConnections,
			[ID(nameof(UserGroup))] long groupId,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(systemIdentifier);
			ArgumentNullException.ThrowIfNull(userAuthority);

			return userAuthority.InvokeTransformable<Models.User, User, UserGraphQLTransformer>(
				authority => authority.Create(
					new UserCreateRequest
					{
						SystemIdentifier = systemIdentifier,
						Enabled = enabled,
						Group = new Api.Models.Internal.UserGroup
						{
							Id = groupId,
						},
						OAuthConnections = oAuthConnections
							?.Select(oAuthConnection => new Api.Models.OAuthConnection
							{
								ExternalUserId = oAuthConnection.ExternalUserId,
								Provider = oAuthConnection.Provider,
							})
							.ToList(),
					},
					cancellationToken));
		}

		/// <summary>
		/// Sets the current user's password.
		/// </summary>
		/// <param name="newPassword">The new password for the current user.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> to get the <see cref="Entity.Id"/> of the user.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The updated current <see cref="User"/>.</returns>
		[TgsGraphQLAuthorize(AdministrationRights.WriteUsers | AdministrationRights.EditOwnPassword)]
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> SetCurrentUserPassword(
			string newPassword,
			[Service] IAuthenticationContext authenticationContext,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrEmpty(newPassword);
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.InvokeTransformable<Models.User, User, UserGraphQLTransformer>(
				async authority => await authority.Update(
					new UserUpdateRequest
					{
						Id = authenticationContext.User.Id,
						Password = newPassword,
					},
					cancellationToken));
		}

		/// <summary>
		/// Sets the current user's <see cref="OAuthConnection"/>s.
		/// </summary>
		/// <param name="newOAuthConnections">The new <see cref="OAuthConnection"/>s for the current user.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> to get the <see cref="Entity.Id"/> of the user.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The updated current <see cref="User"/>.</returns>
		[TgsGraphQLAuthorize(AdministrationRights.WriteUsers | AdministrationRights.EditOwnOAuthConnections)]
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> SetCurrentOAuthConnections(
			IEnumerable<OAuthConnection> newOAuthConnections,
			[Service] IAuthenticationContext authenticationContext,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(newOAuthConnections);
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.InvokeTransformable<Models.User, User, UserGraphQLTransformer>(
				async authority => await authority.Update(
					new UserUpdateRequest
					{
						Id = authenticationContext.User.Id,
						OAuthConnections = newOAuthConnections
							.Select(oAuthConnection => new Api.Models.OAuthConnection
							{
								ExternalUserId = oAuthConnection.ExternalUserId,
								Provider = oAuthConnection.Provider,
							})
							.ToList(),
					},
					cancellationToken));
		}

		/// <summary>
		/// Updates a user.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> of the <see cref="User"/> to update.</param>
		/// <param name="casingOnlyNameChange">Optional casing only change to the <see cref="NamedEntity.Name"/> of the <see cref="User"/>. Only applicable to TGS users.</param>
		/// <param name="newPassword">Optional new password for the <see cref="User"/>. Only applicable to TGS users.</param>
		/// <param name="enabled">Optional new <see cref="User.Enabled"/> status for the <see cref="User"/>.</param>
		/// <param name="newPermissionSet">Optional new owned <see cref="PermissionSet"/> for the user. Note that setting this on a <see cref="User"/> in a <see cref="UserGroup"/> will remove them from that group.</param>
		/// <param name="newGroupId">Optional <see cref="Entity.Id"/> of the <see cref="UserGroup"/> to move the <see cref="User"/> to.</param>
		/// <param name="newOAuthConnections">Optional new <see cref="OAuthConnection"/>s for the <see cref="User"/>.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The updated <see cref="User"/>.</returns>
		[TgsGraphQLAuthorize(AdministrationRights.WriteUsers)]
		[Error(typeof(ErrorMessageException))]
		public ValueTask<User> UpdateUser(
			[ID(nameof(User))] long id,
			string? casingOnlyNameChange,
			string? newPassword,
			bool? enabled,
			PermissionSetInput? newPermissionSet,
			[ID(nameof(UserGroup))] long? newGroupId,
			IEnumerable<OAuthConnection>? newOAuthConnections,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(newOAuthConnections);
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.InvokeTransformable<Models.User, User, UserGraphQLTransformer>(
				async authority => await authority.Update(
					new UserUpdateRequest
					{
						Id = id,
						Name = casingOnlyNameChange,
						Password = newPassword,
						Enabled = enabled,
						PermissionSet = newPermissionSet != null
							? new Api.Models.PermissionSet
							{
								InstanceManagerRights = newPermissionSet.InstanceManagerRights,
								AdministrationRights = newPermissionSet.AdministrationRights,
							}
							: null,
						Group = newGroupId.HasValue
							? new Api.Models.Internal.UserGroup
							{
								Id = newGroupId.Value,
							}
							: null,
						OAuthConnections = newOAuthConnections
							.Select(oAuthConnection => new Api.Models.OAuthConnection
							{
								ExternalUserId = oAuthConnection.ExternalUserId,
								Provider = oAuthConnection.Provider,
							})
							.ToList(),
					},
					cancellationToken));
		}
	}
}
