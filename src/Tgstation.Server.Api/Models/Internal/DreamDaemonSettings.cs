using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Configurable settings for DreamDaemon.
	/// </summary>
	public class DreamDaemonSettings : DreamDaemonLaunchParameters
	{
		/// <summary>
		/// If the watchdog starts when it's <see cref="Instance"/> starts
		/// </summary>
		[Required]
		[ResponseOptions]
		public bool? AutoStart { get; set; }
	}
}
