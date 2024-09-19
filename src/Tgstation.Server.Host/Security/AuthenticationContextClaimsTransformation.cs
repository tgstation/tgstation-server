using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;

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
		readonly ApiHeaders? apiHeaders;

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
				throw new InvalidOperationException($"Missing '{JwtRegisteredClaimNames.Sub}' claim!");

			long userId;
			try
			{
				userId = Int64.Parse(userIdClaim.Value, CultureInfo.InvariantCulture);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("Failed to parse user ID!", e);
			}

			var nbfClaim = principal.FindFirst(JwtRegisteredClaimNames.Nbf);
			if (nbfClaim == default)
				throw new InvalidOperationException($"Missing '{JwtRegisteredClaimNames.Nbf}' claim!");

			DateTimeOffset nbf;
			try
			{
				nbf = new DateTimeOffset(
					EpochTime.DateTime(
						Int64.Parse(nbfClaim.Value, CultureInfo.InvariantCulture)));
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Failed to parse nbf!", ex);
			}

			var authenticationContext = await authenticationContextFactory.CreateAuthenticationContext(
				userId,
				apiHeaders?.InstanceId,
				nbf,
				CancellationToken.None); // DCT: None available

			var enumerator = Enum.GetValues(typeof(RightsType));
			var claims = new List<Claim>();

			if (authenticationContext.Valid)
			{
				claims.Add(
					new(
						ClaimTypes.Role,
						TgsGraphQLAuthorizeAttribute.CoreAccessRole));
			}

			foreach (RightsType rightType in enumerator)
			{
				// if there's a bad condition, do a weird thing and add all the roles
				// we need it so we can get to TgsAuthorizeAttribute where we can properly decide between BadRequest and Forbid
				var rightAsULong = !authenticationContext.Valid
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

			return principal;
		}
	}
}
