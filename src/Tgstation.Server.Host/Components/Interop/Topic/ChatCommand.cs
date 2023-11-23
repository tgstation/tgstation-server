using System;

using Tgstation.Server.Host.Components.Chat;

#nullable disable

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	/// <summary>
	/// Represents a chat command to be handled by DD.
	/// </summary>
	sealed class ChatCommand
	{
		/// <summary>
		/// The command name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The command params.
		/// </summary>
		public string Params { get; }

		/// <summary>
		/// The <see cref="ChatUser"/> that sent the command.
		/// </summary>
		public ChatUser User { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatCommand"/> class.
		/// </summary>
		/// <param name="user">The value of <see cref="User"/>.</param>
		/// <param name="command">The value of <see cref="Name"/>.</param>
		/// <param name="parameters">The value of <see cref="Params"/>.</param>
		public ChatCommand(ChatUser user, string command, string parameters)
		{
			User = user ?? throw new ArgumentNullException(nameof(user));
			Name = command ?? throw new ArgumentNullException(nameof(command));
			Params = parameters ?? throw new ArgumentNullException(nameof(parameters));
		}
	}
}
