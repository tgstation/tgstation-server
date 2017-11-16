using System.Threading.Tasks;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	interface IChatManager : ITGChat
	{
		/// <summary>
		/// Sends a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message to send</param>
		/// <param name="messageType">The <see cref="MessageType"/> of the <paramref name="message"/></param>
		Task SendMessage(string message, MessageType messageType);
	}
}
