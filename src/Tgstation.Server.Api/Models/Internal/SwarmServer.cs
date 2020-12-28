using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Information about a server in the swarm.
	/// </summary>
	public abstract class SwarmServer
	{
		/// <summary>
		/// The public address of the server.
		/// </summary>
		[Required]
		public Uri? Address { get; set; }

		/// <summary>
		/// The server's identifier.
		/// </summary>
		[Required]
		public string? Identifier { get; set; }
	}
}
