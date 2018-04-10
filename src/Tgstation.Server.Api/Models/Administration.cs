using System;
using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Metadata about an installation
	/// </summary>
	public sealed class Administration : Internal.ServerSettings
	{
		/// <summary>
		/// If the <see cref="DreamDaemon"/> instances will not be stopped when the server exits. Resets to <see langword="false"/> when the server restarts
		/// </summary>
		[Permissions(ReadRight = AdministrationRights.SoftStop, WriteRight = AdministrationRights.SoftStop)]
		public bool SoftStop { get; set; }
		
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

		/// <summary>
		/// Users in the <see cref="SystemAuthenticationGroup"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public IReadOnlyList<User> Users { get; set; }
	}
}
