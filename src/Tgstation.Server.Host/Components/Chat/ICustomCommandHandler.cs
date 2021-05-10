using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Handles <see cref="Commands.ICommand"/>s that map to those defined in a <see cref="IChatTrackingContext"/>.
	/// </summary>
	public interface ICustomCommandHandler
	{
		/// <summary>
		/// Handle a chat command.
		/// </summary>
		/// <param name="commandName">The command name.</param>
		/// <param name="arguments">Everything typed after <paramref name="commandName"/> minus leading spaces.</param>
		/// <param name="sender">The sending <see cref="ChatUser"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the response text to send back.</returns>
		Task<string> HandleChatCommand(string commandName, string arguments, ChatUser sender, CancellationToken cancellationToken);
	}
}
