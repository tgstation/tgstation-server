using System;
using System.Globalization;

using Newtonsoft.Json;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a <see cref="Providers.IProvider"/> channel.
	/// </summary>
	/// <remarks>This is referred to as a "ChatChannel" in the DMAPI.</remarks>
	public sealed class ChannelRepresentation
	{
		/// <summary>
		/// Backing field for <see cref="RealId"/>. Represented as a <see cref="string"/> to avoid BYOND percision loss.
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// The <see cref="Providers.IProvider"/> channel Id.
		/// </summary>
		/// <remarks><see cref="ChatManager"/> remaps this to an internal id using <see cref="ChannelMapping"/>. Not sent over the DMAPI.</remarks>
		[JsonIgnore]
		public ulong RealId
		{
			get => UInt64.Parse(Id, CultureInfo.InvariantCulture);
			set => Id = value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// The user friendly name of the <see cref="ChannelRepresentation"/>.
		/// </summary>
		public string FriendlyName { get; }

		/// <summary>
		/// The name of the connection the <see cref="ChannelRepresentation"/> belongs to.
		/// </summary>
		public string ConnectionName { get; }

		/// <summary>
		/// If this is considered a channel for admin commands.
		/// </summary>
		public bool IsAdminChannel { get; set; }

		/// <summary>
		/// If this is a 1-to-1 chat channel.
		/// </summary>
		public bool IsPrivateChannel { get; init; }

		/// <summary>
		/// For user use.
		/// </summary>
		public string? Tag { get; set; }

		/// <summary>
		/// If this channel supports embeds.
		/// </summary>
		public bool EmbedsSupported { get; init; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelRepresentation"/> class.
		/// </summary>
		/// <param name="connectionName">The value of <see cref="ConnectionName"/>.</param>
		/// <param name="friendlyName">The value of <see cref="FriendlyName"/>.</param>
		/// <param name="id">The value of <see cref="RealId"/>/<see cref="Id"/>.</param>
		public ChannelRepresentation(string connectionName, string friendlyName, ulong id)
		{
			ConnectionName = connectionName ?? throw new ArgumentNullException(nameof(connectionName));
			FriendlyName = friendlyName ?? throw new ArgumentNullException(nameof(friendlyName));
			Id = null!;
			RealId = id;
		}
	}
}
