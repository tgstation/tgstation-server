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
		public virtual Uri? Address { get; set; }

		/// <summary>
		/// The address the swarm server can be publically accessed.
		/// </summary>
		public virtual Uri? PublicAddress { get; set; }

		/// <summary>
		/// The server's identifier.
		/// </summary>
		[Required]
		public string? Identifier { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmServer"/> class.
		/// </summary>
		protected SwarmServer()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmServer"/> class.
		/// </summary>
		/// <param name="copy">The <see cref="SwarmServer"/> to copy.</param>
		protected SwarmServer(SwarmServer copy)
		{
			if (copy == null)
			{
				throw new ArgumentNullException(nameof(copy));
			}

			Address = copy.Address;
			PublicAddress = copy.PublicAddress;
			Identifier = copy.Identifier;
		}
	}
}
