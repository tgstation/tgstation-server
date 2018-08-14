using System.Collections.Generic;

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
		/// <param name="initialChatBots">The initial <see cref="Models.ChatBot"/> for the <see cref="IChat"/></param>
		/// <returns>A new <see cref="IChat"/></returns>
		IChat CreateChat(IEnumerable<Models.ChatBot> initialChatBots);
	}
}
