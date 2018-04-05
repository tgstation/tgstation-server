using System;
using System.Net;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents an access token for the server. Read action generates a new one. Update action expires all for user
	/// </summary>
	[Model(RightsType.Token, CanList = true)]
	public sealed class Token
	{
		/// <summary>
		/// The id of the <see cref="Token"/>. Not modifiable
		/// </summary>
		public long Id { get; }

		/// <summary>
		/// The user agent that created the <see cref="Token"/>
		/// </summary>
		public string ClientUserAgent { get; set; }

		/// <summary>
		/// The <see cref="IPAddress"/> the <see cref="Token"/> was originally issued to
		/// </summary>
		public IPAddress IssuedTo { get; set; }

		/// <summary>
		/// When the <see cref="Token"/> was originally issued
		/// </summary>
		public DateTimeOffset IssuedAt { get; set; }

		/// <summary>
		/// The token <see cref="string"/>. Not modifiable, only appears once
		/// </summary>
		public string Value { get; set; }
	}
}