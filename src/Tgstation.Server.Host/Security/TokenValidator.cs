using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Tgstation.Server.Api;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	public class TokenValidator : ITokenValidator
	{
		/// <summary>
		/// The <see cref="IAuthenticationContextFactory"/> for the <see cref="TokenValidator"/>.
		/// </summary>
		readonly IAuthenticationContextFactory authenticationContextFactory;

		/// <summary>
		/// The <see cref="ApiHeaders"/> for the <see cref="TokenValidator"/>.
		/// </summary>
		readonly ApiHeaders? apiHeaders;

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenValidator"/> class.
		/// </summary>
		/// <param name="authenticationContextFactory">The value of <see cref="authenticationContextFactory"/>.</param>
		/// <param name="apiHeadersProvider">The <see cref="IApiHeadersProvider"/> containing the value of <see cref="apiHeaders"/>.</param>
		public TokenValidator(IAuthenticationContextFactory authenticationContextFactory, IApiHeadersProvider apiHeadersProvider)
		{
			this.authenticationContextFactory = authenticationContextFactory ?? throw new ArgumentNullException(nameof(authenticationContextFactory));
			ArgumentNullException.ThrowIfNull(apiHeadersProvider);
			apiHeaders = apiHeadersProvider.ApiHeaders;
		}

		/// <inheritdoc />
		public async Task ValidateToken(TokenValidatedContext tokenValidatedContext, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(tokenValidatedContext);

			if (tokenValidatedContext.SecurityToken is not JsonWebToken jwt)
				throw new ArgumentException($"Expected {nameof(tokenValidatedContext)} to contain a {nameof(JsonWebToken)}!", nameof(tokenValidatedContext));

			var principal = new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims));

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
				cancellationToken);

			if (!authenticationContext.Valid)
				tokenValidatedContext.Fail("Authentication context could not be created!");
		}
	}
}
