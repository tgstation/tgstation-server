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
		/// <param name="initialChatSettings">The initial <see cref="Models.ChatSettings"/> for the <see cref="IChat"/></param>
		/// <returns>A new <see cref="IChat"/></returns>
		IChat CreateChat(IEnumerable<Models.ChatSettings> initialChatSettings);
	}
}
