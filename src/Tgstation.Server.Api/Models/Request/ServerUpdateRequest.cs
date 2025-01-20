using System;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// Represents a request to update TGS.
	/// </summary>
	public sealed class ServerUpdateRequest
	{
		/// <summary>
		/// Changes the version of tgstation-server to the given version from the upstream repository.
		/// </summary>
		/// <example>6.12.3</example>
		[RequestOptions(FieldPresence.Required)]
		public Version? NewVersion { get; set; }

		/// <summary>
		/// If the user will provide the server update package .zip file via file transfer as opposed to TGS retrieving it from GitHub.
		/// </summary>
		public bool? UploadZip { get; set; }
	}
}
