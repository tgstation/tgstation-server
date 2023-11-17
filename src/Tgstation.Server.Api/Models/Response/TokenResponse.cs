using Microsoft.IdentityModel.JsonWebTokens;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents a JWT returned by the API.
	/// </summary>
	public sealed class TokenResponse
	{
		/// <summary>
		/// The value of the JWT.
		/// </summary>
		public string? Bearer { get; set; }

		/// <summary>
		/// Parses the <see cref="Bearer"/> as a <see cref="JsonWebToken"/>.
		/// </summary>
		/// <returns>A new <see cref="JsonWebToken"/> based on <see cref="Bearer"/>.</returns>
		public JsonWebToken ParseJwt() => new (Bearer);
	}
}
