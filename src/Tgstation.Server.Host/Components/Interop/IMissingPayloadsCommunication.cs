using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// A communication that can be missing chunk payloads.
	/// </summary>
	interface IMissingPayloadsCommunication
	{
		/// <summary>
		/// The <see cref="ChunkData.SequenceId"/>s missing from a chunked request.
		/// </summary>
		IReadOnlyCollection<uint> MissingChunks { get; set; }
	}
}
