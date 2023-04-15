namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// A packet of a split serialized set of <see cref="BridgeParameters"/>.
	/// </summary>
	public sealed class ChunkData : ChunkedRequestInfo
	{
		/// <summary>
		/// The sequence ID of the chunk.
		/// </summary>
		public uint SequenceId { get; set; }

		/// <summary>
		/// The partial JSON payload of the chunk.
		/// </summary>
		public string Payload { get; set; }
	}
}
