using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents an embed for the chat.
	/// </summary>
	public sealed class ChatEmbed
	{
		/// <summary>
		/// The title of the embed.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// The description of the embed.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// The URL of the embed.
		/// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
		public string Url { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

		/// <summary>
		/// The ISO 8601 timestamp of the embed.
		/// </summary>
		public string Timestamp { get; set; }

		/// <summary>
		/// The colour of the embed in the format hex "#AARRGGBB".
		/// </summary>
		public string Colour { get; set; }

		/// <summary>
		/// The <see cref="ChatEmbedFooter"/>.
		/// </summary>
		public ChatEmbedFooter Footer { get; set; }

		/// <summary>
		/// The <see cref="ChatEmbedMedia"/> for an image.
		/// </summary>
		public ChatEmbedMedia Image { get; set; }

		/// <summary>
		/// The <see cref="ChatEmbedMedia"/> for a thumbnail.
		/// </summary>
		public ChatEmbedMedia Thumbnail { get; set; }

		/// <summary>
		/// The <see cref="ChatEmbedMedia"/> for a video.
		/// </summary>
		public ChatEmbedMedia Video { get; set; }

		/// <summary>
		/// The <see cref="ChatEmbedProvider"/>.
		/// </summary>
		public ChatEmbedProvider Provider { get; set; }

		/// <summary>
		/// The <see cref="ChatEmbedAuthor"/>.
		/// </summary>
		public ChatEmbedAuthor Author { get; set; }

		/// <summary>
		/// The <see cref="ChatEmbedField"/>s.
		/// </summary>
		public ICollection<ChatEmbedField> Fields { get; set; }
	}
}
