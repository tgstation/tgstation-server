using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
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
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="UserController"/>
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="UserController"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="UserController"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Construct a <see cref="UserController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/></param>
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
		/// Check if a given <paramref name="model"/> has a valid <see cref="Api.Models.Internal.User.Name"/> specified.
		/// </summary>
		/// <param name="model">The <see cref="UserUpdate"/> to check.</param>
		/// <param name="newUser">If this is a new <see cref="Api.Models.User"/>.</param>
		/// <returns><see langword="null"/> if <paramref name="model"/> is valid, a <see cref="BadRequestObjectResult"/> otherwise.</returns>
		BadRequestObjectResult CheckValidName(UserUpdate model, bool newUser)
		{
			var userInvalidWithNullName = newUser && model.Name == null && model.SystemIdentifier == null;
#pragma warning disable CA1508 // https://github.com/dotnet/roslyn-analyzers/issues/3685
			if (userInvalidWithNullName || (model.Name != null && String.IsNullOrWhiteSpace(model.Name)))
#pragma warning restore CA1508
				return BadRequest(new ErrorMessage(ErrorCode.UserMissingName));

			model.Name = model.Name?.Trim();
			if (model.Name != null && model.Name.Contains(':', StringComparison.InvariantCulture))
				return BadRequest(new ErrorMessage(ErrorCode.UserColonInName));
			return null;
		}

		/// <summary>
		/// Attempt to change the password of a given <paramref name="dbUser"/>.
		/// </summary>
		/// <param name="dbUser">The user to update.</param>
		/// <param name="newPassword">The new password.</param>
		/// <param name="newUser">If this is for a new <see cref="Api.Models.User"/>.</param>
		/// <returns><see langword="null"/> on success, <see cref="BadRequestObjectResult"/> if <paramref name="newPassword"/> is too short.</returns>
		BadRequestObjectResult TrySetPassword(Models.User dbUser, string newPassword, bool newUser)
		{
			newPassword ??= String.Empty;
			if (newPassword.Length < generalConfiguration.MinimumPasswordLength)
				return BadRequest(new ErrorMessage(ErrorCode.UserPasswordLength)
				{
					AdditionalData = $"Required password length: {generalConfiguration.MinimumPasswordLength}"
				});
			cryptographySuite.SetUserPassword(dbUser, newPassword, newUser);
			return null;
		}

		/// <summary>
		/// Create a <see cref="Api.Models.User"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.User"/> to create.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="201"><see cref="Api.Models.User"/> created successfully.</response>
		/// <response code="410">The requested system identifier could not be found.</response>
		[HttpPut]
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		[ProducesResponseType(typeof(Api.Models.User), 201)]
		public async Task<IActionResult> Create([FromBody] UserUpdate model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.OAuthConnections?.Any(x => x == null) == true)
				return BadRequest(new ErrorMessage(ErrorCode.ModelValidationFailure));

			if (!(model.Password == null ^ model.SystemIdentifier == null))
				return BadRequest(new ErrorMessage(ErrorCode.UserMismatchPasswordSid));

			model.Name = model.Name?.Trim();
			if (model.Name?.Length == 0)
				model.Name = null;

			if (!(model.Name == null ^ model.SystemIdentifier == null))
				return BadRequest(new ErrorMessage(ErrorCode.UserMismatchNameSid));

			var fail = CheckValidName(model, true);
			if (fail != null)
				return fail;

			var dbUser = CreateNewUserFromModel(model);

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
			else if (!(model.Password?.Length == 0 && model.OAuthConnections.Count != 0))
			{
				var result = TrySetPassword(dbUser, model.Password, true);
				if (result != null)
					return result;
			}

			dbUser.CanonicalName = Models.User.CanonicalizeName(dbUser.Name);

			DatabaseContext.Users.Add(dbUser);

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			Logger.LogInformation("Created new user {0} ({1})", dbUser.Name, dbUser.Id);

			return Created(dbUser.ToApi(true));
		}

		/// <summary>
		/// Update a <see cref="Api.Models.User"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.User"/> to update.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200"><see cref="Api.Models.User"/> updated successfully.</response>
		/// <response code="404">Requested <see cref="Api.Models.Internal.User.Id"/> does not exist.</response>
		[HttpPost]
		[TgsAuthorize(AdministrationRights.WriteUsers | AdministrationRights.EditOwnPassword | AdministrationRights.EditOwnOAuthConnections)]
		[ProducesResponseType(typeof(Api.Models.User), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 404)]
		#pragma warning disable CA1502 // TODO: Decomplexify
		#pragma warning disable CA1506
		public async Task<IActionResult> Update([FromBody] UserUpdate model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (!model.Id.HasValue || model.OAuthConnections?.Any(x => x == null) == true)
				return BadRequest(new ErrorMessage(ErrorCode.ModelValidationFailure));

			var callerAdministrationRights = (AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration);
			var canEditAllUsers = callerAdministrationRights.HasFlag(AdministrationRights.WriteUsers);
			var passwordEdit = canEditAllUsers || callerAdministrationRights.HasFlag(AdministrationRights.EditOwnPassword);
			var oAuthEdit = canEditAllUsers || callerAdministrationRights.HasFlag(AdministrationRights.EditOwnOAuthConnections);

			var originalUser = passwordEdit
				? AuthenticationContext.User
				: await DatabaseContext
					.Users
					.AsQueryable()
					.Where(x => x.Id == model.Id)
					.Include(x => x.CreatedBy)
					.Include(x => x.OAuthConnections)
					.FirstOrDefaultAsync(cancellationToken)
					.ConfigureAwait(false);

			if (originalUser == default)
				return NotFound();

			if (originalUser.CanonicalName == Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
				return Forbid();

			// Ensure they are only trying to edit things they have perms for (system identity change will trigger a bad request)
			if ((!canEditAllUsers
				&& (model.Id != originalUser.Id
				|| model.InstanceManagerRights.HasValue
				|| model.AdministrationRights.HasValue
				|| model.Enabled.HasValue
				|| model.Name != null))
				|| (!passwordEdit && model.Password != null)
				|| (!oAuthEdit && model.OAuthConnections != null))
				return Forbid();

			if (model.SystemIdentifier != null && model.SystemIdentifier != originalUser.SystemIdentifier)
				return BadRequest(new ErrorMessage(ErrorCode.UserSidChange));

			if (model.Password != null)
			{
				var result = TrySetPassword(originalUser, model.Password, false);
				if (result != null)
					return result;
			}

			if (model.Name != null && Models.User.CanonicalizeName(model.Name) != originalUser.CanonicalName)
				return BadRequest(new ErrorMessage(ErrorCode.UserNameChange));

			originalUser.InstanceManagerRights = RightsHelper.Clamp(model.InstanceManagerRights ?? originalUser.InstanceManagerRights.Value);
			originalUser.AdministrationRights = RightsHelper.Clamp(model.AdministrationRights ?? originalUser.AdministrationRights.Value);
			if (model.Enabled.HasValue)
			{
				if (originalUser.Enabled.Value && !model.Enabled.Value)
					originalUser.LastPasswordUpdate = DateTimeOffset.Now;

				originalUser.Enabled = model.Enabled.Value;
			}

			if (model.OAuthConnections != null
				&& (model.OAuthConnections.Count != originalUser.OAuthConnections.Count
				|| !model.OAuthConnections.All(x => originalUser.OAuthConnections.Any(y => y.Provider == x.Provider && y.ExternalUserId == x.ExternalUserId))))
			{
				if (originalUser.CanonicalName == Models.User.CanonicalizeName(Api.Models.User.AdminName))
					return BadRequest(new ErrorMessage(ErrorCode.AdminUserCannotOAuth));

				originalUser.OAuthConnections.Clear();
				foreach (var updatedConnection in model.OAuthConnections)
					originalUser.OAuthConnections.Add(new Models.OAuthConnection
					{
						Provider = updatedConnection.Provider,
						ExternalUserId = updatedConnection.ExternalUserId
					});
			}

			var fail = CheckValidName(model, false);
			if (fail != null)
				return fail;

			originalUser.Name = model.Name ?? originalUser.Name;

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			Logger.LogInformation("Updated user {0} ({1})", originalUser.Name, originalUser.Id);

			// return id only if not a self update and cannot read users
			return Json(
				model.Id == originalUser.Id
				|| callerAdministrationRights.HasFlag(AdministrationRights.ReadUsers)
				? originalUser.ToApi(true)
				: new Api.Models.User
				{
					Id = originalUser.Id
				});
		}
		#pragma warning restore CA1506
		#pragma warning restore CA1502

		/// <summary>
		/// Get information about the current <see cref="Api.Models.User"/>.
		/// </summary>
		/// <returns>The <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">The <see cref="Api.Models.User"/> was retrieved successfully.</response>
		[HttpGet]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Api.Models.User), 200)]
		public IActionResult Read() => Json(AuthenticationContext.User.ToApi(true));

		/// <summary>
		/// List all <see cref="Api.Models.User"/>s in the server.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.User"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		[ProducesResponseType(typeof(IEnumerable<Api.Models.User>), 200)]
		public async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			var users = await DatabaseContext
				.Users
				.AsQueryable()
				.Where(x => x.CanonicalName != Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
				.Include(x => x.CreatedBy)
				.Include(x => x.OAuthConnections)
				.ToListAsync(cancellationToken).ConfigureAwait(false);
			return Json(users.Select(x => x.ToApi(true)));
		}

		/// <summary>
		/// Get a specific <see cref="Api.Models.User"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.Internal.User.Id"/> to retrieve.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">The <see cref="Api.Models.User"/> was retrieved successfully.</response>
		/// <response code="404">The <see cref="Api.Models.User"/> does not exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Api.Models.User), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 404)]
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
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (user == default)
				return NotFound();

			if (user.CanonicalName == Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
				return Forbid();

			return Json(user.ToApi(true));
		}

		/// <summary>
		/// Creates a new <see cref="User"/> from a given <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.User"/> to use as a template.</param>
		/// <returns>A new <see cref="User"/>.</returns>
		Models.User CreateNewUserFromModel(Api.Models.User model) => new Models.User
		{
			AdministrationRights = RightsHelper.Clamp(model.AdministrationRights ?? AdministrationRights.None),
			CreatedAt = DateTimeOffset.Now,
			CreatedBy = AuthenticationContext.User,
			Enabled = model.Enabled ?? false,
			InstanceManagerRights = RightsHelper.Clamp(model.InstanceManagerRights ?? InstanceManagerRights.None),
			Name = model.Name,
			SystemIdentifier = model.SystemIdentifier,
			InstanceUsers = new List<Models.InstanceUser>(),
			OAuthConnections = model
					.OAuthConnections
					?.Select(x => new Models.OAuthConnection
					{
						Provider = x.Provider,
						ExternalUserId = x.ExternalUserId
					})
					.ToList()
		};
	}
}
