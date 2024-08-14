namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration options for telemetry.
	/// </summary>
	public sealed class TelemetryConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="TelemetryConfiguration"/> resides in.
		/// </summary>
		public const string Section = "Telemetry";

		/// <summary>
		/// The default value of <see cref="VersionReportingRepositoryId"/>.
		/// </summary>
		private const long DefaultVersionReportingRepositoryId = 841149827; // https://github.com/tgstation/tgstation-server-deployments

		/// <summary>
		/// If version reporting telemetry is disabled.
		/// </summary>
		public bool DisableVersionReporting { get; set; }

		/// <summary>
		/// The friendly name used on GitHub deployments for version reporting. If <see langword="null"/> only the server <see cref="global::System.Guid"/> will be shown.
		/// </summary>
		public string? ServerFriendlyName { get; set; }

		/// <summary>
		/// The GitHub repository ID used for version reporting.
		/// </summary>
		public long? VersionReportingRepositoryId { get; set; } = DefaultVersionReportingRepositoryId;
	}
}
