using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents administrative server information
	/// </summary>
	public sealed class Administration
	{
		/// <summary>
		/// If the server is running on a windows operating system
		/// </summary>
		public bool WindowsHost { get; set; }

		/// <summary>
		/// The GitHub repository the server is built to recieve updates from
		/// </summary>
		public Uri? TrackedRepositoryUrl { get; set; }

		/// <summary>
		/// The latest available version of the Tgstation.Server.Host assembly from the upstream repository. If <see cref="Version.Major"/> is higher than <see cref="NewVersion"/>'s the update cannot be applied due to API changes
		/// </summary>
		public Version? LatestVersion { get; set; }

		/// <summary>
		/// Changes the version of Tgstation.Server.Host to the given version from the upstream repository
		/// </summary>
		public Version? NewVersion { get; set; }
	}
}
