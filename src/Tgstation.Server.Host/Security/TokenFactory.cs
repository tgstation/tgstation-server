using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class TokenFactory : ITokenFactory
	{
		/// <summary>
		/// Amount of minutes until generated <see cref="Token"/>s expire
		/// </summary>
		const uint TokenExpiryMinutes = 15;

		/// <summary>
		/// Amount of minutes to skew the clock for <see cref="Token"/> validation
		/// </summary>
		const uint TokenClockSkewMinutes = 1;

		/// <summary>
		/// Amount of bytes to use in the <see cref="TokenValidationParameters.IssuerSigningKey"/>
		/// </summary>
		const uint TokenSigningKeyByteAmount = 256;

		/// <inheritdoc />
		public TokenValidationParameters ValidationParameters { get; }

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
		public TokenFactory(
			IAsyncDelayer asyncDelayer,
			ICryptographySuite cryptographySuite,
			IAssemblyInformationProvider assemblyInformationProvider)
		{
			ValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(cryptographySuite.GetSecureBytes(TokenSigningKeyByteAmount)),

				ValidateIssuer = true,
				ValidIssuer = assemblyInformationProvider.Name.Name,

				ValidateLifetime = true,
				ValidateAudience = true,
				ValidAudience = typeof(Token).Assembly.GetName().Name,

				ClockSkew = TimeSpan.FromMinutes(TokenClockSkewMinutes),

				RequireSignedTokens = true,

				RequireExpirationTime = true
			};

			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
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

			var expiry = now.AddMinutes(TokenExpiryMinutes);
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
