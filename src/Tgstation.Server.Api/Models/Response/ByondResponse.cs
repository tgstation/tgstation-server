using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents an installed <see cref="EngineVersion"/>.
	/// </summary>
	public sealed class ByondResponse
	{
		/// <summary>
		/// The represented <see cref="EngineVersion"/>.
		/// </summary>
		public EngineVersion? Version { get; set; }
	}
}
