namespace Tgstation.Server.Host.Components.Watchdog
{
    sealed class MonitorState
    {
		public bool RebootingInactiveServer { get; set; }
		public bool InactiveServerHasStagedDmb { get; set; }

		public ISessionController ActiveServer { get; set; }
		public ISessionController InactiveServer { get; set; }
    }
}
