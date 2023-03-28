namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents information about a <see cref="ChatEmbed"/> author.
	/// </summary>
	public sealed class ChatEmbedAuthor
	{
		/// <summary>
		/// Gets the name of the author.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets the Url of the author.
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// Gets the icon URL of the author.
		/// </summary>
		public string IconUrl { get; set; }

		/// <summary>
		/// Gets the proxied icon URL of the thumbnail.
		/// </summary>
		public string ProxyIconUrl { get; set; }
	}
}
