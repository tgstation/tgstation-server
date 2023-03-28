namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents a footer in a <see cref="ChatEmbed"/>.
	/// </summary>
	public sealed class ChatEmbedFooter
	{
		/// <summary>
		/// Gets the text of the footer.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Gets the URL of the footer icon. Only supports http(s) and attachments.
		/// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
		public string IconUrl { get; set; }

		/// <summary>
		/// Gets the proxied icon URL.
		/// </summary>
		public string ProxyIconUrl { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings
	}
}
