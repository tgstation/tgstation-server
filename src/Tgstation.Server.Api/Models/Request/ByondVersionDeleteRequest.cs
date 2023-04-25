using System;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// A request to delete a specific <see cref="Version"/>.
	/// </summary>
	public class ByondVersionDeleteRequest
	{
		/// <summary>
		/// The BYOND version to install.
		/// </summary>
		[RequestOptions(FieldPresence.Required)]
		public Version? Version { get; set; }
	}
}
