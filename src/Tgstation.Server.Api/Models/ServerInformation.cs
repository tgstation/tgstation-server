using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents basic server information
	/// </summary>
	public sealed class ServerInformation
	{
		/// <summary>
		/// The version of the host
		/// </summary>
		public Version Version { get; set; }

		/// <summary>
		/// The <see cref="Api"/> version of the host
		/// </summary>
		public Version ApiVersion { get; set; }
	}
}
