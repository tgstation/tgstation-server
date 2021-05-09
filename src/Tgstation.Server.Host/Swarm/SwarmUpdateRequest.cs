using System;
using System.ComponentModel.DataAnnotations;

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
		public Version UpdateVersion { get; set; }
	}
}
