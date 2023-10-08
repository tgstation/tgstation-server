using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class TokenFactory : ITokenFactory
	{
		/// <inheritdoc />
		public TokenValidationParameters ValidationParameters { get; }

		/// <summary>
		/// The <see cref="SecurityConfiguration"/> for the <see cref="TokenFactory"/>.
		/// </summary>
		readonly SecurityConfiguration securityConfiguration;

		/// <summary>
		/// The <see cref="JwtRegisteredClaimNames.Iss"/> claim.
		/// </summary>
		readonly Claim issuerClaim;

		/// <summary>
		/// The <see cref="JwtRegisteredClaimNames.Aud"/> claim.
		/// </summary>
		readonly Claim audienceClaim;

		/// <summary>
		/// The <see cref="JwtHeader"/> for generating tokens.
		/// </summary>
		readonly JwtHeader tokenHeader;

		/// <summary>
		/// The <see cref="JwtSecurityTokenHandler"/> used to generate <see cref="TokenResponse.Bearer"/> <see cref="string"/>s.
		/// </summary>
		readonly JwtSecurityTokenHandler tokenHandler;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="TokenFactory"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenFactory"/> class.
		/// </summary>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="cryptographySuite">The <see cref="ICryptographySuite"/> used for generating the <see cref="ValidationParameters"/>.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> used to generate the issuer name.</param>
		/// <param name="securityConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="securityConfiguration"/>.</param>
		public TokenFactory(
			IAsyncDelayer asyncDelayer,
			ICryptographySuite cryptographySuite,
			IAssemblyInformationProvider assemblyInformationProvider,
			IOptions<SecurityConfiguration> securityConfigurationOptions)
		{
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));

			ArgumentNullException.ThrowIfNull(cryptographySuite);
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);

			securityConfiguration = securityConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(securityConfigurationOptions));

			var signingKeyBytes = String.IsNullOrWhiteSpace(securityConfiguration.CustomTokenSigningKeyBase64)
				? cryptographySuite.GetSecureBytes(securityConfiguration.TokenSigningKeyByteCount)
				: Convert.FromBase64String(securityConfiguration.CustomTokenSigningKeyBase64);

			ValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),

				ValidateIssuer = true,
				ValidIssuer = assemblyInformationProvider.AssemblyName.Name,

				ValidateLifetime = true,
				ValidateAudience = true,
				ValidAudience = typeof(TokenResponse).Assembly.GetName().Name,

				ClockSkew = TimeSpan.FromMinutes(securityConfiguration.TokenClockSkewMinutes),

				RequireSignedTokens = true,

				RequireExpirationTime = true,
			};

			issuerClaim = new Claim(JwtRegisteredClaimNames.Iss, ValidationParameters.ValidIssuer);
			audienceClaim = new Claim(JwtRegisteredClaimNames.Aud, ValidationParameters.ValidAudience);
			tokenHeader = new JwtHeader(
				new SigningCredentials(
					ValidationParameters.IssuerSigningKey,
					SecurityAlgorithms.HmacSha256));
			tokenHandler = new JwtSecurityTokenHandler();
		}

		/// <inheritdoc />
		public async ValueTask<TokenResponse> CreateToken(Models.User user, bool oAuth, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);

			var now = DateTimeOffset.UtcNow;
			var nowUnix = now.ToUnixTimeSeconds();

			// this prevents validation conflicts down the line
			// tldr we can (theoretically) send a token the same second we receive it
			// since unix time rounds down, it looks like it came from before the user changed their password
			// this happens occasionally in unit tests
			// just delay a second so we can force a round up
			var userLastPassworUpdateUnix = user.LastPasswordUpdate?.ToUnixTimeSeconds();
			if (nowUnix == userLastPassworUpdateUnix)
				await asyncDelayer.Delay(TimeSpan.FromSeconds(1), cancellationToken);

			var expiry = now.AddMinutes(oAuth
				? securityConfiguration.OAuthTokenExpiryMinutes
				: securityConfiguration.TokenExpiryMinutes);
			var claims = new Claim[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Exp, expiry.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Nbf, nowUnix.ToString(CultureInfo.InvariantCulture)),
				issuerClaim,
				audienceClaim,
			};

			var securityToken = new JwtSecurityToken(
				tokenHeader,
				new JwtPayload(claims));

			var tokenResponse = new TokenResponse
			{
				Bearer = tokenHandler.WriteToken(securityToken),
				ExpiresAt = expiry,
			};

			return tokenResponse;
		}
	}
}
