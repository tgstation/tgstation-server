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

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmUpdateRequest"/> class.
		/// </summary>
		/// <param name="updateVersion">The value of <see cref="UpdateVersion"/>.</param>
		public SwarmUpdateRequest(Version updateVersion)
		{
			UpdateVersion = updateVersion ?? throw new ArgumentNullException(nameof(updateVersion));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmUpdateRequest"/> class.
		/// </summary>
		[Obsolete("For JSON deserialization.", true)]
		public SwarmUpdateRequest()
		{
			UpdateVersion = null!;
		}
	}
}
