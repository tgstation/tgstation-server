using System.Threading.Tasks;

namespace TGS.Server
{
	/// <summary>
	/// Interface for allowing <see cref="TGS.Server.Components"/> to sends chat messages
	/// </summary>
	interface IChatBroadcaster
	{
		/// <summary>
		/// Sends a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message to send</param>
		/// <param name="messageType">The <see cref="MessageType"/> of the <paramref name="message"/></param>
		Task SendMessage(string message, MessageType messageType);
	}
}
