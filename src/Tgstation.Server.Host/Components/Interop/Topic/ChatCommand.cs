using System;
using Tgstation.Server.Host.Components.Chat;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	/// <summary>
	/// Represents a chat command to be handled by DD
	/// </summary>
	sealed class ChatCommand
	{
		/// <summary>
		/// The command name
		/// </summary>
		public string Command { get; }

		/// <summary>
		/// The command params
		/// </summary>
		public string Params { get; }

		/// <summary>
		/// The <see cref="Chat.User"/> that sent the command
		/// </summary>
		public User User { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatCommand"/> <see langword="class"/>.
		/// </summary>
		/// <param name="user">The value of <see cref="User"/>.</param>
		/// <param name="command">The value of <see cref="Command"/>.</param>
		/// <param name="parameters">The value of <see cref="Parames"/>.</param>
		public ChatCommand(User user, string command, string parameters)
		{
			User = user ?? throw new ArgumentNullException(nameof(user));
			Command = command ?? throw new ArgumentNullException(nameof(command));
			Params = parameters ?? throw new ArgumentNullException(nameof(parameters));
		}
	}
}
