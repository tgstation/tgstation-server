namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents information about a thumbnail in a <see cref="ChatEmbed"/>.
	/// </summary>
	public class ChatEmbedMedia
	{
		/// <summary>
		/// Gets the source URL of the media. Only supports http(s) and attachments.
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// Gets the proxied URL of the media.
		/// </summary>
		public string ProxyUrl { get; set; }

		/// <summary>
		/// Gets the width of the media.
		/// </summary>
		public int? Width { get; set; }

		/// <summary>
		/// Gets the height of the media.
		/// </summary>
		public int? Height { get; set; }
	}
}
