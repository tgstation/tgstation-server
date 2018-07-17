namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// Represents a command that can be invoked by talking to chat bots
	/// </summary>
	public abstract class Command
	{
		/// <summary>
		/// The text to invoke the command. May not be "?" or "help" (case-insensitive)
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The help text to display when queires are made about the command
		/// </summary>
		public string HelpText { get; set; }

		/// <summary>
		/// If the command should only be available to <see cref="User"/>s who's <see cref="User.Channel"/> has <see cref="Channel.IsAdmin"/> set
		/// </summary>
		public bool AdminOnly { get; set; }

		/// <summary>
		/// Invoke the <see cref="Command"/>
		/// </summary>
		/// <param name="arguments">The text after <see cref="Name"/> with leading whitespace trimmed</param>
		public abstract void Invoke(string arguments);
	}
}
