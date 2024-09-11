using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="User"/>s.
	/// </summary>
	[Route(Routes.User)]
	public sealed class UserController : ApiController
	{
		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="UserController"/>.
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="UserController"/>.
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="IPermissionsUpdateNotifyee"/> for the <see cref="UserController"/>.
		/// </summary>
		readonly IPermissionsUpdateNotifyee permissionsUpdateNotifyee;

		/// <summary>
		/// The <see cref="IRestAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.
		/// </summary>
		readonly IRestAuthorityInvoker<IUserAuthority> userAuthority;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="UserController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="userAuthority">The value of <see cref="userAuthority"/>.</param>
		/// <param name="permissionsUpdateNotifyee">The value of <see cref="permissionsUpdateNotifyee"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="ApiController"/>.</param>
		public UserController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ISystemIdentityFactory systemIdentityFactory,
			ICryptographySuite cryptographySuite,
			IPermissionsUpdateNotifyee permissionsUpdateNotifyee,
			IRestAuthorityInvoker<IUserAuthority> userAuthority,
			ILogger<UserController> logger,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IApiHeadersProvider apiHeaders)
			: base(
				  databaseContext,
				  authenticationContext,
				  apiHeaders,
				  logger,
				  true)
		{
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.permissionsUpdateNotifyee = permissionsUpdateNotifyee ?? throw new ArgumentNullException(nameof(permissionsUpdateNotifyee));
			this.userAuthority = userAuthority ?? throw new ArgumentNullException(nameof(userAuthority));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <summary>
		/// Create a new <see cref="User"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="201"><see cref="User"/> created successfully.</response>
		/// <response code="410">The requested system identifier could not be found.</response>
		[HttpPut]
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		[ProducesResponseType(typeof(UserResponse), 201)]
#pragma warning disable CA1502, CA1506
		public async ValueTask<IActionResult> Create([FromBody] UserCreateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (model.OAuthConnections?.Any(x => x == null) == true)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			if ((model.Password != null && model.SystemIdentifier != null)
				|| (model.Password == null && model.SystemIdentifier == null && (model.OAuthConnections?.Count > 0) != true))
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserMismatchPasswordSid));

			if (model.Group != null && model.PermissionSet != null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserGroupAndPermissionSet));

			model.Name = model.Name?.Trim();
			if (model.Name?.Length == 0)
				model.Name = null;

			if (!(model.Name == null ^ model.SystemIdentifier == null))
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserMismatchNameSid));

			var fail = CheckValidName(model, true);
			if (fail != null)
				return fail;

			var totalUsers = await DatabaseContext
				.Users
				.AsQueryable()
				.CountAsync(cancellationToken);
			if (totalUsers >= generalConfiguration.UserLimit)
				return Conflict(new ErrorMessageResponse(ErrorCode.UserLimitReached));

			var dbUser = await CreateNewUserFromModel(model, cancellationToken);
			if (dbUser == null)
				return this.Gone();

			if (model.SystemIdentifier != null)
				try
				{
					using var sysIdentity = await systemIdentityFactory.CreateSystemIdentity(dbUser, cancellationToken);
					if (sysIdentity == null)
						return this.Gone();
					dbUser.Name = sysIdentity.Username;
					dbUser.SystemIdentifier = sysIdentity.Uid;
				}
				catch (NotImplementedException ex)
				{
					return RequiresPosixSystemIdentity(ex);
				}
			else if (!(model.Password?.Length == 0 && (model.OAuthConnections?.Count > 0) == true))
			{
				var result = TrySetPassword(dbUser, model.Password!, true);
				if (result != null)
					return result;
			}

			dbUser.CanonicalName = Models.User.CanonicalizeName(dbUser.Name!);

			DatabaseContext.Users.Add(dbUser);

			await DatabaseContext.Save(cancellationToken);

			Logger.LogInformation("Created new user {name} ({id})", dbUser.Name, dbUser.Id);

			return this.Created(dbUser.ToApi());
		}
