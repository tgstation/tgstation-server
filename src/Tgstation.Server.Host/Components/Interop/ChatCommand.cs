using Tgstation.Server.Host.Components.Chat;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents a chat command to be handled by DD
	/// </summary>
	sealed class ChatCommand
	{
		/// <summary>
		/// The command name
		/// </summary>
		public string Command { get; set; }

		/// <summary>
		/// The command params
		/// </summary>
		public string Params { get; set; }

		/// <summary>
		/// The <see cref="Chat.User"/> that sent the command
		/// </summary>
		public User User { get; set; }
	}
}
