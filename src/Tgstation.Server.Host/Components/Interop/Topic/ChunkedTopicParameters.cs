using System.Collections.Generic;

#nullable disable

namespace Tgstation.Server.Host.Components.Interop.Topic
{
	/// <summary>
	/// <see cref="TopicParameters"/> for <see cref="TopicCommandType.ReceiveChunk"/>.
	/// </summary>
	sealed class ChunkedTopicParameters : TopicParameters, IMissingPayloadsCommunication, IChunkPayloadId
	{
		/// <inheritdoc />
		public IReadOnlyCollection<uint> MissingChunks { get; set; }

		/// <inheritdoc />
		public uint? PayloadId { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChunkedTopicParameters"/> class.
		/// </summary>
		public ChunkedTopicParameters()
			: base(TopicCommandType.ReceiveChunk)
		{
		}
	}
}
