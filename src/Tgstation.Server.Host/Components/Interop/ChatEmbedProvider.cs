namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents information about a <see cref="ChatEmbed"/> provider.
	/// </summary>
	public class ChatEmbedProvider
	{
		/// <summary>
		/// Gets the name of the provider.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets the URL of the provider.
		/// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
		public string Url { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings
	}
}
