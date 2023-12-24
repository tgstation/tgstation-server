using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// A request to update a nodes list of <see cref="SwarmServers"/>.
	/// </summary>
	public sealed class SwarmServersUpdateRequest
	{
		/// <summary>
		/// The <see cref="ICollection{T}"/> of updated <see cref="SwarmServerResponse"/>s.
		/// </summary>
		[Required]
		public ICollection<SwarmServerResponse>? SwarmServers { get; set; }
	}
}
