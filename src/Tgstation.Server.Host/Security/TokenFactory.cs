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
		public TokenValidationParameters ValidationParameters { get; private set; }

		/// <inheritdoc />
		public ReadOnlySpan<byte> SigningKey
		{
			get => signingKey;
			set
			{
				signingKey = value.ToArray();
				SetValidationParameters();
			}
		}

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="TokenFactory"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="SecurityConfiguration"/> for the <see cref="TokenFactory"/>.
		/// </summary>
		readonly SecurityConfiguration securityConfiguration;

		/// <summary>
		/// The <see cref="JwtHeader"/> for generating tokens.
		/// </summary>
		readonly JwtHeader tokenHeader;

		/// <summary>
		/// The <see cref="JwtSecurityTokenHandler"/> used to generate <see cref="TokenResponse.Bearer"/> <see cref="string"/>s.
		/// </summary>
		readonly JwtSecurityTokenHandler tokenHandler;

		/// <summary>
		/// Backing field for <see cref="SigningKey"/>.
		/// </summary>
		byte[] signingKey;

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenFactory"/> class.
		/// </summary>
		/// <param name="cryptographySuite">The <see cref="ICryptographySuite"/> used for generating the <see cref="ValidationParameters"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="securityConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="securityConfiguration"/>.</param>
		public TokenFactory(
			ICryptographySuite cryptographySuite,
			IAssemblyInformationProvider assemblyInformationProvider,
			IOptions<SecurityConfiguration> securityConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(cryptographySuite);

			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			securityConfiguration = securityConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(securityConfigurationOptions));

			signingKey = String.IsNullOrWhiteSpace(securityConfiguration.CustomTokenSigningKeyBase64)
				? cryptographySuite.GetSecureBytes(securityConfiguration.TokenSigningKeyByteCount)
				: Convert.FromBase64String(securityConfiguration.CustomTokenSigningKeyBase64);

			SetValidationParameters();

			tokenHeader = new JwtHeader(
				new SigningCredentials(
					ValidationParameters.IssuerSigningKey,
					SecurityAlgorithms.HmacSha256));
			tokenHandler = new JwtSecurityTokenHandler();
		}

		/// <inheritdoc />
		public TokenResponse CreateToken(User user, bool oAuth)
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

			var expiry = now.AddMinutes(oAuth
				? securityConfiguration.OAuthTokenExpiryMinutes
				: securityConfiguration.TokenExpiryMinutes);

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

			var tokenResponse = new TokenResponse
			{
				Bearer = tokenHandler.WriteToken(securityToken),
			};

			return tokenResponse;
		}

		/// <summary>
		/// Initializes <see cref="ValidationParameters"/> based on fields.
		/// </summary>
		[MemberNotNull(nameof(ValidationParameters))]
		void SetValidationParameters()
			=> ValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(signingKey),

				ValidateIssuer = true,
				ValidIssuer = assemblyInformationProvider.AssemblyName.Name,

				ValidateLifetime = true,
				ValidateAudience = true,
				ValidAudience = typeof(TokenResponse).Assembly.GetName().Name,

				ClockSkew = TimeSpan.FromMinutes(securityConfiguration.TokenClockSkewMinutes),

				RequireSignedTokens = true,

				RequireExpirationTime = true,
			};
	}
}
