using System.ComponentModel.DataAnnotations;

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
		[Required]
		public bool? AutoStart { get; set; }
	}
}
