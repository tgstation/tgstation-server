using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class TokenFactory : ITokenFactory
	{
		/// <summary>
		/// Amount of minutes until generated <see cref="Token"/>s expire
		/// </summary>
		const int TokenExpiryMinutes = 15;

		/// <inheritdoc />
		public TokenValidationParameters ValidationParameters { get; }

		/// <summary>
		/// Construct a <see cref="TokenFactory"/>
		/// </summary>
		public TokenFactory()
		{
			ValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(CryptographySuite.GetSecureBytes(256)),

				ValidateIssuer = true,
				ValidIssuer = Assembly.GetExecutingAssembly().GetName().Name,

				ValidateLifetime = true,
				ValidateAudience = true,
				ValidAudience = typeof(Token).Assembly.GetName().Name,

				ClockSkew = TimeSpan.FromMinutes(1),

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
			//this prevents validation conflicts down the line
			var lpuUnix = user.LastPasswordUpdate?.ToUnixTimeSeconds();
			if (nowUnix == lpuUnix)
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

			var expiry = now.AddMinutes(TokenExpiryMinutes);
			var claims = new Claim[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Exp, expiry.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Nbf, nowUnix.ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Iss, ValidationParameters.ValidIssuer),
				new Claim(JwtRegisteredClaimNames.Aud, ValidationParameters.ValidAudience)
			};

			var token = new JwtSecurityToken(new JwtHeader(new SigningCredentials(ValidationParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha256)), new JwtPayload(claims));
			return new Token { Bearer = new JwtSecurityTokenHandler().WriteToken(token), ExpiresAt = expiry };
		}
	}
}
