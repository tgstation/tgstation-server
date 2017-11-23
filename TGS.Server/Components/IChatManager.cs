using System;
using System.Threading.Tasks;
using TGS.Interface.Components;
using TGS.Server.Chat;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	interface IChatManager : ITGChat
	{
		/// <summary>
		/// Called when <see cref="LoadServerChatCommands(string)"/> should be called
		/// </summary>
		event EventHandler OnRequireChatCommands; 

		/// <summary>
		/// Called to populate the fields of a <see cref="ChatCommands.CommandInfo"/>
		/// </summary>
		event EventHandler<PopulateCommandInfoEventArgs> OnPopulateCommandInfo;

		/// <summary>
		/// Load <see cref="ChatCommands.ServerChatCommand"/>s from a <paramref name="json"/> string
		/// </summary>
		/// <param name="json">The JSON string to load <see cref="ChatCommands.ServerChatCommand"/>s from</param>
		void LoadServerChatCommands(string json);

		/// <summary>
		/// Resets known <see cref="ChatCommands.ServerChatCommand"/>s
		/// </summary>
		void ResetChatCommands();

		/// <summary>
		/// Attempts to reconnect any disconnected <see cref="ChatProviders.IChatProvider"/>s
		/// </summary>
		void CheckConnectivity();

		/// <summary>
		/// Sends a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message to send</param>
		/// <param name="messageType">The <see cref="MessageType"/> of the <paramref name="message"/></param>
		Task SendMessage(string message, MessageType messageType);
	}
}
