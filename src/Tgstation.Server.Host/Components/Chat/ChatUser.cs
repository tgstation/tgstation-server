using System;
using System.Globalization;

using Newtonsoft.Json;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a tgs_chat_user datum.
	/// </summary>
	public sealed class ChatUser
	{
		/// <summary>
		/// Backing field for <see cref="RealId"/>. Represented as a <see cref="string"/> to avoid BYOND percision loss.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The internal user id.
		/// </summary>
		[JsonIgnore]
		public ulong RealId
		{
			get => UInt64.Parse(Id, CultureInfo.InvariantCulture);
			set => Id = value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// The friendly name of the user.
		/// </summary>
		public string FriendlyName { get; set; }

		/// <summary>
		/// The text to mention the user.
		/// </summary>
		public string Mention { get; set; }

		/// <summary>
		/// The <see cref="ChannelRepresentation"/> the user spoke from.
		/// </summary>
		public ChannelRepresentation Channel { get; set; }
	}
}
