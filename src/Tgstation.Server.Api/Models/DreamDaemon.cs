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
		IReadOnlyDictionary<int, string> PullRequests { get; set; }

		/// <summary>
		/// The git sha the live game version was compiled at. Not modifiable
		/// </summary>
		string Sha { get; set; }

		/// <summary>
		/// The git sha of the origin branch the live game version was compiled at. Not modifiable
		/// </summary>
		string OriginSha { get; set; }
		
		/// <summary>
		/// If <see cref="DreamDaemon"/> starts when it's <see cref="Instance"/> starts
		/// </summary>
		bool? AutoStart { get; set; }

		/// <summary>
		/// The current status of <see cref="DreamDaemon"/>
		/// </summary>
		DreamDaemonStatus? Status { get; set; }

		/// <summary>
		/// If the BYOND web client can be used to connect to the game server
		/// </summary>
		bool? AllowWebClient { get; set; }

		/// <summary>
		/// If the server is undergoing a soft reset. This may be automatically set by changes to other fields
		/// </summary>
		bool? SoftRestart { get; set; }

		/// <summary>
		/// If the server is undergoing a soft shutdown
		/// </summary>
		bool? SoftShutdown { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of <see cref="DreamDaemon"/>
		/// </summary>
		DreamDaemonSecurity? SecurirtyLevel { get; set; }

		/// <summary>
		/// The first port <see cref="DreamDaemon"/> uses. This should be the publically advertised port
		/// </summary>
		ushort? PrimaryPort { get; set; }

		/// <summary>
		/// The second port <see cref="DreamDaemon"/> uses
		/// </summary>
		ushort? SecondaryPort { get; set; }

		/// <summary>
		/// The port the running <see cref="DreamDaemon"/> instance is set to. Not modifiable
		/// </summary>
		bool CurrentPort { get; set; }
	}
}
