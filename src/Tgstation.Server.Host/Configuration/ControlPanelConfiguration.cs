using System.Collections.Generic;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration options for the web control panel
	/// </summary>
	sealed class ControlPanelConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="ControlPanelConfiguration"/> resides in
		/// </summary>
		public const string Section = "ControlPanel";

		/// <summary>
		/// If the control panel is enabled
		/// </summary>
		public bool Enable { get; set; }
	}
}
