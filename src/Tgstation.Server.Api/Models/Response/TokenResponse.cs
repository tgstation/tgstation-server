using System;

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
		/// When the <see cref="TokenResponse"/> expires.
		/// </summary>
		public DateTimeOffset ExpiresAt { get; set; }
	}
}
