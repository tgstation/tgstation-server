using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a valid OIDC connection.
	/// </summary>
	public class OidcConnection
	{
		/// <summary>
		/// The <see cref="OidcProviderInfo.SchemeKey"/> of the <see cref="OidcConnection"/>.
		/// </summary>]
		[Required]
		[RequestOptions(FieldPresence.Required)]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? SchemeKey { get; set; }

		/// <summary>
		/// The ID of the user in the OIDC proivder ("sub" claim).
		/// </summary>
		[Required]
		[RequestOptions(FieldPresence.Required)]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? ExternalUserId { get; set; }
	}
}
