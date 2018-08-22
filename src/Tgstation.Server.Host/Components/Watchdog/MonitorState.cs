using Newtonsoft.Json;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// The (absolute) state of the <see cref="Watchdog"/>
	/// </summary>
	sealed class MonitorState
	{
		/// <summary>
		/// If the inactive server is being rebooted
		/// </summary>
		public bool RebootingInactiveServer { get; set; }

		/// <summary>
		/// If the inactive server has a .dmb and needs to be swapped in
		/// </summary>
		public bool InactiveServerHasStagedDmb { get; set; }

		/// <summary>
		/// If the inactive server is in an unrecoverable state
		/// </summary>
		public bool InactiveServerCritFail { get; set; }

		/// <summary>
		/// The next <see cref="MonitorAction"/> to take in <see cref="Watchdog.MonitorLifetimes(System.Threading.CancellationToken)"/>
		/// </summary>
		public MonitorAction NextAction { get; set; }

		/// <summary>
		/// The active <see cref="ISessionController"/>
		/// </summary>
		[JsonIgnore]
		public ISessionController ActiveServer { get; set; }

		/// <summary>
		/// The inactive <see cref="ISessionController"/>
		/// </summary>
		[JsonIgnore]
		public ISessionController InactiveServer { get; set; }
	}
}
