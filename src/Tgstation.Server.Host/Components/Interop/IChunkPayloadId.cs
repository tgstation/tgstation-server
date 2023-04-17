namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents the payload ID of a set of chunked data.
	/// </summary>
	public interface IChunkPayloadId
	{
		/// <summary>
		/// The ID of the full request to differentiate different chunkings.
		/// </summary>
		/// <remarks>Nullable to prevent default value omission.</remarks>
		uint? PayloadId { get; set; }
	}
}
