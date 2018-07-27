using System;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class Administration
	{
		/// <summary>
		/// The GitHub repository the server is built to recieve updates from
		/// </summary>
		[Permissions(DenyWrite = true)]
		public Uri TrackedRepositoryUrl { get; set; }

		/// <summary>
		/// The latest available version of the Tgstation.Server.Host assembly from the upstream repository. If <see cref="Version.Minor"/> is higher than <see cref="CurrentVersion"/>'s the update cannot be applied due to API changes
		/// </summary>
		[Permissions(DenyWrite = true)]
		public Version LatestVersion { get; set; }

		/// <summary>
		/// Changes the version of Tgstation.Server.Host to the given version from the upstream repository
		/// </summary>
		[Permissions(WriteRight = AdministrationRights.ChangeVersion)]
		public Version CurrentVersion { get; set; }
	}
}
