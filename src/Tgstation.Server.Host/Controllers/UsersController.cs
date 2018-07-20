using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// For managing <see cref="User"/>s
	/// </summary>
	[Route("/" + nameof(Models.User))]
	public sealed class UsersController : ModelController<UserUpdate>
	{
		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="UsersController"/>
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="UsersController"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="UsersController"/>
		/// </summary>
		readonly ILogger<UsersController> logger;

		/// <summary>
		/// Construct a <see cref="UsersController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public UsersController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, ISystemIdentityFactory systemIdentityFactory, ICryptographySuite cryptographySuite, ILogger<UsersController> logger) : base(databaseContext, authenticationContextFactory)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		[TgsAuthorize(AdministrationRights.EditUsers)]
		public override async Task<IActionResult> Create([FromBody] UserUpdate model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.Name == null)
				return BadRequest(new { message = "Missing user name!" });

			if (model.Password == null && model.SystemIdentifier == null)
				return BadRequest(new { message = "User must have either a password or system identifier!" });

			var dbUser = new Models.User
			{
				AdministrationRights = model.AdministrationRights,
				CreatedAt = DateTimeOffset.Now,
				CreatedBy = AuthenticationContext.User,
				Enabled = model.Enabled,
				InstanceManagerRights = model.InstanceManagerRights,
				Name = model.Name,
				SystemIdentifier = model.SystemIdentifier
			};

			if (model.SystemIdentifier != null)
				using (var systemIdentity = systemIdentityFactory.CreateSystemIdentity(dbUser))
				{
					if (systemIdentity == null)
						return Forbid();
				}
			else
				cryptographySuite.SetUserPassword(dbUser, model.Password);

			DatabaseContext.Users.Add(dbUser);

			try
			{
				try
				{
					await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					logger.LogInformation("Error creating user: {0}", e);
					throw;
				}
			}
			catch (DbUpdateConcurrencyException)
			{
				return Conflict();
			}

			return Ok();
		}
	}
}
