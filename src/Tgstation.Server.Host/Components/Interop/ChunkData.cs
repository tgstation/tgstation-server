namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// A packet of a split serialized set of data.
	/// </summary>
	public sealed class ChunkData : ChunkSetInfo
	{
		/// <summary>
		/// The sequence ID of the chunk.
		/// </summary>
		/// <remarks>Always zero indexed. Nullable to prevent default value omission.</remarks>
		public uint? SequenceId { get; set; }

		/// <summary>
		/// The partial JSON payload of the chunk.
		/// </summary>
		public string? Payload { get; set; }
	}
}
