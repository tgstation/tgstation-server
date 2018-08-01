using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// Represents a command that can be invoked by talking to chat bots
	/// </summary>
	public interface ICommand
	{
		/// <summary>
		/// The text to invoke the command. May not be "?" or "help" (case-insensitive)
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The help text to display when queires are made about the command
		/// </summary>
		string HelpText { get; }

		/// <summary>
		/// If the command should only be available to <see cref="User"/>s who's <see cref="User.Channel"/> has <see cref="Channel.IsAdmin"/> set
		/// </summary>
		bool AdminOnly { get; }

		/// <summary>
		/// Invoke the <see cref="ICommand"/>
		/// </summary>
		/// <param name="arguments">The text after <see cref="Name"/> with leading whitespace trimmed</param>
		/// <param name="user">The <see cref="User"/> who invoked the command</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="string"/> to send to the invoker</returns>
		Task<string> Invoke(string arguments, User user, CancellationToken cancellationToken);
	}
}