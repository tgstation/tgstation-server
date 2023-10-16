using System;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Controllers.Legacy.Models
{
	/// <summary>
	/// A request to delete a specific <see cref="Version"/>.
	/// </summary>
	public class ByondVersionDeleteRequest
	{
		/// <summary>
		/// The BYOND version to delete.
		/// </summary>
		[RequestOptions(FieldPresence.Required)]
		public Version Version { get; set; }
	}
}
