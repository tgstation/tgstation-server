using System;
using System.Collections.Generic;
using System.Text;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Metadata about an installation
	/// </summary>
	public sealed class Administration
	{
		/// <summary>
		/// Use the specified Windows/UNIX authentication to authorize users. Setting this to <see langword="null"/> enables full administrative anonymous access
		/// </summary>
		public string SystemAuthenticationGroup { get; set; }

		/// <summary>
		/// Automatically send unhandled exception data to a public collection service. This will be limited to system information, path data, and game code compilation information.
		/// </summary>
		public bool EnableTelemetry { get; set; }

		/// <summary>
		/// Users in the <see cref="SystemAuthenticationGroup"/>. Not modifiable
		/// </summary>
		public IReadOnlyList<User> Users { get; set; }
	}
}
