using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Metadata about an installation
	/// </summary>
	[Model(RightsType.Administration)]
	public class ServerSettings
	{
		/// <summary>
		/// Automatically send unhandled exception data to a public collection service. This will be limited to system information, path data, and game code compilation information.
		/// </summary>
		[Permissions(ReadRight = AdministrationRights.ChangeTelemetry, WriteRight = AdministrationRights.ChangeTelemetry)]
		public bool EnableTelemetry { get; set; }

		/// <summary>
		/// The git repository URL to recieve updates to Tgstation.Server.Host from, must include credentials if necessary. If set to <see langword="null"/> upstream pulls will be disabled entirely. Should be https://github.com/tgstation/tgstation-server or a fork of it
		/// </summary>
		[Permissions(ReadRight = AdministrationRights.SetUpstreamRepository, WriteRight = AdministrationRights.SetUpstreamRepository)]
		public string UpstreamRepository { get; set; }
	}
}
