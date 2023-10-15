using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// A request to install a <see cref="EngineVersion"/>.
	/// </summary>
	public sealed class ByondVersionRequest : EngineVersion
	{
		/// <summary>
		/// If a custom BYOND version is to be uploaded.
		/// </summary>
		public bool? UploadCustomZip { get; set; }
	}
}
