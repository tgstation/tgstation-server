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
		/// The channel identifier.
		/// For <see cref="ChatProvider.Irc"/>, it's the IRC channel name and optional password colon separated.
		/// For <see cref="ChatProvider.Discord"/>, it's the stringified Discord channel snowflake.
		/// </summary>
		/// <example>124823852418</example>
		[Required]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? ChannelData { get; set; }
	}
}
