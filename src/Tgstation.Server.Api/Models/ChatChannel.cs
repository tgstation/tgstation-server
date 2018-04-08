namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Indicates a chat channel
	/// </summary>
	public sealed class ChatChannel
	{
		/// <summary>
		/// The column ID
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The IRC channel name
		/// </summary>
		public string IrcChannel { get; set; }

		/// <summary>
		/// The Discord channel ID
		/// </summary>
		public long DiscordChannelId { get; set; }
	}
}
