using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a request to update TGS.
	/// </summary>
	public sealed class ServerUpdateRequest
	{
		/// <summary>
		/// Changes the version of tgstation-server to the given version from the upstream repository.
		/// </summary>
		[RequestOptions(FieldPresence.Required)]
		public Version? NewVersion { get; set; }
	}
}