#pragma warning restore CA1502, CA1506

		/// <summary>
		/// Update a <see cref="User"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserResponse"/> to update.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200"><see cref="User"/> updated successfully.</response>
		/// <response code="200"><see cref="User"/> updated successfully. Not returned due to lack of permissions.</response>
		/// <response code="404">Requested <see cref="EntityId.Id"/> does not exist.</response>
		/// <response code="410">Requested <see cref="Api.Models.Internal.UserApiBase.Group"/> does not exist.</response>
		[HttpPost]
		[TgsAuthorize(AdministrationRights.WriteUsers | AdministrationRights.EditOwnPassword | AdministrationRights.EditOwnOAuthConnections)]
		[ProducesResponseType(typeof(UserResponse), 200)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 404)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
#pragma warning disable CA1502 // TODO: Decomplexify
#pragma warning disable CA1506
		public async ValueTask<IActionResult> Update([FromBody] UserUpdateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (!model.Id.HasValue || model.OAuthConnections?.Any(x => x == null) == true)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			if (model.Group != null && model.PermissionSet != null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserGroupAndPermissionSet));

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
				return NotFound();

			if (originalUser.CanonicalName == Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
				return Forbid();

			// Ensure they are only trying to edit things they have perms for (system identity change will trigger a bad request)
			if ((!canEditAllUsers
				&& (model.Id != originalUser.Id
				|| model.Enabled.HasValue
				|| model.Group != null
				|| model.PermissionSet != null
				|| model.Name != null))
				|| (!passwordEdit && model.Password != null)
				|| (!oAuthEdit && model.OAuthConnections != null))
				return Forbid();

			var originalUserHasSid = originalUser.SystemIdentifier != null;
			if (originalUserHasSid && originalUser.PasswordHash != null)
			{
				// cleanup from https://github.com/tgstation/tgstation-server/issues/1528
				Logger.LogDebug("System user ID {userId}'s PasswordHash is polluted, updating database.", originalUser.Id);
				originalUser.PasswordHash = null;
				originalUser.LastPasswordUpdate = DateTimeOffset.UtcNow;
			}

			if (model.SystemIdentifier != null && model.SystemIdentifier != originalUser.SystemIdentifier)
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserSidChange));

			if (model.Password != null)
			{
				if (originalUserHasSid)
					return BadRequest(new ErrorMessageResponse(ErrorCode.UserMismatchPasswordSid));

				var result = TrySetPassword(originalUser, model.Password, false);
				if (result != null)
					return result;
			}

			if (model.Name != null && Models.User.CanonicalizeName(model.Name) != originalUser.CanonicalName)
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserNameChange));

			bool userWasDisabled;
			if (model.Enabled.HasValue)
			{
				userWasDisabled = originalUser.Require(x => x.Enabled) && !model.Enabled.Value;
				if (userWasDisabled)
					originalUser.LastPasswordUpdate = DateTimeOffset.UtcNow;

				originalUser.Enabled = model.Enabled.Value;
			}
			else
				userWasDisabled = false;

			if (model.OAuthConnections != null
				&& (model.OAuthConnections.Count != originalUser.OAuthConnections!.Count
				|| !model.OAuthConnections.All(x => originalUser.OAuthConnections.Any(y => y.Provider == x.Provider && y.ExternalUserId == x.ExternalUserId))))
			{
				if (originalUser.CanonicalName == Models.User.CanonicalizeName(DefaultCredentials.AdminUserName))
					return BadRequest(new ErrorMessageResponse(ErrorCode.AdminUserCannotOAuth));

				if (model.OAuthConnections.Count == 0 && originalUser.PasswordHash == null && originalUser.SystemIdentifier == null)
					return BadRequest(new ErrorMessageResponse(ErrorCode.CannotRemoveLastAuthenticationOption));

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
					return this.Gone();

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

			await DatabaseContext.Save(cancellationToken);

			Logger.LogInformation("Updated user {userName} ({userId})", originalUser.Name, originalUser.Id);

			if (userWasDisabled)
				await permissionsUpdateNotifyee.UserDisabled(originalUser, cancellationToken);

			// return id only if not a self update and cannot read users
			var canReadBack = AuthenticationContext.User.Id == originalUser.Id
				|| callerAdministrationRights.HasFlag(AdministrationRights.ReadUsers);
			return canReadBack
				? Json(originalUser.ToApi())
				: NoContent();
		}
