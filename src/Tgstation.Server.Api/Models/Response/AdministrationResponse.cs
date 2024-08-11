using System;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents administrative server information.
	/// </summary>
	public sealed class AdministrationResponse
	{
		/// <summary>
		/// The GitHub repository the server is built to receive updates from.
		/// </summary>
		public Uri? TrackedRepositoryUrl { get; set; }

		/// <summary>
		/// The latest available version of the Tgstation.Server.Host assembly from the upstream repository. If <see cref="Version.Major"/> is not equal to 4 the update cannot be applied due to API changes.
		/// </summary>
		public Version? LatestVersion { get; set; }

		/// <summary>
		/// This response is cached. This field indicates the <see cref="DateTimeOffset"/> when it was generated.
		/// </summary>
		public DateTimeOffset? GeneratedAt { get; set; }
	}
}
