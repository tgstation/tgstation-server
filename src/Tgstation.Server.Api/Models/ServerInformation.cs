using System;
using System.Collections.Generic;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents basic server information.
	/// </summary>
	public sealed class ServerInformation : Internal.ServerInformation
	{
		/// <summary>
		/// The version of the host
		/// </summary>
		public Version? Version { get; set; }

		/// <summary>
		/// The <see cref="Api"/> version of the host
		/// </summary>
		public Version? ApiVersion { get; set; }

		/// <summary>
		/// The DMAPI version of the host.
		/// </summary>
		public Version? DMApiVersion { get; set; }

		/// <summary>
		/// If the server is running on a windows operating system.
		/// </summary>
		public bool WindowsHost { get; set; }

		/// <summary>
		/// A <see cref="ICollection{T}"/> of connected <see cref="SwarmServer"/>s.
		/// </summary>
		public ICollection<SwarmServer>? SwarmServers { get; set; }

		/// <summary>
		/// Map of <see cref="OAuthProvider"/> to the <see cref="OAuthProviderInfo"/> for them.
		/// </summary>
		public IDictionary<OAuthProvider, OAuthProviderInfo>? OAuthProviderInfos { get; set; }
	}
}
