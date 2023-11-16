using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// A request to update <see cref="DreamDaemonSettings"/>.
	/// </summary>
	public sealed class DreamDaemonRequest : DreamDaemonApiBase
	{
		/// <summary>
		/// A <see cref="string"/> to send to the running server's DMAPI for broadcasting. How this is displayed is up to how the DMAPI is integrated in the codebase. Requires interop version >=5.7.0.
		/// </summary>
		public string? BroadcastMessage { get; set; }
	}
}
