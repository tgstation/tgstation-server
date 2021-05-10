using System.Collections.Generic;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration options for the web control panel.
	/// </summary>
	public sealed class ControlPanelConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="ControlPanelConfiguration"/> resides in.
		/// </summary>
		public const string Section = "ControlPanel";

		/// <summary>
		/// If the control panel is enabled.
		/// </summary>
		public bool Enable { get; set; }

		/// <summary>
		/// If any origin is allowed for CORS requests. This overrides <see cref="AllowedOrigins"/>.
		/// </summary>
		public bool AllowAnyOrigin { get; set; }

		/// <summary>
		/// The channel to retrieve the webpanel from. "local" uses the bundled version.
		/// </summary>
		public string Channel { get; set; }

		/// <summary>
		/// Origins allowed for CORS requests.
		/// </summary>
		public ICollection<string> AllowedOrigins { get; set; }
	}
}
