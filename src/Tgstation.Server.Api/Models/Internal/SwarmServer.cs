using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Information about a server in the swarm.
	/// </summary>
	public class SwarmServer
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
		/// <example>myserver-us-east</example>
		[Required]
		public string? Identifier { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmServer"/> class.
		/// </summary>
		public SwarmServer()
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

#pragma warning disable CS0618 // Type or member is obsolete
			Address = copy.Address;
#pragma warning restore CS0618 // Type or member is obsolete
			PublicAddress = copy.PublicAddress;
			Identifier = copy.Identifier;
		}
	}
}
