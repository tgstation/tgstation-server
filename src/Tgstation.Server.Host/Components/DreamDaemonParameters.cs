namespace Tgstation.Server.Host.Components
{
	static class DreamDaemonParameters
	{
		/// <summary>
		/// Do not change to keep forwards/backwards api compatibility
		/// </summary>
		public const string HostVersion = "server_service_versions";
		public const string AccessToken = "tgs_key";
		public const string PrimaryPort = "tgs_port1";
		public const string SecondaryPort = "tgs_port2";
		public const string IsSecondaryServer = "tgs_second";
	}
}
