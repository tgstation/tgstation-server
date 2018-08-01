using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Configurable settings for <see cref="DreamDaemon"/>
	/// </summary>
	public class DreamDaemonSettings : DreamDaemonLaunchParameters
	{
		/// <summary>
		/// If <see cref="DreamDaemon"/> starts when it's <see cref="Instance"/> starts
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SetAutoStart)]
		[Required]
		public bool? AutoStart { get; set; }

		/// <summary>
		/// If the server is undergoing a soft reset. This may be automatically set by changes to other fields
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SoftRestart)]
		[Required]
		public bool? SoftRestart { get; set; }

		/// <summary>
		/// If the server is undergoing a soft shutdown
		/// </summary>
		[Permissions(ReadRight = DreamDaemonRights.ReadMetadata, WriteRight = DreamDaemonRights.SoftShutdown)]
		[Required]
		public bool? SoftShutdown { get; set; }
	}
}
