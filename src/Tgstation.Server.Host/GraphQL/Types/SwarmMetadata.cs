using System;

using Tgstation.Server.Api;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents information that is constant across all servers in a <see cref="ServerSwarm"/>.
	/// </summary>
	public sealed class SwarmMetadata
	{
		/// <summary>
		/// The version of the host.
		/// </summary>
		public Version Version { get; }

		/// <summary>
		/// The <see cref="Api"/> version of the host.
		/// </summary>
		public Version ApiVersion => ApiHeaders.Version;

		/// <summary>
		/// The DMAPI interop version the server uses.
		/// </summary>
		public Version DMApiVersion => DMApiConstants.InteropVersion;

		/// <summary>
		/// If there is a server update in progress.
		/// </summary>
		public bool UpdateInProgress { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmMetadata"/> class.
		/// </summary>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> used to derive the <see cref="Version"/>.</param>
		/// <param name="updateInProgress">The value of <see cref="UpdateInProgress"/>.</param>
		public SwarmMetadata(IAssemblyInformationProvider assemblyInformationProvider, bool updateInProgress)
		{
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);
			Version = assemblyInformationProvider.Version;
			UpdateInProgress = updateInProgress;
		}
	}
}
