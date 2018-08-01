using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Launch settings for DreamDaemon
	/// </summary>
	[Model(RightsType.DreamDaemon, CanCrud = true, RequiresInstance = true)]
	public class DreamDaemonLaunchParameters
	{
		/// <summary>
		/// If the BYOND web client can be used to connect to the game server
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetWebClient)]
		[Required]
		public bool? AllowWebClient { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of <see cref="DreamDaemon"/>
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetSecurity)]
		[Required]
		public DreamDaemonSecurity? SecurityLevel { get; set; }

		/// <summary>
		/// The first port <see cref="DreamDaemon"/> uses. This should be the publically advertised port
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetPorts)]
		[Required]
		public ushort? PrimaryPort { get; set; }

		/// <summary>
		/// The second port <see cref="DreamDaemon"/> uses
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetPorts)]
		[Required]
		public ushort? SecondaryPort { get; set; }

		/// <summary>
		/// The DreamDaemon startup timeout in seconds
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetStartupTimeout)]
		[Required]
		public int? StartupTimeout { get; set; }
	}
}