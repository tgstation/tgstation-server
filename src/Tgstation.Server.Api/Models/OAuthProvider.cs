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
		/// https://tgstation13.org.
		/// </summary>
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
