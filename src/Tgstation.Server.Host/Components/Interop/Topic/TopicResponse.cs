using System.Collections.Generic;

using Tgstation.Server.Host.Components.Chat.Commands;

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	/// <summary>
	/// A response to a topic request.
	/// </summary>
	sealed class TopicResponse : DMApiResponse, IMissingPayloadsCommunication
	{
		/// <summary>
		/// The text to reply with as the result of a <see cref="TopicCommandType.ChatCommand"/> request, if any. Deprecated circa Interop 5.4.0.
		/// </summary>
		public string? CommandResponseMessage { get; set; }

		/// <summary>
		/// The <see cref="ChatMessage"/> response from a <see cref="ChatCommand"/>. Added in Interop 5.4.0.
		/// </summary>
		public ChatMessage? CommandResponse { get; set; }

		/// <summary>
		/// The <see cref="ChatMessage"/>s to send as the result of a <see cref="TopicCommandType.EventNotification"/> request, if any.
		/// </summary>
		public ICollection<ChatMessage>? ChatResponses { get; set; }

		/// <summary>
		/// The DMAPI <see cref="CustomCommand"/>s for <see cref="TopicCommandType.ServerRestarted"/> requests.
		/// </summary>
		public ICollection<CustomCommand>? CustomCommands { get; set; }

		/// <summary>
		/// The <see cref="ChunkData"/> for a partial response.
		/// </summary>
		public ChunkData? Chunk { get; set; }

		/// <inheritdoc />
		public IReadOnlyCollection<uint>? MissingChunks { get; set; }

		/// <summary>
		/// The number of connected clients to the game. Added in Interop 5.10.0.
		/// </summary>
		public int? ClientCount { get; }
	}
}
