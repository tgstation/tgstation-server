using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Metadata about an installation
	/// </summary>
	[Model(typeof(AdministrationRights))]
	public sealed class Administration
	{
		/// <summary>
		/// Use the specified Windows/UNIX authentication to authorize users. Setting this to <see langword="null"/> enables full administrative anonymous access
		/// </summary>
		[Permissions(ReadRight = AdministrationRights.ChangeAuthenticationGroup, WriteRight = AdministrationRights.ChangeAuthenticationGroup)]
		public string SystemAuthenticationGroup { get; set; }

		/// <summary>
		/// Automatically send unhandled exception data to a public collection service. This will be limited to system information, path data, and game code compilation information.
		/// </summary>
		[Permissions(ReadRight = AdministrationRights.ChangeTelemetry, WriteRight = AdministrationRights.ChangeTelemetry)]
		public bool EnableTelemetry { get; set; }

		/// <summary>
		/// Users in the <see cref="SystemAuthenticationGroup"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public IReadOnlyList<User> Users { get; set; }
	}
}
