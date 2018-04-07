using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	public class ServerSettings
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
		/// The git repository to recieve updates to Tgstation.Server.Host from, must include credentials if necessary. If set to <see langword="null"/> upstream pulls will be disabled entirely
		/// </summary>
		[Permissions(ReadRight = AdministrationRights.SetUpstreamRepository, WriteRight = AdministrationRights.SetUpstreamRepository)]
		public string UpstreamRepository { get; set; }
	}
}
