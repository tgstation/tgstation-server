namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents information about a <see cref="ChatEmbed"/> provider.
	/// </summary>
	public sealed class ChatEmbedProvider
	{
		/// <summary>
		/// Gets the name of the provider.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets the URL of the provider.
		/// </summary>
		public string Url { get; set; }
	}
}
