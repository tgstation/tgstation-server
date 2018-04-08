using System;
using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents an access token for the server. Read action generates a new one. Update action expires all for user
	/// </summary>
	[Model(RightsType.Token, CanList = true)]
	public class Token
	{
		/// <summary>
		/// The id of the <see cref="Token"/>. Not modifiable
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The user agent that created the <see cref="Token"/>
		/// </summary>
		[Required]
		public string ClientUserAgent { get; set; }

		[Required, MinLength(4), MaxLength(16)]
		public byte[] IssuedTo { get; set; }

		[Required, MinLength(4), MaxLength(16)]
		public byte[] LastUsedBy { get; set; }

		/// <summary>
		/// When the <see cref="Token"/> was originally issued
		/// </summary>
		[Required]
		public DateTimeOffset IssuedAt { get; set; }

		/// <summary>
		/// When the <see cref="Token"/> was last used
		/// </summary>
		public DateTimeOffset LastUsedAt { get; set; }

		/// <summary>
		/// The token <see cref="string"/>. Not modifiable, only appears once
		/// </summary>
		[Required]
		public string Value { get; set; }
	}
}