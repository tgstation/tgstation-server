using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Interop;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// Represents a command that can be invoked by talking to chat bots.
	/// </summary>
	public interface ICommand
	{
		/// <summary>
		/// The text to invoke the command. May not be "?" or "help" (case-insensitive).
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The help text to display when queires are made about the command.
		/// </summary>
		string HelpText { get; }

		/// <summary>
		/// If the command should only be available to <see cref="ChatUser"/>s who's <see cref="ChatUser.Channel"/> has <see cref="ChannelRepresentation.IsAdminChannel"/> set.
		/// </summary>
		bool AdminOnly { get; }

		/// <summary>
		/// Invoke the <see cref="ICommand"/>.
		/// </summary>
		/// <param name="arguments">The text after <see cref="Name"/> with leading whitespace trimmed.</param>
		/// <param name="user">The <see cref="ChatUser"/> who invoked the command.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="MessageContent"/> to send to the invoker.</returns>
		ValueTask<MessageContent> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken);
	}
}