#pragma warning restore CA1506
#pragma warning restore CA1502

		/// <summary>
		/// Get information about the current <see cref="User"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">The <see cref="User"/> was retrieved successfully.</response>
		[HttpGet]
		[TgsRestAuthorize<IUserAuthority>(nameof(IUserAuthority.Read))]
		[ProducesResponseType(typeof(UserResponse), 200)]
		public ValueTask<IActionResult> Read(CancellationToken cancellationToken)
			=> userAuthority.InvokeTransformable<User, UserResponse>(this, authority => authority.Read(cancellationToken));

		/// <summary>
		/// List all <see cref="User"/>s in the server.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Retrieved <see cref="User"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		[ProducesResponseType(typeof(PaginatedResponse<UserResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<User, UserResponse>(
				() => ValueTask.FromResult(
					new PaginatableResult<User>(
						DatabaseContext
							.Users
							.AsQueryable()
							.Where(x => x.CanonicalName != Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
							.Include(x => x.CreatedBy)
							.Include(x => x.PermissionSet)
							.Include(x => x.OAuthConnections)
							.Include(x => x.Group!)
								.ThenInclude(x => x.PermissionSet)
							.OrderBy(x => x.Id))),
				null,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// Get a specific <see cref="User"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> to retrieve.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">The <see cref="User"/> was retrieved successfully.</response>
		/// <response code="404">The <see cref="User"/> does not exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize]
		[ProducesResponseType(typeof(UserResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 404)]
		public async ValueTask<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			if (id == AuthenticationContext.User.Id)
				return await Read(cancellationToken);

			if (!((AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration)).HasFlag(AdministrationRights.ReadUsers))
				return Forbid();

			return await userAuthority.InvokeTransformable<User, UserResponse>(
				this,
				authority => authority.GetId(id, true, cancellationToken));
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
		/// Check if a given <paramref name="model"/> has a valid <see cref="UserName.Name"/> specified.
		/// </summary>
		/// <param name="model">The <see cref="UserUpdateRequest"/> to check.</param>
		/// <param name="newUser">If this is a new <see cref="UserResponse"/>.</param>
		/// <returns><see langword="null"/> if <paramref name="model"/> is valid, a <see cref="BadRequestObjectResult"/> otherwise.</returns>
		BadRequestObjectResult? CheckValidName(UserUpdateRequest model, bool newUser)
		{
			var userInvalidWithNullName = newUser && model.Name == null && model.SystemIdentifier == null;
			if (userInvalidWithNullName || (model.Name != null && String.IsNullOrWhiteSpace(model.Name)))
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserMissingName));

			model.Name = model.Name?.Trim();
			if (model.Name != null && model.Name.Contains(':', StringComparison.InvariantCulture))
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserColonInName));
			return null;
		}

		/// <summary>
		/// Attempt to change the password of a given <paramref name="dbUser"/>.
		/// </summary>
		/// <param name="dbUser">The user to update.</param>
		/// <param name="newPassword">The new password.</param>
		/// <param name="newUser">If this is for a new <see cref="UserResponse"/>.</param>
		/// <returns><see langword="null"/> on success, <see cref="BadRequestObjectResult"/> if <paramref name="newPassword"/> is too short.</returns>
		BadRequestObjectResult? TrySetPassword(User dbUser, string newPassword, bool newUser)
		{
			newPassword ??= String.Empty;
			if (newPassword.Length < generalConfiguration.MinimumPasswordLength)
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserPasswordLength)
				{
					AdditionalData = $"Required password length: {generalConfiguration.MinimumPasswordLength}",
				});
			cryptographySuite.SetUserPassword(dbUser, newPassword, newUser);
			return null;
		}
	}
}
