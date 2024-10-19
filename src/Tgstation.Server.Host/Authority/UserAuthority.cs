﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GreenDonut;

using HotChocolate.Subscriptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Models.Transformers;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority
{
	/// <inheritdoc cref="IUserAuthority" />
	sealed class UserAuthority : AuthorityBase, IUserAuthority
	{
		/// <summary>
		/// The <see cref="IUsersDataLoader"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly IUsersDataLoader usersDataLoader;

		/// <summary>
		/// The <see cref="IOAuthConnectionsDataLoader"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly IOAuthConnectionsDataLoader oAuthConnectionsDataLoader;

		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="IPermissionsUpdateNotifyee"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly IPermissionsUpdateNotifyee permissionsUpdateNotifyee;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="ISessionInvalidationTracker"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly ISessionInvalidationTracker sessionInvalidationTracker;

		/// <summary>
		/// The <see cref="ITopicEventSender"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly ITopicEventSender topicEventSender;

		/// <summary>
		/// The <see cref="IOptionsSnapshot{TOptions}"/> of <see cref="GeneralConfiguration"/> for the <see cref="UserAuthority"/>.
		/// </summary>
		readonly IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions;

		/// <summary>
		/// Implements the <see cref="usersDataLoader"/>.
		/// </summary>
		/// <param name="ids">The <see cref="IReadOnlyList{T}"/> of <see cref="User"/> <see cref="EntityId.Id"/>s to load.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to load from.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of the requested <see cref="User"/>s.</returns>
		[DataLoader]
		public static Task<Dictionary<long, User>> GetUsers(
			IReadOnlyList<long> ids,
			IDatabaseContext databaseContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(ids);
			ArgumentNullException.ThrowIfNull(databaseContext);

			return databaseContext
				.Users
				.AsQueryable()
				.Where(x => ids.Contains(x.Id!.Value))
				.ToDictionaryAsync(user => user.Id!.Value, cancellationToken);
		}

		/// <summary>
		/// Implements the <see cref="usersDataLoader"/>.
		/// </summary>
		/// <param name="userIds">The <see cref="IReadOnlyCollection{T}"/> of <see cref="User"/> <see cref="EntityId.Id"/>s to load the OAuthConnections for.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to load from.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of the requested <see cref="User"/>s.</returns>
		[DataLoader]
		public static async ValueTask<ILookup<long, GraphQL.Types.OAuth.OAuthConnection>> GetOAuthConnections(
			IReadOnlyList<long> userIds,
			IDatabaseContext databaseContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userIds);
			ArgumentNullException.ThrowIfNull(databaseContext);

			var list = await databaseContext
				.OAuthConnections
				.AsQueryable()
				.Where(x => userIds.Contains(x.User!.Id!.Value))
				.ToListAsync(cancellationToken);

			return list.ToLookup(
				oauthConnection => oauthConnection.UserId,
				x => new GraphQL.Types.OAuth.OAuthConnection(x.ExternalUserId!, x.Provider));
		}

		/// <summary>
		/// Check if a given <paramref name="model"/> has a valid <see cref="UserName.Name"/> specified.
		/// </summary>
		/// <param name="model">The <see cref="UserUpdateRequest"/> to check.</param>
		/// <param name="newUser">If this is a new <see cref="User"/>.</param>
		/// <returns><see langword="null"/> if <paramref name="model"/> is valid, an <see cref="AuthorityResponse{TResult}"/> errored otherwise.</returns>
		static AuthorityResponse<User>? CheckValidName(UserUpdateRequest model, bool newUser)
		{
			var userInvalidWithNullName = newUser && model.Name == null && model.SystemIdentifier == null;
			if (userInvalidWithNullName || (model.Name != null && String.IsNullOrWhiteSpace(model.Name)))
				return BadRequest<User>(ErrorCode.UserMissingName);

			model.Name = model.Name?.Trim();
			if (model.Name != null && model.Name.Contains(':', StringComparison.InvariantCulture))
				return BadRequest<User>(ErrorCode.UserColonInName);
			return null;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UserAuthority"/> class.
		/// </summary>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> to use.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="usersDataLoader">The value of <see cref="usersDataLoader"/>.</param>
		/// <param name="oAuthConnectionsDataLoader">The value of <see cref="oAuthConnectionsDataLoader"/>.</param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="permissionsUpdateNotifyee">The value of <see cref="permissionsUpdateNotifyee"/>.</param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="sessionInvalidationTracker">The value of <see cref="sessionInvalidationTracker"/>.</param>
		/// <param name="topicEventSender">The value of <see cref="topicEventSender"/>.</param>
		/// <param name="generalConfigurationOptions">The value of <see cref="generalConfigurationOptions"/>.</param>
		public UserAuthority(
			IAuthenticationContext authenticationContext,
			IDatabaseContext databaseContext,
			ILogger<UserAuthority> logger,
			IUsersDataLoader usersDataLoader,
			IOAuthConnectionsDataLoader oAuthConnectionsDataLoader,
			ISystemIdentityFactory systemIdentityFactory,
			IPermissionsUpdateNotifyee permissionsUpdateNotifyee,
			ICryptographySuite cryptographySuite,
			ISessionInvalidationTracker sessionInvalidationTracker,
			ITopicEventSender topicEventSender,
			IOptionsSnapshot<GeneralConfiguration> generalConfigurationOptions)
			: base(
				  authenticationContext,
				  databaseContext,
				  logger)
		{
			this.usersDataLoader = usersDataLoader ?? throw new ArgumentNullException(nameof(usersDataLoader));
			this.oAuthConnectionsDataLoader = oAuthConnectionsDataLoader ?? throw new ArgumentNullException(nameof(oAuthConnectionsDataLoader));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.permissionsUpdateNotifyee = permissionsUpdateNotifyee ?? throw new ArgumentNullException(nameof(permissionsUpdateNotifyee));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.sessionInvalidationTracker = sessionInvalidationTracker ?? throw new ArgumentNullException(nameof(sessionInvalidationTracker));
			this.topicEventSender = topicEventSender ?? throw new ArgumentNullException(nameof(topicEventSender));
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <summary>
		/// Checks if a <paramref name="createRequest"/> should return a bad request <see cref="AuthorityResponse{TResult}"/>.
		/// </summary>
		/// <param name="createRequest">The <see cref="UserCreateRequest"/> to check.</param>
		/// <param name="needZeroLengthPasswordWithOAuthConnections">If a zero-length <see cref="UserUpdateRequest.Password"/> indicates and OAuth only user.</param>
		/// <param name="failResponse">The output failing <see cref="AuthorityResponse{TResult}"/>, if any.</param>
		/// <returns><see langword="true"/> if checks failed and <paramref name="failResponse"/> was populated, <see langword="false"/> otherwise.</returns>
		static bool BadCreateRequestChecks(
			UserCreateRequest createRequest,
			bool? needZeroLengthPasswordWithOAuthConnections,
			[NotNullWhen(true)] out AuthorityResponse<User>? failResponse)
		{
			if (createRequest.OAuthConnections?.Any(x => x == null) == true)
			{
				failResponse = BadRequest<User>(ErrorCode.ModelValidationFailure);
				return true;
			}

			var hasNonNullPassword = createRequest.Password != null;
			var hasNonNullSystemIdentifier = createRequest.SystemIdentifier != null;
			var hasOAuthConnections = (createRequest.OAuthConnections?.Count > 0) == true;
			if ((hasNonNullPassword && hasNonNullSystemIdentifier)
				|| (!hasNonNullPassword && !hasNonNullSystemIdentifier && !hasOAuthConnections))
			{
				failResponse = BadRequest<User>(ErrorCode.UserMismatchPasswordSid);
				return true;
			}

			var hasZeroLengthPassword = createRequest.Password?.Length == 0;
			if (needZeroLengthPasswordWithOAuthConnections.HasValue)
			{
				if (needZeroLengthPasswordWithOAuthConnections.Value)
				{
					if (createRequest.OAuthConnections == null)
						throw new InvalidOperationException($"Expected {nameof(UserCreateRequest.OAuthConnections)} to be set here!");

					if (createRequest.OAuthConnections.Count == 0)
					{
						failResponse = BadRequest<User>(ErrorCode.ModelValidationFailure);
						return true;
					}
				}
				else if (hasZeroLengthPassword)
				{
					failResponse = BadRequest<User>(ErrorCode.ModelValidationFailure);
					return true;
				}
			}

			if (createRequest.Group != null && createRequest.PermissionSet != null)
			{
				failResponse = BadRequest<User>(ErrorCode.UserGroupAndPermissionSet);
				return true;
			}

			createRequest.Name = createRequest.Name?.Trim();
			if (createRequest.Name?.Length == 0)
				createRequest.Name = null;

			if (!(createRequest.Name == null ^ createRequest.SystemIdentifier == null))
			{
				failResponse = BadRequest<User>(ErrorCode.UserMismatchNameSid);
				return true;
			}

			failResponse = CheckValidName(createRequest, true);
			return failResponse != null;
		}

		/// <inheritdoc />
		public ValueTask<AuthorityResponse<User>> Read(CancellationToken cancellationToken)
			=> ValueTask.FromResult(new AuthorityResponse<User>(AuthenticationContext.User));

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<User>> GetId(long id, bool includeJoins, bool allowSystemUser, CancellationToken cancellationToken)
		{
			if (id != AuthenticationContext.User.Id && !((AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration)).HasFlag(AdministrationRights.ReadUsers))
				return Forbid<User>();

			User? user;
			if (includeJoins)
			{
				var queryable = Queryable(true, true);

				user = await queryable.FirstOrDefaultAsync(
					dbModel => dbModel.Id == id,
					cancellationToken);
			}
			else
				user = await usersDataLoader.LoadAsync(id, cancellationToken);

			if (user == default)
				return NotFound<User>();

			if (!allowSystemUser && user.CanonicalName == User.CanonicalizeName(User.TgsSystemUserName))
				return Forbid<User>();

			return new AuthorityResponse<User>(user);
		}

		/// <inheritdoc />
		public IQueryable<User> Queryable(bool includeJoins)
			=> Queryable(includeJoins, false);

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<GraphQL.Types.OAuth.OAuthConnection[]>> OAuthConnections(long userId, CancellationToken cancellationToken)
			=> new AuthorityResponse<GraphQL.Types.OAuth.OAuthConnection[]>(
				await oAuthConnectionsDataLoader.LoadRequiredAsync(userId, cancellationToken));

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<User>> Create(
			UserCreateRequest createRequest,
			bool? needZeroLengthPasswordWithOAuthConnections,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(createRequest);

			if (BadCreateRequestChecks(createRequest, needZeroLengthPasswordWithOAuthConnections, out var failResponse))
				return failResponse;

			var totalUsers = await DatabaseContext
				.Users
				.AsQueryable()
				.CountAsync(cancellationToken);
			if (totalUsers >= generalConfigurationOptions.Value.UserLimit)
				return Conflict<User>(ErrorCode.UserLimitReached);

			var dbUser = await CreateNewUserFromModel(createRequest, cancellationToken);
			if (dbUser == null)
				return Gone<User>();

			if (createRequest.SystemIdentifier != null)
				try
				{
					using var sysIdentity = await systemIdentityFactory.CreateSystemIdentity(dbUser, cancellationToken);
					if (sysIdentity == null)
						return Gone<User>();
					dbUser.Name = sysIdentity.Username;
					dbUser.SystemIdentifier = sysIdentity.Uid;
				}
				catch (NotImplementedException ex)
				{
					Logger.LogTrace(ex, "System identities not implemented!");
					return new AuthorityResponse<User>(
						new ErrorMessageResponse(ErrorCode.RequiresPosixSystemIdentity),
						HttpFailureResponse.NotImplemented);
				}
			else
			{
				var hasZeroLengthPassword = createRequest.Password?.Length == 0;
				var hasOAuthConnections = (createRequest.OAuthConnections?.Count > 0) == true;

				// special case allow PasswordHash to be null by setting Password to "" if OAuthConnections are set
				if (!(needZeroLengthPasswordWithOAuthConnections != false && hasZeroLengthPassword && hasOAuthConnections))
				{
					var result = TrySetPassword(dbUser, createRequest.Password!, true);
					if (result != null)
						return result;
				}
			}

			dbUser.CanonicalName = User.CanonicalizeName(dbUser.Name!);

			DatabaseContext.Users.Add(dbUser);

			await DatabaseContext.Save(cancellationToken);

			Logger.LogInformation("Created new user {name} ({id})", dbUser.Name, dbUser.Id);

			await SendUserUpdatedTopics(dbUser);

			return new AuthorityResponse<User>(dbUser, HttpSuccessResponse.Created);
		}

		/// <inheritdoc />
#pragma warning disable CA1502
#pragma warning disable CA1506 // TODO: Decomplexify
		public async ValueTask<AuthorityResponse<User>> Update(UserUpdateRequest model, CancellationToken cancellationToken)
#pragma warning restore CA1502
#pragma warning restore CA1506
		{
			ArgumentNullException.ThrowIfNull(model);

			if (!model.Id.HasValue || model.OAuthConnections?.Any(x => x == null) == true)
				return BadRequest<User>(ErrorCode.ModelValidationFailure);

			if (model.Group != null && model.PermissionSet != null)
				return BadRequest<User>(ErrorCode.UserGroupAndPermissionSet);

			var callerAdministrationRights = (AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration);
			var canEditAllUsers = callerAdministrationRights.HasFlag(AdministrationRights.WriteUsers);
			var passwordEdit = canEditAllUsers || callerAdministrationRights.HasFlag(AdministrationRights.EditOwnPassword);
			var oAuthEdit = canEditAllUsers || callerAdministrationRights.HasFlag(AdministrationRights.EditOwnOAuthConnections);

			var originalUser = !canEditAllUsers
				? AuthenticationContext.User
				: await DatabaseContext
					.Users
					.AsQueryable()
					.Where(x => x.Id == model.Id)
					.Include(x => x.CreatedBy)
					.Include(x => x.OAuthConnections)
					.Include(x => x.Group!)
						.ThenInclude(x => x.PermissionSet)
					.Include(x => x.PermissionSet)
					.FirstOrDefaultAsync(cancellationToken);

			if (originalUser == default)
				return NotFound<User>();

			if (originalUser.CanonicalName == User.CanonicalizeName(User.TgsSystemUserName))
				return Forbid<User>();

			// Ensure they are only trying to edit things they have perms for (system identity change will trigger a bad request)
			if ((!canEditAllUsers
				&& (model.Id != originalUser.Id
				|| model.Enabled.HasValue
				|| model.Group != null
				|| model.PermissionSet != null
				|| model.Name != null))
				|| (!passwordEdit && model.Password != null)
				|| (!oAuthEdit && model.OAuthConnections != null))
				return Forbid<User>();

			var originalUserHasSid = originalUser.SystemIdentifier != null;
			var invalidateSessions = false;
			if (originalUserHasSid && originalUser.PasswordHash != null)
			{
				// cleanup from https://github.com/tgstation/tgstation-server/issues/1528
				Logger.LogDebug("System user ID {userId}'s PasswordHash is polluted, updating database.", originalUser.Id);
				originalUser.PasswordHash = null;

				invalidateSessions = true;
			}

			if (model.SystemIdentifier != null && model.SystemIdentifier != originalUser.SystemIdentifier)
				return BadRequest<User>(ErrorCode.UserSidChange);

			if (model.Password != null)
			{
				if (originalUserHasSid)
					return BadRequest<User>(ErrorCode.UserMismatchPasswordSid);

				var result = TrySetPassword(originalUser, model.Password, false);
				if (result != null)
					return result;

				invalidateSessions = true;
			}

			if (model.Name != null && User.CanonicalizeName(model.Name) != originalUser.CanonicalName)
				return BadRequest<User>(ErrorCode.UserNameChange);

			if (model.OAuthConnections != null
				&& (model.OAuthConnections.Count != originalUser.OAuthConnections!.Count
				|| !model.OAuthConnections.All(x => originalUser.OAuthConnections.Any(y => y.Provider == x.Provider && y.ExternalUserId == x.ExternalUserId))))
			{
				if (originalUser.CanonicalName == User.CanonicalizeName(DefaultCredentials.AdminUserName))
					return BadRequest<User>(ErrorCode.AdminUserCannotOAuth);

				if (model.OAuthConnections.Count == 0 && originalUser.PasswordHash == null && originalUser.SystemIdentifier == null)
					return BadRequest<User>(ErrorCode.CannotRemoveLastAuthenticationOption);

				originalUser.OAuthConnections.Clear();
				foreach (var updatedConnection in model.OAuthConnections)
					originalUser.OAuthConnections.Add(new Models.OAuthConnection
					{
						Provider = updatedConnection.Provider,
						ExternalUserId = updatedConnection.ExternalUserId,
					});
			}

			if (model.Group != null)
			{
				originalUser.Group = await DatabaseContext
					.Groups
					.AsQueryable()
					.Where(x => x.Id == model.Group.Id)
					.Include(x => x.PermissionSet)
					.FirstOrDefaultAsync(cancellationToken);

				if (originalUser.Group == default)
					return Gone<User>();

				DatabaseContext.Groups.Attach(originalUser.Group);
				if (originalUser.PermissionSet != null)
				{
					Logger.LogInformation("Deleting permission set {permissionSetId}...", originalUser.PermissionSet.Id);
					DatabaseContext.PermissionSets.Remove(originalUser.PermissionSet);
					originalUser.PermissionSet = null;
				}
			}
			else if (model.PermissionSet != null)
			{
				if (originalUser.PermissionSet == null)
				{
					Logger.LogTrace("Creating new permission set...");
					originalUser.PermissionSet = new Models.PermissionSet();
				}

				originalUser.PermissionSet.AdministrationRights = model.PermissionSet.AdministrationRights ?? AdministrationRights.None;
				originalUser.PermissionSet.InstanceManagerRights = model.PermissionSet.InstanceManagerRights ?? InstanceManagerRights.None;

				originalUser.Group = null;
				originalUser.GroupId = null;
			}

			var fail = CheckValidName(model, false);
			if (fail != null)
				return fail;

			originalUser.Name = model.Name ?? originalUser.Name;

			if (model.Enabled.HasValue)
			{
				invalidateSessions = originalUser.Require(x => x.Enabled) && !model.Enabled.Value;
				originalUser.Enabled = model.Enabled.Value;
			}

			if (invalidateSessions)
				sessionInvalidationTracker.UserModifiedInvalidateSessions(originalUser);

			await DatabaseContext.Save(cancellationToken);

			Logger.LogInformation("Updated user {userName} ({userId})", originalUser.Name, originalUser.Id);

			if (invalidateSessions)
				await permissionsUpdateNotifyee.UserDisabled(originalUser, cancellationToken);

			await SendUserUpdatedTopics(originalUser);

			// return id only if not a self update and cannot read users
			var canReadBack = AuthenticationContext.User.Id == originalUser.Id
				|| callerAdministrationRights.HasFlag(AdministrationRights.ReadUsers);
			return canReadBack
				? new AuthorityResponse<User>(originalUser)
				: new AuthorityResponse<User>();
		}

		/// <summary>
		/// Send topics through the <see cref="topicEventSender"/> indicating a given <paramref name="user"/> was created or updated.
		/// </summary>
		/// <param name="user">The <see cref="User"/> that was created or updated.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask SendUserUpdatedTopics(User user)
			=> ValueTaskExtensions.WhenAll(
				GraphQL.Subscriptions.UserSubscriptions.UserUpdatedTopics(
					user.Require(x => x.Id))
					.Select(topic => topicEventSender.SendAsync(
						topic,
						((IApiTransformable<User, GraphQL.Types.User, UserGraphQLTransformer>)user).ToApi(),
						CancellationToken.None))); // DCT: Operation should always run

		/// <summary>
		/// Gets all registered <see cref="User"/>s.
		/// </summary>
		/// <param name="includeJoins">If related entities should be loaded.</param>
		/// <param name="allowSystemUser">If the <see cref="User"/> with the <see cref="User.TgsSystemUserName"/> should be included in results.</param>
		/// <returns>A <see cref="IQueryable{T}"/> of <see cref="User"/>s.</returns>
		IQueryable<User> Queryable(bool includeJoins, bool allowSystemUser)
		{
			var tgsUserCanonicalName = User.CanonicalizeName(User.TgsSystemUserName);
			var queryable = DatabaseContext
				.Users
				.AsQueryable();

			if (!allowSystemUser)
				queryable = queryable
					.Where(user => user.CanonicalName != tgsUserCanonicalName);

			if (includeJoins)
				queryable = queryable
					.Include(x => x.CreatedBy)
					.Include(x => x.OAuthConnections)
					.Include(x => x.Group!)
						.ThenInclude(x => x.PermissionSet)
					.Include(x => x.PermissionSet);

			return queryable;
		}

		/// <summary>
		/// Creates a new <see cref="User"/> from a given <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.Internal.UserApiBase"/> to use as a template.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="User"/> on success, <see langword="null"/> if the requested <see cref="UserGroup"/> did not exist.</returns>
		async ValueTask<User> CreateNewUserFromModel(Api.Models.Internal.UserApiBase model, CancellationToken cancellationToken)
		{
			Models.PermissionSet? permissionSet = null;
			UserGroup? group = null;
			if (model.Group != null)
				group = await DatabaseContext
					.Groups
					.AsQueryable()
					.Where(x => x.Id == model.Group.Id)
					.Include(x => x.PermissionSet)
					.FirstOrDefaultAsync(cancellationToken);
			else
				permissionSet = new Models.PermissionSet
				{
					AdministrationRights = model.PermissionSet?.AdministrationRights ?? AdministrationRights.None,
					InstanceManagerRights = model.PermissionSet?.InstanceManagerRights ?? InstanceManagerRights.None,
				};

			return new User
			{
				CreatedAt = DateTimeOffset.UtcNow,
				CreatedBy = AuthenticationContext.User,
				Enabled = model.Enabled ?? false,
				PermissionSet = permissionSet,
				Group = group,
				Name = model.Name,
				SystemIdentifier = model.SystemIdentifier,
				OAuthConnections = model
					.OAuthConnections
					?.Select(x => new Models.OAuthConnection
					{
						Provider = x.Provider,
						ExternalUserId = x.ExternalUserId,
					})
					.ToList()
					?? new List<Models.OAuthConnection>(),
			};
		}

		/// <summary>
		/// Attempt to change the password of a given <paramref name="dbUser"/>.
		/// </summary>
		/// <param name="dbUser">The user to update.</param>
		/// <param name="newPassword">The new password.</param>
		/// <param name="newUser">If this is for a new <see cref="UserResponse"/>.</param>
		/// <returns><see langword="null"/> on success, an errored <see cref="AuthorityResponse{TResult}"/> if <paramref name="newPassword"/> is too short.</returns>
		AuthorityResponse<User>? TrySetPassword(User dbUser, string newPassword, bool newUser)
		{
			newPassword ??= String.Empty;
			if (newPassword.Length < generalConfigurationOptions.Value.MinimumPasswordLength)
				return new AuthorityResponse<User>(
					new ErrorMessageResponse(ErrorCode.UserPasswordLength)
					{
						AdditionalData = $"Required password length: {generalConfigurationOptions.Value.MinimumPasswordLength}",
					},
					HttpFailureResponse.BadRequest);
			cryptographySuite.SetUserPassword(dbUser, newPassword, newUser);
			return null;
		}
	}
}
