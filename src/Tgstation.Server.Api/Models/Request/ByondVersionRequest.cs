using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// A request to install a <see cref="ByondVersion"/>.
	/// </summary>
	public sealed class ByondVersionRequest : ByondVersion
	{
		/// <summary>
		/// If a custom BYOND version is to be uploaded.
		/// </summary>
		public bool? UploadCustomZip { get; set; }
	}
}
