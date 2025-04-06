using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// A <see cref="IClaimsTransformation"/> that maps <see cref="Claim"/>s using an <see cref="IAuthenticationContext"/>.
	/// </summary>
	sealed class AuthenticationContextClaimsTransformation : IClaimsTransformation
	{
		/// <summary>
		/// The <see cref="IAuthenticationContext"/> for the <see cref="AuthenticationContextClaimsTransformation"/>.
		/// </summary>
		readonly IAuthenticationContext authenticationContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationContextClaimsTransformation"/> class.
		/// </summary>
		/// <param name="authenticationContext">The value of <see cref="authenticationContext"/>.</param>
		public AuthenticationContextClaimsTransformation(IAuthenticationContext authenticationContext)
		{
			this.authenticationContext = authenticationContext ?? throw new ArgumentNullException(nameof(authenticationContext));
		}

		/// <inheritdoc />
		public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
		{
			ArgumentNullException.ThrowIfNull(principal);

			if (principal.Identity?.AuthenticationType == SwarmConstants.AuthenticationSchemeAndPolicy)
				return Task.FromResult(principal);

			if (!authenticationContext.Valid)
				throw new InvalidOperationException("Expected a valid authentication context here!");

			var enumerator = Enum.GetValues(typeof(RightsType));
			var claims = new List<Claim>();
			if (authenticationContext.User.Require(x => x.Enabled))
				claims.Add(
					new Claim(
						ClaimTypes.Role,
						TgsAuthorizeAttribute.UserEnabledRole));

			foreach (RightsType rightType in enumerator)
			{
				// if there's a bad condition, do a weird thing and add all the roles
				// we need it so we can get to TgsAuthorizeAttribute where we can properly decide between BadRequest and Forbid
				var rightAsULong = (RightsHelper.IsInstanceRight(rightType) && authenticationContext.InstancePermissionSet == null)
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

			return Task.FromResult(principal);
		}
	}
}
