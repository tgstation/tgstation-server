using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Chat.Commands;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a tracking of dynamic chat json files
	/// </summary>
	public interface IJsonTrackingContext : IDisposable
	{
		Task<IReadOnlyList<CustomCommand>> GetCustomCommands(CancellationToken cancellationToken);
		Task SetChannels(IEnumerable<Channel> channels, CancellationToken cancellationToken);
	}
}