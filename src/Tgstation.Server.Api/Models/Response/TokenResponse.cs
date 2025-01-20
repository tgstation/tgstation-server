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
		/// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmJmIjoxNzM3MDkzNDgwLCJleHAiOjE3MzcwOTQzODAsImlhdCI6MTczNzA5MzQ4MCwiaXNzIjoiVGdzdGF0aW9uLlNlcnZlci5Ib3N0IiwiYXVkIjoiVGdzdGF0aW9uLlNlcnZlci5BcGkifQ.v64KX34_YOpH-HCbwqlx1p8u-MNbb4L6a9qEyXhcNcU</example>
		public string? Bearer { get; set; }

		/// <summary>
		/// Parses the <see cref="Bearer"/> as a <see cref="JsonWebToken"/>.
		/// </summary>
		/// <returns>A new <see cref="JsonWebToken"/> based on <see cref="Bearer"/>.</returns>
		public JsonWebToken ParseJwt() => new(Bearer);
	}
}
