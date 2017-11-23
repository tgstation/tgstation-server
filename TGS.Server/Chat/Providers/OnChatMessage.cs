namespace TGS.Server.Chat.Providers
{
	/// <summary>
	/// Callback for the chat provider recieving a <paramref name="message"/>
	/// </summary>
	/// <param name="ChatProvider">The chat provider the message came from</param>
	/// <param name="speaker">The username of the speaker</param>
	/// <param name="channel">The name of the channel</param>
	/// <param name="message">The message text</param>
	/// <param name="isAdmin"><see langword="true"/> if <paramref name="speaker"/> is considered a chat admin, <see langword="false"/> otherwise</param>
	/// <param name="isAdminChannel"><see langword="true"/> if <paramref name="channel"/> is an admin chat channel, <see langword="false"/> otherwise</param>

	delegate void OnChatMessage(IProvider ChatProvider, string speaker, string channel, string message, bool isAdmin, bool isAdminChannel);
}
