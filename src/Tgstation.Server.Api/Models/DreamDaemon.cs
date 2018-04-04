using System;
using System.Collections.Generic;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents an instance of BYOND's DreamDaemon game server
	/// </summary>
	public sealed class DreamDaemon
	{
		/// <summary>
		/// The pull requests merged on the live game version. Not modifiable
		/// </summary>
		public IReadOnlyDictionary<int, string> PullRequests { get; set; }

		/// <summary>
		/// The git sha the live game version was compiled at. Not modifiable
		/// </summary>
		public string Sha { get; set; }

		/// <summary>
		/// The git sha of the origin branch the live game version was compiled at. Not modifiable
		/// </summary>
		public string OriginSha { get; set; }

		/// <summary>
		/// If <see cref="DreamDaemon"/> starts when it's <see cref="Instance"/> starts
		/// </summary>
		public bool? AutoStart { get; set; }

		/// <summary>
		/// The current status of <see cref="DreamDaemon"/>
		/// </summary>
		public DreamDaemonStatus? Status { get; set; }

		/// <summary>
		/// If the BYOND web client can be used to connect to the game server
		/// </summary>
		public bool? AllowWebClient { get; set; }

		/// <summary>
		/// If the server is undergoing a soft reset. This may be automatically set by changes to other fields
		/// </summary>
		public bool? SoftRestart { get; set; }

		/// <summary>
		/// If the server is undergoing a soft shutdown
		/// </summary>
		public bool? SoftShutdown { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of <see cref="DreamDaemon"/>
		/// </summary>
		public DreamDaemonSecurity? SecurirtyLevel { get; set; }

		/// <summary>
		/// The first port <see cref="DreamDaemon"/> uses. This should be the publically advertised port
		/// </summary>
		public ushort? PrimaryPort { get; set; }

		/// <summary>
		/// The second port <see cref="DreamDaemon"/> uses
		/// </summary>
		public ushort? SecondaryPort { get; set; }

		/// <summary>
		/// The port the running <see cref="DreamDaemon"/> instance is set to. Not modifiable
		/// </summary>
		public bool CurrentPort { get; set; }

        /// <summary>
        /// When the live revision was compiled. Not modifiable
        /// </summary>
        public DateTimeOffset CompiledAt { get; set; }
	}
}
