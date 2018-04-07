using System;
using System.Collections.Generic;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents an instance of BYOND's DreamDaemon game server. Create action starts the server. Delete action shuts down the server
	/// </summary>
	[Model(RightsType.DreamDaemon, CanCrud = true, RequiresInstance = true)]
	public sealed class DreamDaemon : DreamDaemonSettings
	{
		/// <summary>
		/// The current status of <see cref="DreamDaemon"/>
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadMetadata)]
		public DreamDaemonStatus? Status { get; set; }

		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadMetadata)]
		public DreamDaemonSecurity? CurrentSecurityLevel { get; set; }

		/// <summary>
		/// The port the running <see cref="DreamDaemon"/> instance is set to
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadMetadata)]
		public ushort? CurrentPort { get; set; }
	}
}
