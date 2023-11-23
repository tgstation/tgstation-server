using System;
using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models.Internal;

#nullable disable

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// A request to register with a swarm controller.
	/// </summary>
	public sealed class SwarmRegistrationRequest : SwarmServer
	{
		/// <summary>
		/// The TGS <see cref="Version"/> of the sending server.
		/// </summary>
		[Required]
		public Version ServerVersion { get; set; }
	}
}
