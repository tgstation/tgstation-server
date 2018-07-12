using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Main <see cref="ApiController"/> for the <see cref="Application"/>
	/// </summary>
	[Route("/")]
	public sealed class HomeController : ApiController
	{
		/// <summary>
		/// The <see cref="ITokenFactory"/> for the <see cref="HomeController"/>
		/// </summary>
		readonly ITokenFactory tokenFactory;
		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="HomeController"/>
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;
		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="HomeController"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;
		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="HomeController"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// Construct a <see cref="HomeController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="tokenFactory">The value of <see cref="tokenFactory"/></param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="application">The value of <see cref="application"/></param>
		public HomeController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, ITokenFactory tokenFactory, ISystemIdentityFactory systemIdentityFactory, ICryptographySuite cryptographySuite, IApplication application) : base(databaseContext, authenticationContextFactory)
		{
			this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
		}

		/// <summary>
		/// Returns the version of the <see cref="Application"/>
		/// </summary>
		/// <returns><see cref="Application.Version"/></returns>
		[TgsAuthorize]
		[HttpGet]
		public JsonResult Home() => Json(application.Version);

		/// <summary>
		/// Attempt to authenticate a <see cref="User"/> using <see cref="ApiController.ApiHeaders"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		[HttpPost]
		public async Task<IActionResult> CreateToken(CancellationToken cancellationToken)
		{
			if (ApiHeaders.IsTokenAuthentication)
				return BadRequest(new { message = "Cannot create a token using another token!" });

			var user = await DatabaseContext.Users.Where(x => x.Name == ApiHeaders.Username).Select(x => new User{
				Id = x.Id,
				PasswordHash = x.PasswordHash,
				SystemIdentifier = x.SystemIdentifier,
				Enabled = x.Enabled
			}).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (user == null)
				return Unauthorized();

			if (user.PasswordHash != null)
			{
				var originalHash = user.PasswordHash;
				if (!cryptographySuite.CheckUserPassword(user, ApiHeaders.Password))
					return Unauthorized();
				if (user.PasswordHash != originalHash)
				{
					var updatedUser = new User
					{
						Id = user.Id,
						PasswordHash = originalHash
					};
					DatabaseContext.Users.Attach(updatedUser);
					updatedUser.PasswordHash = user.PasswordHash;
					await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
				}
			}
			else
				try
				{
					var systemIdentity = systemIdentityFactory.CreateSystemIdentity(ApiHeaders.Username, ApiHeaders.Password);
				}
				catch
				{
					return Unauthorized();
				}

			if (!user.Enabled)
				return Forbid();

			return Json(tokenFactory.CreateToken(user));
		}
	}
}
