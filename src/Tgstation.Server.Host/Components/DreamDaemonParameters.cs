namespace Tgstation.Server.Host.Components
{
	static class DreamDaemonParameters
	{
		/// <summary>
		/// Host version. Do not change to keep forwards/backwards api compatibility
		/// </summary>
		public const string HostVersion = "server_service_version";
		/// <summary>
		/// Path to <see cref="Models.InteropInfo"/> json
		/// </summary>
		public const string InfoJsonPath = "tgs_json";
	}
}
