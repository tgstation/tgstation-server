using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Configurable settings for <see cref="DreamDaemonResponse"/>
	/// </summary>
	public class DreamDaemonSettings : DreamDaemonLaunchParameters
	{
		/// <summary>
		/// If <see cref="DreamDaemonResponse"/> starts when it's <see cref="Instance"/> starts
		/// </summary>
		[Required]
		[ResponseOptions]
		public bool? AutoStart { get; set; }
	}
}
