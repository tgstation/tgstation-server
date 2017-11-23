using System;
using System.Threading.Tasks;
using TGS.Interface;

namespace TGS.Server.Chat.Providers
{
	/// <summary>
	/// Interface for a chat provder service
	/// </summary>
	interface IProvider : IDisposable
	{
		/// <summary>
		/// Sets <paramref name="info"/> for the provider
		/// </summary>
		/// <param name="info">The <see cref="ChatSetupInfo"/> to set</param>
		/// <returns>null on success, error message on failure</returns>
		string SetProviderInfo(ChatSetupInfo info);

		/// <summary>
		/// Gets the info of the provider
		/// </summary>
		/// <returns>The <see cref="ChatSetupInfo"/> for the chat provider</returns>
		ChatSetupInfo ProviderInfo();

		/// <summary>
		/// Called with chat message info
		/// </summary>
		event OnChatMessage OnChatMessage;

		/// <summary>
		/// Connects the chat provider if it's enabled
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string Connect();
		/// <summary>
		/// Forces a reconnection of the chat provider if it's enabled
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string Reconnect();

		/// <summary>
		/// Checks if the chat provider is connected
		/// </summary>
		/// <returns><see langword="true"/> if the provider is connected, <see langword="false"/> otherwise</returns>
		bool Connected();

		/// <summary>
		/// Disconnects the chat provider
		/// </summary>
		void Disconnect();

		/// <summary>
		/// Send a <paramref name="message"/> to a <paramref name="channel"/>
		/// </summary>
		/// <param name="message">The message to send</param>
		/// <param name="channel">The channel to send to</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string SendMessageDirect(string message, string channel);

		/// <summary>
		/// Broadcast a <paramref name="message"/> to appropriate channels based on the message type
		/// </summary>
		/// <param name="message">The message to send</param>
		/// <param name="mt">The <see cref="MessageType"/></param>
		Task SendMessage(string message, MessageType mt);
	}
}
