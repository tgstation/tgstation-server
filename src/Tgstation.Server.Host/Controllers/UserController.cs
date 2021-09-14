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
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

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
		/// The <see cref="GeneralConfiguration"/> for the <see cref="UserController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/>.</param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		public UserController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			ISystemIdentityFactory systemIdentityFactory,
			ICryptographySuite cryptographySuite,
			ILogger<UserController> logger,
			IOptions<GeneralConfiguration> generalConfigurationOptions)
			: base(
				  databaseContext,
				  authenticationContextFactory,
				  logger,
				  true)
		{
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <summary>
		/// Create a new <see cref="User"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="201"><see cref="User"/> created successfully.</response>
		/// <response code="410">The requested system identifier could not be found.</response>
		[HttpPut]
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		[ProducesResponseType(typeof(UserResponse), 201)]
#pragma warning disable CA1502, CA1506
		public async Task<IActionResult> Create([FromBody] UserCreateRequest model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.OAuthConnections?.Any(x => x?.ExternalUserId == null) == true)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			if ((model.Password != null && model.SystemIdentifier != null)
				|| (model.Password == null && model.SystemIdentifier == null && model.OAuthConnections?.Any() != true))
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
				.CountAsync(cancellationToken)
				.ConfigureAwait(false);
			if (totalUsers >= generalConfiguration.UserLimit)
				return Conflict(new ErrorMessageResponse(ErrorCode.UserLimitReached));

			var dbUser = await CreateNewUserFromModel(model, cancellationToken).ConfigureAwait(false);
			if (dbUser == null)
				return Gone();

			if (model.SystemIdentifier != null)
				try
				{
					using var sysIdentity = await systemIdentityFactory.CreateSystemIdentity(dbUser, cancellationToken).ConfigureAwait(false);
					if (sysIdentity == null)
						return Gone();
					dbUser.Name = sysIdentity.Username;
					dbUser.SystemIdentifier = sysIdentity.Uid;
				}
				catch (NotImplementedException)
				{
					return RequiresPosixSystemIdentity();
				}
			else if (!(model.Password?.Length == 0 && model.OAuthConnections?.Any() == true))
			{
				var result = TrySetPassword(dbUser, model.Password!, true);
				if (result != null)
					return result;
			}

			dbUser.CanonicalName = Models.User.CanonicalizeName(dbUser.Name!);

			DatabaseContext.Users.Add(dbUser);

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			Logger.LogInformation("Created new user {0} ({1})", dbUser.Name, dbUser.Id);

			return Created(dbUser.ToApi());
		}
