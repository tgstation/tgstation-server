using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

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
		[JsonConverter(typeof(StringEnumConverter))]
		[EnumDataType(typeof(OAuthProvider))]
		public OAuthProvider Provider { get; set; }

		/// <summary>
		/// The ID of the user in the <see cref="Provider"/>.
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumIndexableStringLength)]
		public string? ExternalUserId { get; set; }
	}
}
