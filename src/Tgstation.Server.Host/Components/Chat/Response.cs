using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a chat message requested by DD
	/// </summary>
	sealed class Response
	{
		/// <summary>
		/// The message string
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// The list of internal channel ids to send <see cref="Message"/> to
		/// </summary>
		public List<ulong> ChannelIds { get; set; }
	}
}
