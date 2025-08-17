using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut;
using GreenDonut.Data;

using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.GraphQL.Interfaces;
using Tgstation.Server.Host.GraphQL.Transformers;
using Tgstation.Server.Host.GraphQL.Types.OAuth;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// A user registered in the server.
	/// </summary>
	[Node]
	public sealed class User : UserName
	{
		/// <inheritdoc />
		[IsProjected(true)]
		public override required long Id
		{
			get => base.Id;
			set => base.Id = value;
		}

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
		/// The <see cref="UserGroup"/> for the user.
		/// </summary>
		public required UserGroup? Group { get; set; }

		/// <summary>
		/// The <see cref="PermissionSet"/> associated with the <see cref="User"/>.
		/// </summary>
		public required PermissionSet EffectivePermissionSet { get; set; }

		/// <summary>
		/// The <see cref="PermissionSet"/> for the user if the user does not belong to a <see cref="Group"/>.
		/// </summary>
		public required PermissionSet? OwnedPermissionSet { get; set; }

		/// <summary>
		/// The <see cref="Entity.Id"/> of the <see cref="CreatedBy"/> <see cref="User"/>.
		/// </summary>
		[IsProjected(true)]
		public required long? CreatedById { get; init; }

		/// <summary>
		/// Implements the <see cref="IUserGroupsDataLoader"/>.
		/// </summary>
		/// <param name="ids">The <see cref="IReadOnlyList{T}"/> of <see cref="User"/> <see cref="Api.Models.EntityId.Id"/>s to load paired with if the system user should be allowed.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="queryContext">The <see cref="QueryContext{TEntity}"/> for <see cref="User"/> mapped to an <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of the requested <see cref="User"/> <see cref="AuthorityResponse{TResult}"/>s.</returns>
		[DataLoader(AccessModifier = DataLoaderAccessModifier.PublicInterface)]
		public static ValueTask<Dictionary<long, AuthorityResponse<User>>> GetUsers(
			IReadOnlyList<long> ids,
			IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			QueryContext<AuthorityResponse<User>>? queryContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(ids);
			ArgumentNullException.ThrowIfNull(userAuthority);

			return userAuthority.ExecuteDataLoader<Models.User, User, UserTransformer>(
				(authority, id) => authority.GetId<User>(id, false, cancellationToken),
				ids,
				queryContext);
		}

		/// <summary>
		/// Node resolver for <see cref="User"/>s.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> to lookup.</param>
		/// <param name="usersDataLoader">The <see cref="IUsersDataLoader"/> to use.</param>
		/// <param name="queryContext">The <see cref="QueryContext{TEntity}"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the queried <see cref="User"/>, if present.</returns>
		public static ValueTask<User?> GetUser(
			long id,
			[Service] IUsersDataLoader usersDataLoader,
			QueryContext<User>? queryContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(usersDataLoader);
			return usersDataLoader.LoadAuthorityResponse(queryContext, id, cancellationToken);
		}

		/// <summary>
		/// The <see cref="User"/> who created this <see cref="User"/>.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="queryContext">The <see cref="QueryContext{TEntity}"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="IUserName"/> that created this <see cref="User"/>, if any.</returns>
		public async ValueTask<IUserName?> CreatedBy(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			QueryContext<User>? queryContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			if (!CreatedById.HasValue)
				return null;

			// This one is particular and cannot be data-loaded due to necessitating a different parameter
			var user = await userAuthority.InvokeTransformable<Models.User, User, UserTransformer>(
				authority => authority.GetId<User>(CreatedById.Value, true, cancellationToken),
				queryContext);
			if (user == null)
				throw new InvalidOperationException($"Query for created by of user ID {CreatedById.Value} returned null!");

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
		[UsePaging]
		[UseProjection]
		[UseFiltering]
		[UseSorting]
		public ValueTask<IQueryable<OAuthConnection>> OAuthConnections(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.InvokeTransformableQueryable<Models.OAuthConnection, OAuthConnection, OAuthConnectionTransformer>(
				authority => authority.OAuthConnections(Id, cancellationToken));
		}

		/// <summary>
		/// List of <see cref="OidcConnection"/>s associated with the user if OIDC is configured.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="Array"/> of <see cref="OidcConnection"/>s for the <see cref="User"/> if OAuth is configured.</returns>
		[UsePaging]
		[UseProjection]
		[UseFiltering]
		[UseSorting]
		public ValueTask<IQueryable<OidcConnection>> OidcConnections(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.InvokeTransformableQueryable<Models.OidcConnection, OidcConnection, OidcConnectionTransformer>(
				authority => authority.OidcConnections(Id, cancellationToken));
		}
	}
}
