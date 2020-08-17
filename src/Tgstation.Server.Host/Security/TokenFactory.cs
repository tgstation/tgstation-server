using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.System;

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
		/// The <see cref="IAsyncDelayer"/> for the <see cref="TokenFactory"/>
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// Construct a <see cref="TokenFactory"/>
		/// </summary>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/></param>
		/// <param name="cryptographySuite">The <see cref="ICryptographySuite"/> used for generating the <see cref="ValidationParameters"/></param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> used to generate the issuer name.</param>
		/// <param name="securityConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="securityConfiguration"/>.</param>
		public TokenFactory(
			IAsyncDelayer asyncDelayer,
			ICryptographySuite cryptographySuite,
			IAssemblyInformationProvider assemblyInformationProvider,
			IOptions<SecurityConfiguration> securityConfigurationOptions)
		{
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));

			if (cryptographySuite == null)
				throw new ArgumentNullException(nameof(cryptographySuite));
			if (assemblyInformationProvider == null)
				throw new ArgumentNullException(nameof(assemblyInformationProvider));

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
				ValidAudience = typeof(Token).Assembly.GetName().Name,

				ClockSkew = TimeSpan.FromMinutes(securityConfiguration.TokenClockSkewMinutes),

				RequireSignedTokens = true,

				RequireExpirationTime = true
			};
		}

		/// <inheritdoc />
		public async Task<Token> CreateToken(Models.User user, CancellationToken cancellationToken)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			var now = DateTimeOffset.Now;
			var nowUnix = now.ToUnixTimeSeconds();

			// this prevents validation conflicts down the line
			// tldr we can (theoretically) send a token the same second we receive it
			// since unix time rounds down, it looks like it came from before the user changed their password
			// this happens occasionally in unit tests
			// just delay a second so we can force a round up
			var lpuUnix = user.LastPasswordUpdate?.ToUnixTimeSeconds();
			if (nowUnix == lpuUnix)
				await asyncDelayer.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

			var expiry = now.AddMinutes(securityConfiguration.TokenExpiryMinutes);
			var claims = new Claim[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Exp, expiry.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Nbf, nowUnix.ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Iss, ValidationParameters.ValidIssuer),
				new Claim(JwtRegisteredClaimNames.Aud, ValidationParameters.ValidAudience)
			};

			var token = new JwtSecurityToken(new JwtHeader(new SigningCredentials(ValidationParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha256)), new JwtPayload(claims));
			return new Token
			{
				Bearer = new JwtSecurityTokenHandler().WriteToken(token),
				ExpiresAt = expiry
			};
		}
	}
}
