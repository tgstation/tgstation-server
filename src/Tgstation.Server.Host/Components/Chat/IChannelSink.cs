using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Chat
{
	interface IChannelSink
	{
		Task UpdateChannels(IEnumerable<ChannelRepresentation> newChannels, CancellationToken cancellationToken);
	}
}
