namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// A request to install a BYOND <see cref="ByondVersionDeleteRequest.Version"/>.
	/// </summary>
	public sealed class ByondVersionRequest : ByondVersionDeleteRequest
	{
		/// <summary>
		/// If a custom BYOND version is to be uploaded.
		/// </summary>
		public bool? UploadCustomZip { get; set; }
	}
}
