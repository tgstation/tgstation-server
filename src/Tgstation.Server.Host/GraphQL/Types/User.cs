using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.GraphQL.Interfaces;
using Tgstation.Server.Host.GraphQL.Types.OAuth;
using Tgstation.Server.Host.Models.Transformers;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// A user registered in the server.
	/// </summary>
	[Node]
	public sealed class User : NamedEntity, IUserName
	{
		/// <summary>
		/// If the <see cref="User"/> is enabled since users cannot be deleted. System users cannot be disabled.
		/// </summary>
		public required bool Enabled { get; init; }

		/// <summary>
		/// The user's canonical (Uppercase) name.
		/// </summary>
		public required string CanonicalName { get; init; }

		/// <summary>
		/// When the <see cref="User"/> was created.
		/// </summary>
		public required DateTimeOffset CreatedAt { get; init; }

		/// <summary>
		/// The SID/UID of the <see cref="User"/> on Windows/POSIX respectively.
		/// </summary>
		public required string? SystemIdentifier { get; init; }

		/// <summary>
		/// The <see cref="Entity.Id"/> of the <see cref="CreatedBy"/> <see cref="User"/>.
		/// </summary>
		[GraphQLIgnore]
		public required long? CreatedById { get; init; }

		/// <summary>
		/// The <see cref="Entity.Id"/> of the <see cref="Group"/>.
		/// </summary>
		[GraphQLIgnore]
		public required long? GroupId { get; init; }

		/// <summary>
		/// Node resolver for <see cref="User"/>s.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> to lookup.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the queried <see cref="User"/>, if present.</returns>
		[TgsGraphQLAuthorize]
		public static ValueTask<User?> GetUser(
			long id,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.InvokeTransformableAllowMissing<Models.User, User, UserGraphQLTransformer>(
				authority => authority.GetId(id, false, false, cancellationToken));
		}

		/// <summary>
		/// The <see cref="User"/> who created this <see cref="User"/>.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="IUserName"/> that created this <see cref="User"/>, if any.</returns>
		public async ValueTask<IUserName?> CreatedBy(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			if (!CreatedById.HasValue)
				return null;

			var user = await userAuthority.InvokeTransformable<Models.User, User, UserGraphQLTransformer>(authority => authority.GetId(CreatedById.Value, false, true, cancellationToken));
			if (user.CanonicalName == Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
				return new UserName(user);

			return user;
		}

		/// <summary>
		/// List of <see cref="OAuthConnection"/>s associated with the user if OAuth is configured.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="Array"/> of <see cref="OAuthConnection"/>s for the <see cref="User"/> if OAuth is configured.</returns>
		public ValueTask<OAuthConnection[]> OAuthConnections(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.Invoke<OAuthConnection[], OAuthConnection[]>(
				authority => authority.OAuthConnections(Id, cancellationToken));
		}

		/// <summary>
		/// The <see cref="PermissionSet"/> associated with the <see cref="User"/>.
		/// </summary>
		/// <param name="permissionSetAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IPermissionSetAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="PermissionSet"/> associated with the <see cref="User"/>.</returns>
		public ValueTask<PermissionSet> EffectivePermissionSet(
			[Service] IGraphQLAuthorityInvoker<IPermissionSetAuthority> permissionSetAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(permissionSetAuthority);

			long lookupId;
			PermissionSetLookupType lookupType;
			if (GroupId.HasValue)
			{
				lookupId = GroupId.Value;
				lookupType = PermissionSetLookupType.GroupId;
			}
			else
			{
				lookupId = Id;
				lookupType = PermissionSetLookupType.UserId;
			}

			return permissionSetAuthority.InvokeTransformable<Models.PermissionSet, PermissionSet, PermissionSetGraphQLTransformer>(
				authority => authority.GetId(lookupId, lookupType, cancellationToken));
		}

		/// <summary>
		/// The <see cref="PermissionSet"/> owned by the <see cref="User"/>, if any.
		/// </summary>
		/// <param name="permissionSetAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IPermissionSetAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="PermissionSet"/> owned by the <see cref="User"/>, if any.</returns>
		public ValueTask<PermissionSet?> OwnedPermissionSet(
			[Service] IGraphQLAuthorityInvoker<IPermissionSetAuthority> permissionSetAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(permissionSetAuthority);

			return permissionSetAuthority.InvokeTransformableAllowMissing<Models.PermissionSet, PermissionSet, PermissionSetGraphQLTransformer>(
				authority => authority.GetId(Id, PermissionSetLookupType.UserId, cancellationToken));
		}

		/// <summary>
		/// The <see cref="UserGroup"/> asociated with the user, if any.
		/// </summary>
		/// <param name="userGroupAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserGroupAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="UserGroup"/> associated with the <see cref="User"/>, if any.</returns>
		public async ValueTask<UserGroup?> Group(
			[Service] IGraphQLAuthorityInvoker<IUserGroupAuthority> userGroupAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userGroupAuthority);
			if (!GroupId.HasValue)
				return null;

			return await userGroupAuthority.InvokeTransformable<Models.UserGroup, UserGroup, UserGroupGraphQLTransformer>(
				authority => authority.GetId(GroupId.Value, false, cancellationToken));
		}
	}
}
