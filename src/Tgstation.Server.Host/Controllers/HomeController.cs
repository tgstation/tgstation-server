using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
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
		/// The <see cref="IPasswordHasher"/> for the <see cref="HomeController"/>
		/// </summary>
		readonly IPasswordHasher<User> passwordHasher;

		public HomeController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, ITokenFactory tokenFactory, ISystemIdentityFactory systemIdentityFactory, IPasswordHasher<User> passwordHasher) : base(databaseContext, authenticationContextFactory)
		{
			this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
		}

		[Authorize]
		[HttpGet]
		public JsonResult Home() => Json(Assembly.GetExecutingAssembly().GetName().Version);

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

			if(user.PasswordHash != null)
			{
				var hashResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, ApiHeaders.Password);
				switch (hashResult)
				{
					case PasswordVerificationResult.Failed:
						return Unauthorized();
					case PasswordVerificationResult.SuccessRehashNeeded:
						user.PasswordHash = passwordHasher.HashPassword(user, ApiHeaders.Password);
						await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
						break;
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
