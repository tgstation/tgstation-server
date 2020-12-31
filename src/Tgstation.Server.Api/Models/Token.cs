using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a JWT returned by the API
	/// </summary>
	public sealed class Token
	{
		/// <summary>
		/// The value of the JWT
		/// </summary>
		public string? Bearer { get; set; }

		/// <summary>
		/// When the <see cref="Token"/> expires
		/// </summary>
		public DateTimeOffset ExpiresAt { get; set; }
	}
}
