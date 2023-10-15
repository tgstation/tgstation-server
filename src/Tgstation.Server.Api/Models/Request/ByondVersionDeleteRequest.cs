using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// A request to delete a specific <see cref="EngineVersion"/>.
	/// </summary>
	public class ByondVersionDeleteRequest
	{
		/// <summary>
		/// The <see cref="Internal.EngineVersion"/> to delete.
		/// </summary>
		public EngineVersion? EngineVersion { get; set; }
	}
}
