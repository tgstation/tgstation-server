using System;
using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Indicates a chat channel.
	/// </summary>
	public class ChatChannel : ChatChannelBase
	{
		/// <summary>
		/// The channel identifier. Supercedes <see cref="IrcChannel"/> and <see cref="DiscordChannelId"/>.
		/// For <see cref="ChatProvider.Irc"/>, it's the IRC channel name and optional password colon separated.
		/// For <see cref="ChatProvider.Discord"/>, it's the stringified Discord channel snowflake.
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? ChannelData { get; set; }

		/// <summary>
		/// The IRC channel name. Also potentially contains the channel passsword (if separated by a colon).
		/// If multiple copies of the same channel with different keys are added to the server, the one that will be used is undefined.
		/// </summary>
		[ResponseOptions]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		[Obsolete($"Use {nameof(ChannelData)}")]
		public string? IrcChannel { get; set; }

		/// <summary>
		/// The Discord channel ID.
		/// </summary>
		[Obsolete($"Use {nameof(ChannelData)}")]
		[ResponseOptions]
		public ulong? DiscordChannelId { get; set; }
	}
}
