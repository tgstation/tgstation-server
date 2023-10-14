using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents an installed <see cref="ByondVersion"/>.
	/// </summary>
	public sealed class ByondResponse
	{
		/// <summary>
		/// The represented <see cref="ByondVersion"/>.
		/// </summary>
		public ByondVersion? Version { get; set; }
	}
}
