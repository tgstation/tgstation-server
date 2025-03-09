using System;
using System.Collections.Generic;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents basic server information.
	/// </summary>
	public sealed class ServerInformationResponse : Internal.ServerInformationBase
	{
		/// <summary>
		/// The version of the host.
		/// </summary>
		/// <example>6.12.3</example>
		public Version? Version { get; set; }

		/// <summary>
		/// The <see cref="Api"/> version of the host.
		/// </summary>
		/// <example>10.12.0</example>
		public Version? ApiVersion { get; set; }

		/// <summary>
		/// The DMAPI interop version the server uses.
		/// </summary>
		/// <example>7.3.0</example>
		public Version? DMApiVersion { get; set; }

		/// <summary>
		/// If the server is running on a windows operating system.
		/// </summary>
		public bool WindowsHost { get; set; }

		/// <summary>
		/// Map of <see cref="OAuthProvider"/> to the <see cref="OAuthProviderInfo"/> for them.
		/// </summary>
		public Dictionary<OAuthProvider, OAuthProviderInfo>? OAuthProviderInfos { get; set; }

		/// <summary>
		/// List of configured <see cref="OidcProviderInfo"/>s.
		/// </summary>
		public List<OidcProviderInfo>? OidcProviderInfos { get; set; }

		/// <summary>
		/// If there is a server update in progress.
		/// </summary>
		/// <example>false</example>
		public bool UpdateInProgress { get; set; }

		/// <summary>
		/// A <see cref="ICollection{T}"/> of connected <see cref="SwarmServerResponse"/>s.
		/// </summary>
		[ResponseOptions]
		public ICollection<SwarmServerResponse>? SwarmServers { get; set; }
	}
}
