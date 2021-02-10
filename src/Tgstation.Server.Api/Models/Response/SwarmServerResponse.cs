using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class SwarmServerResponse : SwarmServer
	{
		/// <summary>
		/// If the <see cref="SwarmServerResponse"/> is the controller.
		/// </summary>
		public bool Controller { get; set; }
	}
}
