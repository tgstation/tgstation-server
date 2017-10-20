using System;
using TGServiceInterface;

namespace TGServerService.ChatProviders
{
	/// <summary>
	/// Callback for the chat provider recieving a message
	/// </summary>
	/// <param name="ChatProvider">The chat provider the message came from</param>
	/// <param name="speaker">The username of the speaker</param>
	/// <param name="channel">The name of the channel</param>
	/// <param name="message">The message text</param>
	/// <param name="isAdmin"><see langword="true"/> if <paramref name="speaker"/> is considered a chat admin, <see langword="false"/> otherwise</param>
	/// <param name="isAdminChannel"><see langword="true"/> if <paramref name="channel"/> is an admin chat channel, <see langword="false"/> otherwise</param>

	delegate void OnChatMessage(ITGChatProvider ChatProvider, string speaker, string channel, string message, bool isAdmin, bool isAdminChannel);
	/// <summary>
	/// Interface for a chat provder service
	/// </summary>
	interface ITGChatProvider : IDisposable
	{
		/// <summary>
		/// Sets info for the provider
		/// </summary>
		/// <param name="info">The info to set</param>
		/// <returns>null on success, error message on failure</returns>
		string SetProviderInfo(ChatSetupInfo info);

		/// <summary>
		/// Gets the info of the provider
		/// </summary>
		/// <returns>The info for the chat provider</returns>
		ChatSetupInfo ProviderInfo();

		/// <summary>
		/// Called with chat message info
		/// </summary>
		event OnChatMessage OnChatMessage;

		/// <summary>
		/// Connects the chat provider if it's enabled
		/// </summary>
		/// <returns>null on success, error message on failure</returns>
		string Connect();
		/// <summary>
		/// Forces a reconnection of the chat provider if it's enabled
		/// </summary>
		/// <returns>null on success, error message on failure</returns>
		string Reconnect();

		/// <summary>
		/// Checks if the chat provider is connected
		/// </summary>
		/// <returns>true if the provider is connected, false otherwise</returns>
		bool Connected();

		/// <summary>
		/// Disconnects the chat provider
		/// </summary>
		void Disconnect();

		/// <summary>
		/// Send a message to a channel
		/// </summary>
		/// <param name="message">The message to send</param>
		/// <param name="channel">The channel to send to</param>
		/// <returns>null on success, error message on failure</returns>
		string SendMessageDirect(string message, string channel);

		/// <summary>
		/// Broadcast a message to appropriate channels based on the message type
		/// </summary>
		/// <param name="msg">The message to send</param>
		/// <param name="mt">The message type</param>
		void SendMessage(string msg, MessageType mt);
	}
}
