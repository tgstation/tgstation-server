using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// A <see cref="IClaimsTransformation"/> that maps <see cref="Claim"/>s using an <see cref="IAuthenticationContext"/>.
	/// </summary>
	sealed class AuthenticationContextClaimsTransformation : IClaimsTransformation
	{
		/// <summary>
		/// The <see cref="IAuthenticationContextFactory"/> for the <see cref="AuthenticationContextClaimsTransformation"/>.
		/// </summary>
		readonly IAuthenticationContextFactory authenticationContextFactory;

		/// <summary>
		/// The <see cref="ApiHeaders"/> for the <see cref="AuthenticationContextClaimsTransformation"/>.
		/// </summary>
		readonly ApiHeaders apiHeaders;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationContextClaimsTransformation"/> class.
		/// </summary>
		/// <param name="authenticationContextFactory">The value of <see cref="authenticationContextFactory"/>.</param>
		/// <param name="apiHeadersProvider">The <see cref="IApiHeadersProvider"/> containing the value of <see cref="apiHeaders"/>.</param>
		public AuthenticationContextClaimsTransformation(IAuthenticationContextFactory authenticationContextFactory, IApiHeadersProvider apiHeadersProvider)
		{
			this.authenticationContextFactory = authenticationContextFactory ?? throw new ArgumentNullException(nameof(authenticationContextFactory));
			ArgumentNullException.ThrowIfNull(apiHeadersProvider);
			apiHeaders = apiHeadersProvider.ApiHeaders;
		}

		/// <inheritdoc />
		public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
		{
			ArgumentNullException.ThrowIfNull(principal);

			var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
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

			var authenticationContext = await authenticationContextFactory.CreateAuthenticationContext(
				userId,
				apiHeaders?.InstanceId,
				CancellationToken.None); // DCT: None available

			if (authenticationContext.Valid)
			{
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
							claims.Add(
								new Claim(
									ClaimTypes.Role,
									RightsHelper.RoleName(rightType, enumeratedRight)));
				}

				principal.AddIdentity(new ClaimsIdentity(claims));
			}

			return principal;
		}
	}
}
