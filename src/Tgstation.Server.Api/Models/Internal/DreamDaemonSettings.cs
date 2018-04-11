using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Configurable settings for <see cref="DreamDaemon"/>
	/// </summary>
	[Model(RightsType.DreamDaemon, CanCrud = true, RequiresInstance = true)]
	public class DreamDaemonSettings
	{
		/// <summary>
		/// If <see cref="DreamDaemon"/> starts when it's <see cref="Instance"/> starts
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetAutoStart)]
		public bool AutoStart { get; set; }

		/// <summary>
		/// If the BYOND web client can be used to connect to the game server
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetWebClient)]
		public bool AllowWebClient { get; set; }

		/// <summary>
		/// If the server is undergoing a soft reset. This may be automatically set by changes to other fields
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SoftRestart)]
		public bool SoftRestart { get; set; }

		/// <summary>
		/// If the server is undergoing a soft shutdown
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SoftShutdown)]
		public bool SoftShutdown { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of <see cref="DreamDaemon"/>
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetSecurity)]
		public DreamDaemonSecurity SecurityLevel { get; set; }

		/// <summary>
		/// The first port <see cref="DreamDaemon"/> uses. This should be the publically advertised port
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetPorts)]
		public ushort PrimaryPort { get; set; }

		/// <summary>
		/// The second port <see cref="DreamDaemon"/> uses
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetPorts)]
		public ushort SecondaryPort { get; set; }
	}
}
