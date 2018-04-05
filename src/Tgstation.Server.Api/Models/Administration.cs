using System;
using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Metadata about an installation
	/// </summary>
	[Model(RightsType.Administration)]
	public sealed class Administration
	{
		/// <summary>
		/// Use the specified Windows/UNIX authentication to authorize users. Setting this to <see langword="null"/> enables full administrative anonymous access
		/// </summary>
		[Permissions(ReadRight = AdministrationRights.ChangeAuthenticationGroup, WriteRight = AdministrationRights.ChangeAuthenticationGroup)]
		public string SystemAuthenticationGroup { get; set; }

		/// <summary>
		/// Automatically send unhandled exception data to a public collection service. This will be limited to system information, path data, and game code compilation information.
		/// </summary>
		[Permissions(ReadRight = AdministrationRights.ChangeTelemetry, WriteRight = AdministrationRights.ChangeTelemetry)]
		public bool EnableTelemetry { get; set; }

		/// <summary>
		/// If the <see cref="DreamDaemon"/> instances will not be stopped when the server exits. Resets to <see langword="false"/> when the server restarts
		/// </summary>
		[Permissions(ReadRight = AdministrationRights.SoftStop, WriteRight = AdministrationRights.SoftStop)]
		public bool SoftStop { get; set; }

		/// <summary>
		/// The git repository to recieve updates to Tgstation.Server.Host from, must include credentials if necessary. If set to <see langword="null"/> upstream pulls will be disabled entirely
		/// </summary>
		[Permissions(ReadRight = AdministrationRights.SetUpstreamRepository, WriteRight = AdministrationRights.SetUpstreamRepository)]
		public string UpstreamRepository { get; set; }

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
