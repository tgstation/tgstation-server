using System;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Indicates data from the TGS update source.
	/// </summary>
	public class UpdateInformation
	{
		/// <summary>
		/// The latest available version of the Tgstation.Server.Host assembly from the upstream repository. If <see cref="Version.Major"/> is less than 4 the update cannot be applied due to API changes.
		/// </summary>
		/// <example>1.0.0</example>
		public Version? LatestVersion { get; set; }

		/// <summary>
		/// This response is cached. This field indicates the <see cref="DateTimeOffset"/> the <see cref="UpdateInformation"/> was generated.
		/// </summary>
		public DateTimeOffset? GeneratedAt { get; set; }
	}
}
