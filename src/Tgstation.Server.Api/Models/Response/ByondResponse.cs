using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents an installed <see cref="Internal.EngineVersion"/>.
	/// </summary>
	public sealed class ByondResponse
	{
		/// <summary>
		/// The represented <see cref="Internal.EngineVersion"/>. If <see langword="null"/> that indicates none were found.
		/// </summary>
		public EngineVersion? EngineVersion { get; set; }
	}
}
