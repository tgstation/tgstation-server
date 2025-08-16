using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class TokenFactory : ITokenFactory
	{
		/// <inheritdoc />
		public TokenValidationParameters ValidationParameters { get; }

		/// <inheritdoc />
		public ReadOnlySpan<byte> SigningKeyBytes
		{
			get => signingKey.Key;
			[MemberNotNull(nameof(signingKey))]
			[MemberNotNull(nameof(tokenHeader))]
			set
			{
				signingKey = new SymmetricSecurityKey(value.ToArray());
				tokenHeader = new JwtHeader(
				new SigningCredentials(
					signingKey,
					SecurityAlgorithms.HmacSha256));
			}
		}

		/// <summary>
		/// The <see cref="IOptions{TOptions}"/> of <see cref="SecurityConfiguration"/> for the <see cref="TokenFactory"/>.
		/// </summary>
		readonly IOptions<SecurityConfiguration> securityConfigurationOptions;

		/// <summary>
		/// The <see cref="JwtSecurityTokenHandler"/> used to generate <see cref="TokenResponse.Bearer"/> <see cref="string"/>s.
		/// </summary>
		readonly JwtSecurityTokenHandler tokenHandler;

		/// <summary>
		/// Backing field for <see cref="SigningKeyBytes"/>.
		/// </summary>
		SymmetricSecurityKey signingKey;

		/// <summary>
		/// The <see cref="JwtHeader"/> for generating tokens.
		/// </summary>
		JwtHeader tokenHeader;

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenFactory"/> class.
		/// </summary>
		/// <param name="cryptographySuite">The <see cref="ICryptographySuite"/> used for generating the <see cref="ValidationParameters"/>.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> used to generate the issuer name.</param>
		/// <param name="securityConfigurationOptions">The value of <see cref="securityConfigurationOptions"/>.</param>
		public TokenFactory(
			ICryptographySuite cryptographySuite,
			IAssemblyInformationProvider assemblyInformationProvider,
			IOptions<SecurityConfiguration> securityConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(cryptographySuite);
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);

			this.securityConfigurationOptions = securityConfigurationOptions ?? throw new ArgumentNullException(nameof(securityConfigurationOptions));

			SigningKeyBytes = string.IsNullOrWhiteSpace(securityConfigurationOptions.Value.CustomTokenSigningKeyBase64)
				? cryptographySuite.GetSecureBytes(securityConfigurationOptions.Value.TokenSigningKeyByteCount)
				: Convert.FromBase64String(securityConfigurationOptions.Value.CustomTokenSigningKeyBase64);

			ValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKeyResolver = (_, _, _, _) => Enumerable.Repeat(signingKey, 1),

				ValidateIssuer = true,
				ValidIssuer = assemblyInformationProvider.AssemblyName.Name,

				ValidateLifetime = true,
				ValidateAudience = true,
				ValidAudience = typeof(TokenResponse).Assembly.GetName().Name,

				ClockSkew = TimeSpan.FromMinutes(securityConfigurationOptions.Value.TokenClockSkewMinutes),

				RequireSignedTokens = true,

				RequireExpirationTime = true,
			};

			tokenHandler = new JwtSecurityTokenHandler();
		}

		/// <inheritdoc />
		public string CreateToken(User user, bool serviceLogin)
		{
			ArgumentNullException.ThrowIfNull(user);

			var uid = user.Require(x => x.Id);
			var now = DateTimeOffset.UtcNow;
			var nowUnix = now.ToUnixTimeSeconds();

			// this prevents validation conflicts down the line
			// tldr we can (theoretically) receive a token the same second after we generate it
			// since unix time rounds down, it looks like it came from before the user changed their password
			// this happens occasionally in unit tests
			// just delay a second so we can force a round up
			var userLastPassworUpdateUnix = user.LastPasswordUpdate?.ToUnixTimeSeconds();
			DateTimeOffset notBefore;
			if (nowUnix == userLastPassworUpdateUnix)
				notBefore = now.AddSeconds(1);
			else
				notBefore = now;

			var expiry = now.AddMinutes(serviceLogin
				? securityConfigurationOptions.Value.OAuthTokenExpiryMinutes
				: securityConfigurationOptions.Value.TokenExpiryMinutes);

			var securityToken = new JwtSecurityToken(
				tokenHeader,
				new JwtPayload(
					ValidationParameters.ValidIssuer,
					ValidationParameters.ValidAudience,
					Enumerable.Empty<Claim>(),
					new Dictionary<string, object>
					{
						{ JwtRegisteredClaimNames.Sub, uid.ToString(CultureInfo.InvariantCulture) },
					},
					notBefore.UtcDateTime,
					expiry.UtcDateTime,
					now.UtcDateTime));

			var tokenResponse = tokenHandler.WriteToken(securityToken);

			return tokenResponse;
		}
	}
}
