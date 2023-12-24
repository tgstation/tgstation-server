namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents information about a <see cref="ChatEmbed"/> author.
	/// </summary>
	public sealed class ChatEmbedAuthor : ChatEmbedProvider
	{
		/// <summary>
		/// Gets the icon URL of the author.
		/// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
		public string? IconUrl { get; set; }

		/// <summary>
		/// Gets the proxied icon URL of the thumbnail.
		/// </summary>
		public string? ProxyIconUrl { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings
	}
}