#pragma warning restore CA1502, CA1506

		/// <summary>
		/// Update a <see cref="User"/>.
		/// </summary>
		/// <param name="model">The <see cref="UserResponse"/> to update.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
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
		public async Task<IActionResult> Update([FromBody] UserUpdateRequest model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (!model.Id.HasValue || model.OAuthConnections?.Any(x => x?.ExternalUserId == null) == true)
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
					.Include(x => x.Group)
						.ThenInclude(x => x!.PermissionSet)
					.Include(x => x.PermissionSet)
					.FirstOrDefaultAsync(cancellationToken)
					.ConfigureAwait(false);

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

			if (model.SystemIdentifier != null && model.SystemIdentifier != originalUser.SystemIdentifier)
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserSidChange));

			if (model.Password != null)
			{
				var result = TrySetPassword(originalUser, model.Password, false);
				if (result != null)
					return result;
			}

			if (model.Name != null && Models.User.CanonicalizeName(model.Name) != originalUser.CanonicalName)
				return BadRequest(new ErrorMessageResponse(ErrorCode.UserNameChange));

			if (model.Enabled.HasValue)
			{
				if (originalUser.Enabled && !model.Enabled.Value)
					originalUser.LastPasswordUpdate = DateTimeOffset.UtcNow;

				originalUser.Enabled = model.Enabled.Value;
			}

			if (model.OAuthConnections != null
				&& (model.OAuthConnections.Count != originalUser.OAuthConnections.Count
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
						ExternalUserId = updatedConnection.ExternalUserId!,
					});
			}

			if (model.Group != null)
			{
				originalUser.Group = await DatabaseContext
					.Groups
					.AsQueryable()
					.Where(x => x.Id == model.Group.Id)
					.Include(x => x.PermissionSet)
					.FirstOrDefaultAsync(cancellationToken)
					.ConfigureAwait(false);

				if (originalUser.Group == default)
					return Gone();

				DatabaseContext.Groups.Attach(originalUser.Group);
				if (originalUser.PermissionSet != null)
				{
					Logger.LogInformation("Deleting permission set {0}...", originalUser.PermissionSet.Id);
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

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			Logger.LogInformation("Updated user {0} ({1})", originalUser.Name, originalUser.Id);

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
		/// <returns>The <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">The <see cref="User"/> was retrieved successfully.</response>
		[HttpGet]
		[TgsAuthorize]
		[ProducesResponseType(typeof(UserResponse), 200)]
		public IActionResult Read() => Json(AuthenticationContext.User.ToApi());

		/// <summary>
		/// List all <see cref="User"/>s in the server.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Retrieved <see cref="User"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		[ProducesResponseType(typeof(PaginatedResponse<UserResponse>), 200)]
		public Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<User, UserResponse>(
				() => Task.FromResult(
					new PaginatableResult<User>(
						DatabaseContext
							.Users
							.AsQueryable()
							.Where(x => x.CanonicalName != Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
							.Include(x => x.CreatedBy)
							.Include(x => x.PermissionSet)
							.Include(x => x.OAuthConnections)
							.Include(x => x.Group)
								.ThenInclude(x => x!.PermissionSet)
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
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">The <see cref="User"/> was retrieved successfully.</response>
		/// <response code="404">The <see cref="User"/> does not exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize]
		[ProducesResponseType(typeof(UserResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 404)]
		public async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			if (id == AuthenticationContext.User.Id)
				return Read();

			if (!((AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration)).HasFlag(AdministrationRights.ReadUsers))
				return Forbid();

			var user = await DatabaseContext.Users
				.AsQueryable()
				.Where(x => x.Id == id)
				.Include(x => x.CreatedBy)
				.Include(x => x.OAuthConnections)
				.Include(x => x.Group)
					.ThenInclude(x => x!.PermissionSet)
				.Include(x => x.PermissionSet)
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (user == default)
				return NotFound();

			if (user.CanonicalName == Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
				return Forbid();

			return Json(user.ToApi());
		}

		/// <summary>
		/// Creates a new <see cref="User"/> from a given <paramref name="validated"/>.
		/// </summary>
		/// <param name="validated">The validated <see cref="Api.Models.Internal.UserApiBase"/> to use as a template.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="User"/> on success, <see langword="null"/> if the requested <see cref="UserGroup"/> did not exist.</returns>
		async Task<User> CreateNewUserFromModel(Api.Models.Internal.UserApiBase validated, CancellationToken cancellationToken)
		{
			Models.PermissionSet? permissionSet = null;
			UserGroup? group = null;
			if (validated.Group != null)
				group = await DatabaseContext
					.Groups
					.AsQueryable()
					.Where(x => x.Id == validated.Group.Id)
					.Include(x => x.PermissionSet)
					.FirstOrDefaultAsync(cancellationToken)
					.ConfigureAwait(false);
			else
				permissionSet = new Models.PermissionSet
				{
					AdministrationRights = validated.PermissionSet?.AdministrationRights ?? AdministrationRights.None,
					InstanceManagerRights = validated.PermissionSet?.InstanceManagerRights ?? InstanceManagerRights.None,
				};

			return new User
			{
				CreatedAt = DateTimeOffset.UtcNow,
				CreatedBy = AuthenticationContext.User,
				Enabled = validated.Enabled ?? false,
				PermissionSet = permissionSet,
				Group = group,
				Name = validated.Name!,
				SystemIdentifier = validated.SystemIdentifier,
				OAuthConnections = validated
					.OAuthConnections
					?.Select(x => new Models.OAuthConnection
					{
						Provider = x.Provider,
						ExternalUserId = x.ExternalUserId!,
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
