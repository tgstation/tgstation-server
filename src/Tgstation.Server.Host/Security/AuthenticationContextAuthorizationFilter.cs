using System;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

#nullable disable

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// An <see cref="IAuthorizationFilter"/> that maps <see cref="Claim"/>s using an <see cref="IAuthenticationContext"/>.
	/// </summary>
	sealed class AuthenticationContextAuthorizationFilter : IAuthorizationFilter
	{
		/// <summary>
		/// The <see cref="IAuthenticationContext"/> for the <see cref="AuthenticationContextAuthorizationFilter"/>.
		/// </summary>
		readonly IAuthenticationContext authenticationContext;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="AuthenticationContextAuthorizationFilter"/>.
		/// </summary>
		readonly ILogger<AuthenticationContextAuthorizationFilter> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationContextAuthorizationFilter"/> class.
		/// </summary>
		/// <param name="authenticationContext">The value of <see cref="authenticationContext"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public AuthenticationContextAuthorizationFilter(IAuthenticationContext authenticationContext, ILogger<AuthenticationContextAuthorizationFilter> logger)
		{
			this.authenticationContext = authenticationContext ?? throw new ArgumentNullException(nameof(authenticationContext));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void OnAuthorization(AuthorizationFilterContext context)
		{
			if (!authenticationContext.Valid)
			{
				logger.LogTrace("authenticationContext is invalid!");
				context.Result = new UnauthorizedResult();
				return;
			}

			if (authenticationContext.User.Enabled.Value)
				return;

			logger.LogTrace("authenticationContext is for a disabled user!");
			context.Result = new ForbidResult();
		}
	}
}
