namespace Tgstation.Server.Host.Components.Events
{
	/// <summary>
	/// Types of events. Mirror in tgs.dm
	/// </summary>
	public enum EventType
	{
		/// <summary>
		/// Parameters: Reference name, commit sha
		/// </summary>
		[EventScript("RepoResetOrigin")]
		RepoResetOrigin,

		/// <summary>
		/// Parameters: Checkout target
		/// </summary>
		[EventScript("RepoCheckout")]
		RepoCheckout,

		/// <summary>
		/// No parameters
		/// </summary>
		[EventScript("RepoFetch")]
		RepoFetch,

		/// <summary>
		/// Parameters: Test merge number, test merge target sha, merger message
		/// </summary>
		[EventScript("RepoMergePullRequest")]
		RepoAddTestMerge,

		/// <summary>
		/// Parameters: Absolute path to repository root
		/// </summary>
		/// <remarks>Changes made to the repository during this event will be pushed to the tracked branch if no test merges are present.</remarks>
		[EventScript("PreSynchronize")]
		RepoPreSynchronize,

		/// <summary>
		/// Parameters: Version being installed
		/// </summary>
		[EventScript("ByondInstallStart")]
		ByondInstallStart,

		/// <summary>
		/// Parameters: Error string
		/// </summary>
		[EventScript("ByondInstallFail")]
		ByondInstallFail,

		/// <summary>
		/// Parameters: Old active version, new active version
		/// </summary>
		[EventScript("ByondActiveVersionChange")]
		ByondActiveVersionChange,

		/// <summary>
		/// Parameters: Game directory path, origin commit sha
		/// </summary>
		[EventScript("PreCompile")]
		CompileStart,

		/// <summary>
		/// No parameters
		/// </summary>
		[EventScript("CompileCancelled")]
		CompileCancelled,

		/// <summary>
		/// Parameters: Game directory path, "1" if compile succeeded and api validation failed, "0" otherwise
		/// </summary>
		[EventScript("CompileFailure")]
		CompileFailure,

		/// <summary>
		/// Parameters: Game directory path
		/// </summary>
		[EventScript("PostCompile")]
		CompileComplete,

		/// <summary>
		/// No parameters
		/// </summary>
		[EventScript("InstanceAutoUpdateStart")]
		InstanceAutoUpdateStart,

		/// <summary>
		/// Parameters: Base sha, target sha, base reference, target reference
		/// </summary>
		[EventScript("RepoMergeConflict")]
		RepoMergeConflict,

		/// <summary>
		/// No parameters
		/// </summary>
		[EventScript("DeploymentComplete")]
		DeploymentComplete,

		/// <summary>
		/// Before the watchdog shuts down. Not sent for graceful shutdowns. No parameters.
		/// </summary>
		[EventScript("WatchdogShutdown")]
		WatchdogShutdown,

		/// <summary>
		/// Before the watchdog detaches. No parameters.
		/// </summary>
		[EventScript("WatchdogDetach")]
		WatchdogDetach,

		/// <summary>
		/// Before the watchdog launches. No parameters.
		/// </summary>
		[EventScript("WatchdogLaunch")]
		WatchdogLaunch,

		/// <summary>
		/// Watchdog event when DreamDaemon exits unexpectedly. No parameters.
		/// </summary>
		[EventScript("WatchdogCrash")]
		WatchdogCrash,

		/// <summary>
		/// In between watchdog DreamDaemon restarts if the process has been force-ended by the DMAPI (TgsEndProcess()). No parameters.
		/// </summary>
		[EventScript("WorldEndProcess")]
		WorldEndProcess,

		/// <summary>
		/// Watchdog event when TgsReboot() is called. Not synchronous. Called after <see cref="WorldEndProcess"/>. No parameters.
		/// </summary>
		[EventScript("WorldReboot")]
		WorldReboot,

		/// <summary>
		/// Watchdog event when TgsInitializationsComplete() is called. No parameters.
		/// </summary>
		[EventScript("WorldPrime")]
		WorldPrime,

		/// <summary>
		/// After DD has launched. Not the same as WatchdogLaunch. Parameters: PID of DreamDaemon
		/// </summary>
		[EventScript("DreamDaemonLaunch")]
		DreamDaemonLaunch,
	}
}
