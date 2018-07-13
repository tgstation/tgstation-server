using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class ChatSettings : Internal.ChatSettings
	{
		/// <summary>
		/// Channels the Discord bot should listen/announce in
		/// </summary>
		[Permissions(WriteRight = ChatSettingsRights.WriteChannels)]
		public List<ChatChannel> Channels { get; set; }
	}
}
