namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Information about a chunked bridge request.
	/// </summary>
	public abstract class ChunkSetInfo : IChunkPayloadId
	{
		/// <inheritdoc />
		public uint? PayloadId { get; set; }

		/// <summary>
		/// The total number of chunks in the request.
		/// </summary>
		public uint TotalChunks { get; set; }
	}
}
