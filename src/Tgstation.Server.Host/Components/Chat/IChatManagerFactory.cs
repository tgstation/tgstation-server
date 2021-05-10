using System.Collections.Generic;

using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// For creating <see cref="IChatManager"/>s.
	/// </summary>
	interface IChatManagerFactory
	{
		/// <summary>
		/// Create a <see cref="IChatManager"/>.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="IChatManager"/>.</param>
		/// <param name="commandFactory">The <see cref="ICommandFactory"/> for the <see cref="IChatManager"/>.</param>
		/// <param name="initialChatBots">The initial <see cref="Models.ChatBot"/> for the <see cref="IChatManager"/>.</param>
		/// <returns>A new <see cref="IChatManager"/>.</returns>
		IChatManager CreateChatManager(IIOManager ioManager, ICommandFactory commandFactory, IEnumerable<Models.ChatBot> initialChatBots);
	}
}
