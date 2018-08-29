using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// For managing <see cref="User"/>s
	/// </summary>
	[Route(Routes.User)]
	public sealed class UserController : ModelController<UserUpdate>
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
		/// The <see cref="ILogger"/> for the <see cref="UserController"/>
		/// </summary>
		readonly ILogger<UserController> logger;

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
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/></param>
		public UserController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, ISystemIdentityFactory systemIdentityFactory, ICryptographySuite cryptographySuite, ILogger<UserController> logger, IOptions<GeneralConfiguration> generalConfigurationOptions) : base(databaseContext, authenticationContextFactory, logger, false)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <inheritdoc />
		[TgsAuthorize(AdministrationRights.WriteUsers)]
		public override async Task<IActionResult> Create([FromBody] UserUpdate model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (!(model.Password == null ^ model.SystemIdentifier == null))
				return BadRequest(new ErrorMessage { Message = "User must have exactly one of either a password or system identifier!" });

			model.Name = model.Name?.Trim();
			if (model.Name?.Length == 0)
				model.Name = null;

			if (!(model.Name == null ^ model.SystemIdentifier == null))
				return BadRequest(new ErrorMessage { Message = "User must have a name if and only if user has no system identifier!" });

			var dbUser = new Models.User
			{
				AdministrationRights = model.AdministrationRights ?? AdministrationRights.None,
				CreatedAt = DateTimeOffset.Now,
				CreatedBy = AuthenticationContext.User,
				Enabled = model.Enabled ?? false,
				InstanceManagerRights = model.InstanceManagerRights ?? InstanceManagerRights.None,
				Name = model.Name,
				SystemIdentifier = model.SystemIdentifier,
				InstanceUsers = new List<Models.InstanceUser>()
			};

			if (model.SystemIdentifier != null)
				try
				{
					using (var sysIdentity = await systemIdentityFactory.CreateSystemIdentity(dbUser, cancellationToken).ConfigureAwait(false))
					{
						if (sysIdentity == null)
							return StatusCode((int)HttpStatusCode.Gone);
						dbUser.Name = sysIdentity.Username;
						dbUser.SystemIdentifier = sysIdentity.Uid;
					}
				}
				catch (NotImplementedException)
				{
					return StatusCode((int)HttpStatusCode.NotImplemented);
				}
			else
			{
				if (model.Password.Length < generalConfiguration.MinimumPasswordLength)
					return BadRequest(new ErrorMessage { Message = String.Format(CultureInfo.InvariantCulture, "Password must be at least {0} characters long!", generalConfiguration.MinimumPasswordLength) });
				cryptographySuite.SetUserPassword(dbUser, model.Password);
			}

			dbUser.CanonicalName = dbUser.Name.ToUpperInvariant();

			DatabaseContext.Users.Add(dbUser);

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			return StatusCode((int)HttpStatusCode.Created, dbUser.ToApi(true));
		}

		/// <inheritdoc />
		[TgsAuthorize(AdministrationRights.WriteUsers | AdministrationRights.EditOwnPassword)]
		public override async Task<IActionResult> Update([FromBody] UserUpdate model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var passwordEditOnly = !AuthenticationContext.User.AdministrationRights.Value.HasFlag(AdministrationRights.WriteUsers);

			var originalUser = passwordEditOnly ? AuthenticationContext.User : await DatabaseContext.Users.Where(x => x.Id == model.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (originalUser == default)
				return NotFound();

			if (passwordEditOnly && (model.Id != originalUser.Id || model.InstanceManagerRights.HasValue || model.AdministrationRights.HasValue || model.Enabled.HasValue || model.SystemIdentifier != null || model.Name != null))
				return Forbid();

			if (model.Password != null)
			{
				if (originalUser.PasswordHash == null)
					return BadRequest(new ErrorMessage { Message = "Cannot convert a system user to a password user!" });
				cryptographySuite.SetUserPassword(originalUser, model.Password);
			}
			else if (model.SystemIdentifier != null && model.SystemIdentifier != originalUser.SystemIdentifier)
				return BadRequest(new ErrorMessage { Message = "Cannot change a user's system identifier!" });

			if (model.Name != null && model.Name.ToUpperInvariant() != originalUser.CanonicalName)
				return BadRequest(new ErrorMessage { Message = "Can only change capitalization of a user's name!" });

			originalUser.InstanceManagerRights = model.InstanceManagerRights ?? originalUser.InstanceManagerRights;
			originalUser.AdministrationRights = model.AdministrationRights ?? originalUser.AdministrationRights;
			originalUser.Enabled = model.Enabled ?? originalUser.Enabled;
			originalUser.Name = model.Name ?? originalUser.Name;

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			return Json(originalUser.ToApi(true));
		}

		/// <inheritdoc />
		[TgsAuthorize]
		public override Task<IActionResult> Read(CancellationToken cancellationToken) => Task.FromResult<IActionResult>(Json(AuthenticationContext.User.ToApi(true)));

		/// <inheritdoc />
		[TgsAuthorize(AdministrationRights.ReadUsers)]
		public override async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			var users = await DatabaseContext.Users.ToListAsync(cancellationToken).ConfigureAwait(false);
			return Json(users.Select(x => x.ToApi(true)));
		}

		/// <inheritdoc />
		[TgsAuthorize]
		public override async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			if (id == AuthenticationContext.User.Id)
				return await Read(cancellationToken).ConfigureAwait(false);

			if (!((AdministrationRights)AuthenticationContext.GetRight(RightsType.Administration)).HasFlag(AdministrationRights.ReadUsers))
				return Forbid();

			var user = await DatabaseContext.Users.Where(x => x.Id == id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (user == default)
				return NotFound();
			return Json(user.ToApi(true));
		}
	}
}
