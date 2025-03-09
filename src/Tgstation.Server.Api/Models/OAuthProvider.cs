using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// List of OAuth2.0 providers supported by TGS that do not support OIDC.
	/// </summary>
	[JsonConverter(typeof(StringEnumConverter))]
	public enum OAuthProvider
	{
		/// <summary>
		/// https://github.com.
		/// </summary>
		GitHub,

		/// <summary>
		/// https://discord.com.
		/// </summary>
		Discord,

		/// <summary>
		/// Pre-hostening https://tgstation13.org. No longer supported.
		/// </summary>
		[Obsolete("tgstation13.org no longer has a custom OAuth solution. This option will be removed in a future TGS version.", true)]
		TGForums,

		/// <summary>
		/// https://www.keycloak.org.
		/// </summary>
		[Obsolete("This should now be implemented as an OIDC provider. This option will be removed in a future TGS version.")]
		Keycloak,

		/// <summary>
		/// https://invisioncommunity.com.
		/// </summary>
		InvisionCommunity,
	}
}
