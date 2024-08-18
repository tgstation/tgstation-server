using System.ComponentModel;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// The different types of <see cref="Response.JobResponse"/>.
	/// </summary>
	public enum JobCode : byte
	{
		/// <summary>
		/// This catch-all code is applied to jobs that were created on a tgstation-server before v5.17.0.
		/// </summary>
		[Description("Legacy job")]
		Unknown,

		/// <summary>
		/// When the instance is being moved.
		/// </summary>
		[Description("Move instance")]
		Move,

		/// <summary>
		/// When the repository is cloning.
		/// </summary>
		[Description("Clone repository")]
		RepositoryClone,

		/// <summary>
		/// When the repository is being manually updated.
		/// </summary>
		[Description("Update repository")]
		RepositoryUpdate,

		/// <summary>
		/// When the repository is being automatically updated.
		/// </summary>
		[Description("Scheduled repository update")]
		RepositoryAutoUpdate,

		/// <summary>
		/// When the repository is being deleted.
		/// </summary>
		[Description("Delete repository")]
		RepositoryDelete,

		/// <summary>
		/// When a new official engine version is being installed.
		/// </summary>
		[Description("Install engine version")]
		EngineOfficialInstall,

		/// <summary>
		/// When a new custom engine version is being installed.
		/// </summary>
		[Description("Install custom engine version")]
		EngineCustomInstall,

		/// <summary>
		/// When an installed engine version is being deleted.
		/// </summary>
		[Description("Delete installed engine version")]
		EngineDelete,

		/// <summary>
		/// When a deployment is manually triggered.
		/// </summary>
		[Description("Compile active repository code")]
		Deployment,

		/// <summary>
		/// When a deployment is automatically triggered.
		/// </summary>
		[Description("Scheduled code deployment")]
		AutomaticDeployment,

		/// <summary>
		/// When the watchdog is started manually.
		/// </summary>
		[Description("Launch Watchdog")]
		WatchdogLaunch,

		/// <summary>
		/// When the watchdog is restarted manually.
		/// </summary>
		[Description("Restart Watchdog")]
		WatchdogRestart,

		/// <summary>
		/// When a the watchdog is dumping the game server process.
		/// </summary>
		[Description("Create DreamDaemon Process Dump")]
		WatchdogDump,

		/// <summary>
		/// When the watchdog starts due to an instance being onlined.
		/// </summary>
		[Description("Instance startup watchdog launch")]
		StartupWatchdogLaunch,

		/// <summary>
		/// When the watchdog reattaches due to an instance being onlined.
		/// </summary>
		[Description("Instance startup watchdog reattach")]
		StartupWatchdogReattach,

		/// <summary>
		/// When a chat bot connects/reconnects.
		/// </summary>
		[Description("Reconnect chat bot")]
		ReconnectChatBot,

		/// <summary>
		/// When a repository is recloned.
		/// </summary>
		[Description("Reclone repository")]
		RepositoryReclone,
	}
}
