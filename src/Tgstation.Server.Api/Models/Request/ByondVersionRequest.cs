using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// A request to switch to a given <see cref="EngineVersion"/>.
	/// </summary>
	public sealed class ByondVersionRequest
	{
		/// <summary>
		/// The <see cref="Internal.EngineVersion"/> to switch to.
		/// </summary>
		public EngineVersion? EngineVersion { get; set; }

		/// <summary>
		/// If a custom BYOND version is to be uploaded.
		/// </summary>
		public bool? UploadCustomZip { get; set; }
	}
}
