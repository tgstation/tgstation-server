namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Represents a <see cref="Providers.IProvider"/> channel
	/// </summary>
    public sealed class Channel
	{
		/// <summary>
		/// The <see cref="Providers.IProvider"/> channel Id.
		/// </summary>
		/// <remarks><see cref="Chat"/> remaps this to an internal id using <see cref="ChannelMapping"/></remarks>
		public long Id { get; set; }

		/// <summary>
		/// The user friendly name of the <see cref="Channel"/>
		/// </summary>
		public string FriendlyName { get; set; }

		/// <summary>
		/// If this is considered a channel for admin commands
		/// </summary>
		public bool IsAdminChannel { get; set; }

		/// <summary>
		/// If this i
		/// </summary>
		public bool IsPrivateChannel { get; set; }
	}
}
