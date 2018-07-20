using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// For managing <see cref="User"/>s
	/// </summary>
	[Route("/" + nameof(Models.User))]
	public sealed class UsersController : ModelController<Api.Models.User>
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="UsersController"/>
		/// </summary>
		readonly ILogger<UsersController> logger;

		/// <summary>
		/// Construct a <see cref="UsersController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="logger"></param>
		public UsersController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, ILogger<UsersController> logger) : base(databaseContext, authenticationContextFactory)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}
	}
}
