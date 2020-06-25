using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Models.Internal;

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
		public CompileJob? ActiveCompileJob { get; set; }

		/// <summary>
		/// The next revision to go live
		/// </summary>
		public CompileJob? StagedCompileJob { get; set; }

		/// <summary>
		/// The current <see cref="WatchdogStatus"/>.
		/// </summary>
		[EnumDataType(typeof(WatchdogStatus))]
		public WatchdogStatus? Status { get; set; }

		/// <summary>
		/// The current <see cref="DreamDaemonSecurity"/> of <see cref="DreamDaemon"/>. May be downgraded due to requirements of <see cref="ActiveCompileJob"/>
		/// </summary>
		[EnumDataType(typeof(DreamDaemonSecurity))]
		public DreamDaemonSecurity? CurrentSecurity { get; set; }

		/// <summary>
		/// The port the running <see cref="DreamDaemon"/> instance is set to
		/// </summary>
		public ushort? CurrentPort { get; set; }

		/// <summary>
		/// The webclient status the running <see cref="DreamDaemon"/> instance is set to
		/// </summary>
		public bool? CurrentAllowWebclient { get; set; }

		/// <summary>
		/// If the server is undergoing a soft reset. This may be automatically set by changes to other fields
		/// </summary>
		public bool? SoftRestart { get; set; }

		/// <summary>
		/// If the server is undergoing a soft shutdown
		/// </summary>
		public bool? SoftShutdown { get; set; }

		/// <summary>
		/// If a dump of the active DreamDaemon executable should be created.
		/// </summary>
		public bool? CreateDump { get; set; }
	}
}
