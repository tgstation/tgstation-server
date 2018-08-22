using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a <see cref="Providers.IProvider"/> channel
	/// </summary>
	public sealed class Channel
	{
		/// <summary>
		/// Backing field for <see cref="RealId"/>. Represented as a <see cref="string"/> to avoid BYOND percision loss
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The <see cref="Providers.IProvider"/> channel Id.
		/// </summary>
		/// <remarks><see cref="Chat"/> remaps this to an internal id using <see cref="ChannelMapping"/></remarks>
		[JsonIgnore]
		public ulong RealId
		{
			get => UInt64.Parse(Id, CultureInfo.InvariantCulture);
			set => Id = value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// The user friendly name of the <see cref="Channel"/>
		/// </summary>
		public string FriendlyName { get; set; }

		/// <summary>
		/// The name of the connection the <see cref="Channel"/> belongs to
		/// </summary>
		public string ConnectionName { get; set; }

		/// <summary>
		/// If this is considered a channel for admin commands
		/// </summary>
		public bool IsAdmin { get; set; }

		/// <summary>
		/// If this is a 1-to-1 chat channel
		/// </summary>
		public bool IsPrivate { get; set; }

		/// <summary>
		/// For user use
		/// </summary>
		public string Tag { get; set; }
	}
}
