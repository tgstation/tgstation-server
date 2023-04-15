namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// Information about a chunked bridge request.
	/// </summary>
	public abstract class ChunkedRequestInfo
	{
		/// <summary>
		/// The ID of the full request to differentiate different chunkings.
		/// </summary>
		public uint PayloadId { get; set; }

		/// <summary>
		/// The total number of chunks in the request.
		/// </summary>
		public uint TotalChunks { get; set; }
	}
}
