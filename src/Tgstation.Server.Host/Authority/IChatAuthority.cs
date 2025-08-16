using System.Collections.Generic;
using System.Threading;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// <see cref="IAuthority"/> for manipulating chat bots.
	/// </summary>
	public interface IChatAuthority : IAuthority
	{
		/// <summary>
		/// Create a new <see cref="ChatBot"/>.
		/// </summary>
		/// <param name="initialChannels">The initial <see cref="ChatChannel"/>s for the chat bot. Must have been previously validated to be compatible with the <paramref name="provider"/>.</param>
		/// <param name="name">The name of the chat bot.</param>
		/// <param name="connectionString">The connection string for the chat bot.</param>
		/// <param name="provider">The <see cref="ChatProvider"/> to use.</param>
		/// <param name="instanceId">The ID of the instance the chat bot belongs to.</param>
		/// <param name="reconnectionInterval">The interval in minutes that TGS attempts to reconnect the chat provider if it disconnects while it is enabled.</param>
		/// <param name="channelLimit">The maximum number of channels that can be created for the chat bot.</param>
		/// <param name="enabled">If the chat bot is enabled.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for operations.</param>
		/// <returns>A <see cref="RequirementsGated{TResult}"/> <see cref="AuthorityResponse{TResult}"/> for the created <see cref="ChatBot"/>.</returns>
		RequirementsGated<AuthorityResponse<ChatBot>> Create(
			IEnumerable<Models.ChatChannel> initialChannels,
			string name,
			string connectionString,
			ChatProvider provider,
			long instanceId,
			uint? reconnectionInterval,
			ushort? channelLimit,
			bool enabled,
			CancellationToken cancellationToken);
	}
}
