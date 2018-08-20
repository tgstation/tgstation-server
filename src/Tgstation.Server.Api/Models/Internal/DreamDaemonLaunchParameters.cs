using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Launch settings for DreamDaemon
	/// </summary>
	public class DreamDaemonLaunchParameters
	{
		/// <summary>
		/// If the BYOND web client can be used to connect to the game server
		/// </summary>
		[Required]
		public bool? AllowWebClient { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of <see cref="DreamDaemon"/>
		/// </summary>
		[Required]
		public DreamDaemonSecurity? SecurityLevel { get; set; }

		/// <summary>
		/// The first port <see cref="DreamDaemon"/> uses. This should be the publically advertised port
		/// </summary>
		[Required]
		public ushort? PrimaryPort { get; set; }

		/// <summary>
		/// The second port <see cref="DreamDaemon"/> uses
		/// </summary>
		[Required]
		public ushort? SecondaryPort { get; set; }

		/// <summary>
		/// The DreamDaemon startup timeout in seconds
		/// </summary>
		[Required]
		public uint? StartupTimeout { get; set; }
	}
}