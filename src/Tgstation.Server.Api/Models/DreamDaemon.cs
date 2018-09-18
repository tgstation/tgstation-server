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
		public CompileJob ActiveCompileJob { get; set; }

		/// <summary>
		/// The next revision to go live
		/// </summary>
		public CompileJob StagedCompileJob { get; set; }

		/// <summary>
		/// The current status of <see cref="DreamDaemon"/>
		/// </summary>
		public bool? Running { get; set; }

		/// <summary>
		/// The current <see cref="DreamDaemonSecurity"/> of <see cref="DreamDaemon"/>. May be downgraded due to requirements of <see cref="ActiveCompileJob"/>
		/// </summary>
		public DreamDaemonSecurity? CurrentSecurity { get; set; }

		/// <summary>
		/// The port the running <see cref="DreamDaemon"/> instance is set to
		/// </summary>
		public ushort? CurrentPort { get; set; }

		/// <summary>
		/// The webclient status the running <see cref="DreamDaemon"/> instance is set to
		/// </summary>
		public bool? CurrentAllowWebclient { get; set; }
	}
}
