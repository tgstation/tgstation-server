using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Indicates a chat channel
	/// </summary>
	public class ChatChannel
	{
		/// <summary>
		/// The IRC channel name
		/// </summary>
		[StringLength(Limits.MaximumStringLength)]
		public string IrcChannel { get; set; }

		/// <summary>
		/// The Discord channel ID
		/// </summary>
		public ulong? DiscordChannelId { get; set; }

		/// <summary>
		/// If the <see cref="ChatChannel"/> is an admin channel
		/// </summary>
		[Required]
		public bool? IsAdminChannel { get; set; }

		/// <summary>
		/// If the <see cref="ChatChannel"/> is a watchdog channel
		/// </summary>
		[Required]
		public bool? IsWatchdogChannel { get; set; }

		/// <summary>
		/// If the <see cref="ChatChannel"/> is an updates channel
		/// </summary>
		[Required]
		public bool? IsUpdatesChannel { get; set; }

		/// <summary>
		/// A custom tag users can define to group channels together
		/// </summary>
		[StringLength(Limits.MaximumStringLength)]
		public string Tag { get; set; }
	}
}
