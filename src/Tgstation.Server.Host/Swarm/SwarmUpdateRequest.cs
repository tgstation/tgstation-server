using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// A request to update the swarm's TGS version.
	/// </summary>
	public sealed class SwarmUpdateRequest
	{
		/// <summary>
		/// The TGS <see cref="Version"/> to update to.
		/// </summary>
		[Required]
		public Version? UpdateVersion { get; init; }

		/// <summary>
		/// The <see cref="Api.Models.Internal.SwarmServer.Identifier"/> of the node to download the update package from.
		/// </summary>
		[Required]
		public string? SourceNode { get; init; }

		/// <summary>
		/// The map of <see cref="Api.Models.Internal.SwarmServer.Identifier"/>s to <see cref="FileTicketResponse"/>s for retrieving the update package from the initiating server.
		/// </summary>
		public Dictionary<string, FileTicketResponse>? DownloadTickets { get; init; }
	}
}
