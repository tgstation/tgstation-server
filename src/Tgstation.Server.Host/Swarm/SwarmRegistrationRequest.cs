using System;
using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models.Internal;

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
		public Version ServerVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmRegistrationRequest"/> class.
		/// </summary>
		/// <param name="serverVersion">The value of <see cref="ServerVersion"/>.</param>
		public SwarmRegistrationRequest(Version serverVersion)
		{
			ServerVersion = serverVersion ?? throw new ArgumentNullException(nameof(serverVersion));
		}
	}
}
