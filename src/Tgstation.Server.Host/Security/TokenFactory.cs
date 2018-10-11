using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
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
		public Token CreateToken(Models.User user)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			var now = DateTimeOffset.Now;
			var expiry = now.AddMinutes(TokenExpiryMinutes);
			var claims = new Claim[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Exp, expiry.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Nbf, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
				new Claim(JwtRegisteredClaimNames.Iss, ValidationParameters.ValidIssuer),
				new Claim(JwtRegisteredClaimNames.Aud, ValidationParameters.ValidAudience)
			};

			var token = new JwtSecurityToken(new JwtHeader(new SigningCredentials(ValidationParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha256)), new JwtPayload(claims));
			return new Token { Bearer = new JwtSecurityTokenHandler().WriteToken(token), ExpiresAt = expiry };
		}
	}
}
