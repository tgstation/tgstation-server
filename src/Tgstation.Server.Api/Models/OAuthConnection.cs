﻿using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a valid OAuth connection.
	/// </summary>
	public class OAuthConnection
	{
		/// <summary>
		/// The <see cref="OAuthProvider"/> of the <see cref="OAuthConnection"/>.
		/// </summary>]
		[EnumDataType(typeof(OAuthProvider))]
		public OAuthProvider Provider { get; set; }

		/// <summary>
		/// The ID of the user in the <see cref="Provider"/>.
		/// </summary>
		[Required]
		[RequestOptions(FieldPresence.Required)]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? ExternalUserId { get; set; }
	}
}
