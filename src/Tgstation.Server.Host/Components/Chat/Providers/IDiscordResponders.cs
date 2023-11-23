using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;

#nullable disable

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// Combined interface for the <see cref="IResponder"/> types used by TGS.
	/// </summary>
	interface IDiscordResponders : IResponder<IMessageCreate>, IResponder<IReady>
	{
	}
}
