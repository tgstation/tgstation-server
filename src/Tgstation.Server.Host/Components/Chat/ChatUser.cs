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
		public string Id { get; private set; }

		/// <summary>
		/// The internal user id.
		/// </summary>
		[JsonIgnore]
		public ulong RealId
		{
			get => UInt64.Parse(Id!, CultureInfo.InvariantCulture);
			private set => Id = value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// The friendly name of the user.
		/// </summary>
		public string FriendlyName { get; }

		/// <summary>
		/// The text to mention the user.
		/// </summary>
		public string Mention { get; }

		/// <summary>
		/// The <see cref="ChannelRepresentation"/> the user spoke from.
		/// </summary>
		public ChannelRepresentation Channel { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatUser"/> class.
		/// </summary>
		/// <param name="channel">The value of <see cref="Channel"/>.</param>
		/// <param name="friendlyName">The value of <see cref="FriendlyName"/>.</param>
		/// <param name="mention">The value of <see cref="Mention"/>.</param>
		/// <param name="realId">The value of <see cref="RealId"/>.</param>
		public ChatUser(ChannelRepresentation channel, string friendlyName, string mention, ulong realId)
		{
			Channel = channel ?? throw new ArgumentNullException(nameof(channel));
			FriendlyName = friendlyName ?? throw new ArgumentNullException(nameof(friendlyName));
			Mention = mention ?? throw new ArgumentNullException(nameof(mention));

			Id = null!;
			RealId = realId;
		}
	}
}
