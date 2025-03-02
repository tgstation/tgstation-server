using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// List of OAuth providers supported by TGS.
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
		[Obsolete("tgstation13.org no longer has a custom OAuth solution", true)]
		TGForums,

		/// <summary>
		/// https://www.keycloak.org.
		/// </summary>
		Keycloak,

		/// <summary>
		/// https://invisioncommunity.com.
		/// </summary>
		InvisionCommunity,
	}
}
