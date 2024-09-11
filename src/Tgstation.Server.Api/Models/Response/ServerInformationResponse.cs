using System;
using System.Collections.Generic;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents basic server information.
	/// </summary>
	public sealed class ServerInformationResponse : Internal.LocalServerInformation
	{
		/// <summary>
		/// The version of the host.
		/// </summary>
		public Version? Version { get; set; }

		/// <summary>
		/// The <see cref="Api"/> version of the host.
		/// </summary>
		public Version? ApiVersion { get; set; }

		/// <summary>
		/// The DMAPI interop version the server uses.
		/// </summary>
		public Version? DMApiVersion { get; set; }

		/// <summary>
		/// If there is a server update in progress.
		/// </summary>
		public bool UpdateInProgress { get; set; }

		/// <summary>
		/// A <see cref="ICollection{T}"/> of connected <see cref="SwarmServerResponse"/>s.
		/// </summary>
		[ResponseOptions]
		public ICollection<SwarmServerResponse>? SwarmServers { get; set; }
	}
}
