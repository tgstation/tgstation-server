using System.Collections.Generic;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// For creating <see cref="IChat"/>s
	/// </summary>
	interface IChatFactory
	{
		/// <summary>
		/// Create a <see cref="IChat"/>
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="IChat"/></param>
		/// <param name="commandFactory">The <see cref="ICommandFactory"/> for the <see cref="IChat"/></param>
		/// <param name="initialChatBots">The initial <see cref="Models.ChatBot"/> for the <see cref="IChat"/></param>
		/// <returns>A new <see cref="IChat"/></returns>
		IChat CreateChat(IIOManager ioManager, ICommandFactory commandFactory, IEnumerable<Models.ChatBot> initialChatBots);
	}
}
