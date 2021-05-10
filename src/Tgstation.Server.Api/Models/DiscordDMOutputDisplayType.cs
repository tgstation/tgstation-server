namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// When the DM output section of Discord deployment embeds should be shown.
	/// </summary>
	public enum DiscordDMOutputDisplayType
	{
		/// <summary>
		/// Always show.
		/// </summary>
		Always,

		/// <summary>
		/// Only show if DM failed.
		/// </summary>
		OnError,

		/// <summary>
		/// Never show.
		/// </summary>
		Never,
	}
}
