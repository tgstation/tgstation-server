using System;
using System.Collections.Generic;
using System.Linq;

using Tgstation.Server.Host.Components.Chat;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents an update of <see cref="ChannelRepresentation"/>s.
	/// </summary>
	public class ChatUpdate
	{
		/// <summary>
		/// The <see cref="IEnumerable{T}"/> of <see cref="ChannelRepresentation"/>s.
		/// </summary>
		public IEnumerable<ChannelRepresentation> Channels { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatUpdate"/> class.
		/// </summary>
		/// <param name="channels">The <see cref="IEnumerable{T}"/> that forms the value of <see cref="Channels"/>.</param>
		public ChatUpdate(IEnumerable<ChannelRepresentation> channels)
		{
			Channels = channels?.ToList() ?? throw new ArgumentNullException(nameof(channels));
		}
	}
}
