namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a tgs_chat_user datum
	/// </summary>
	public sealed class User
	{
		/// <summary>
		/// The internal user id
		/// </summary>
		public ulong Id { get; set; }

		/// <summary>
		/// The friendly name of the user
		/// </summary>
		public string FriendlyName { get; set; }

		/// <summary>
		/// The text to mention the user
		/// </summary>
		public string Mention { get; set; }

		/// <summary>
		/// The <see cref="Components.Chat.Channel"/> the user spoke from
		/// </summary>
		public Channel Channel { get; set; }
	}
}
