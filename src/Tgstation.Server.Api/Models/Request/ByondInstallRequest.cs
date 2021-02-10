using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// A request to install a BYOND <see cref="Version"/>.
	/// </summary>
	public sealed class ByondInstallRequest
	{
		/// <summary>
		/// The BYOND version to install.
		/// </summary>
		[RequestOptions(FieldPresence.Required)]
		public Version? Version { get; set; }

		/// <summary>
		/// If a custom BYOND version is to be uploaded.
		/// </summary>
		public bool? UploadCustomZip { get; set; }
	}
}
