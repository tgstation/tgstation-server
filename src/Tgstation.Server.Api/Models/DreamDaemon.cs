using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents an instance of BYOND's DreamDaemon game server. Create action starts the server. Delete action shuts down the server
	/// </summary>
	public sealed class DreamDaemon : DreamDaemonSettings
	{
		/// <summary>
		/// The live revision
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadRevision)]
		public CompileJob ActiveCompileJob { get; set; }

		/// <summary>
		/// The next revision to go live
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadRevision)]
		public CompileJob StagedCompileJob { get; set; }

		/// <summary>
		/// The current status of <see cref="DreamDaemon"/>
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadMetadata)]
		public bool? Running { get; set; }

		/// <summary>
		/// The current <see cref="DreamDaemonSecurity"/> of <see cref="DreamDaemon"/>
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadMetadata)]
		public DreamDaemonSecurity? CurrentSecurity { get; set; }

		/// <summary>
		/// The port the running <see cref="DreamDaemon"/> instance is set to
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadMetadata)]
		public ushort? CurrentPort { get; set; }

		/// <summary>
		/// The webclient status the running <see cref="DreamDaemon"/> instance is set to
		/// </summary>
		[Permissions(DenyWrite = true, ReadRight = DreamDaemonRights.ReadMetadata)]
		public bool? CurrentAllowWebclient { get; set; }
	}
}
