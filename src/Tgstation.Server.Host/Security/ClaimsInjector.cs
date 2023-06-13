using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class ClaimsInjector : IClaimsInjector
	{
		/// <summary>
		/// The <see cref="IAuthenticationContextFactory"/> for the <see cref="ClaimsInjector"/>.
		/// </summary>
		readonly IAuthenticationContextFactory authenticationContextFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="ClaimsInjector"/> class.
		/// </summary>
		/// <param name="authenticationContextFactory">The value of <see cref="authenticationContextFactory"/>.</param>
		public ClaimsInjector(IAuthenticationContextFactory authenticationContextFactory)
		{
			this.authenticationContextFactory = authenticationContextFactory ?? throw new ArgumentNullException(nameof(authenticationContextFactory));
		}

		/// <inheritdoc />
		public async Task InjectClaimsIntoContext(TokenValidatedContext tokenValidatedContext, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(tokenValidatedContext);

			// Find the user id in the token
			var userIdClaim = tokenValidatedContext.Principal.FindFirst(JwtRegisteredClaimNames.Sub);
			if (userIdClaim == default)
				throw new InvalidOperationException("Missing required claim!");

			long userId;
			try
			{
				userId = Int64.Parse(userIdClaim.Value, CultureInfo.InvariantCulture);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("Failed to parse user ID!", e);
			}

			ApiHeaders apiHeaders;
			try
			{
				apiHeaders = new ApiHeaders(tokenValidatedContext.HttpContext.Request.GetTypedHeaders());
			}
			catch (HeadersException)
			{
				// we are not responsible for handling header validation issues
				return;
			}

			// This populates the CurrentAuthenticationContext field for use by us and subsequent controllers
			await authenticationContextFactory.CreateAuthenticationContext(
				userId,
				apiHeaders.InstanceId,
				tokenValidatedContext.SecurityToken.ValidFrom,
				cancellationToken);

			var authenticationContext = authenticationContextFactory.CurrentAuthenticationContext;

			var enumerator = Enum.GetValues(typeof(RightsType));
			var claims = new List<Claim>();
			foreach (RightsType rightType in enumerator)
			{
				// if there's no instance user, do a weird thing and add all the instance roles
				// we need it so we can get to OnActionExecutionAsync where we can properly decide between BadRequest and Forbid
				// if user is null that means they got the token with an expired password
				var rightAsULong = authenticationContext.User == null
					|| (RightsHelper.IsInstanceRight(rightType) && authenticationContext.InstancePermissionSet == null)
					? ~0UL
					: authenticationContext.GetRight(rightType);
				var rightEnum = RightsHelper.RightToType(rightType);
				var right = (Enum)Enum.ToObject(rightEnum, rightAsULong);
				foreach (Enum enumeratedRight in Enum.GetValues(rightEnum))
					if (right.HasFlag(enumeratedRight))
						claims.Add(new Claim(ClaimTypes.Role, RightsHelper.RoleName(rightType, enumeratedRight)));
			}

			tokenValidatedContext.Principal.AddIdentity(new ClaimsIdentity(claims));
		}
	}
}
