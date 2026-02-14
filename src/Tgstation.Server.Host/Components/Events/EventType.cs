namespace Tgstation.Server.Host.Components.Events
{
	/// <summary>
	/// Types of events. Mirror in tgs.dm. Prefer last listed name for script.
	/// </summary>
	public enum EventType
	{
		/// <summary>
		/// Parameters: Reference name, commit sha.
		/// </summary>
		[EventScript("RepoResetOrigin")]
		RepoResetOrigin,

		/// <summary>
		/// Parameters: Checkout target, hard reset flag (If "True", this is actually a hard reset, not a checkout).
		/// </summary>
		[EventScript("RepoCheckout")]
		RepoCheckout,

		/// <summary>
		/// Parameters: (Optional) Repository access username, (Optional) repository access password.
		/// </summary>
		[EventScript("RepoFetch")]
		RepoFetch,

		/// <summary>
		/// Parameters: Test merge number, test merge target sha, merger message, source repository(?).
		/// </summary>
		[EventScript("RepoMergePullRequest")]
		RepoAddTestMerge,

		/// <summary>
		/// Parameters: Absolute path to repository root.
		/// </summary>
		/// <remarks>Changes made to the repository during this event will be pushed to the tracked branch if no test merges are present.</remarks>
		[EventScript("PreSynchronize")]
		RepoPreSynchronize,

		/// <summary>
		/// Parameters: Version being installed.
		/// </summary>
		[EventScript("ByondInstallStart", "EngineInstallStart")]
		EngineInstallStart,

		/// <summary>
		/// Parameters: Error string.
		/// </summary>
		[EventScript("ByondInstallFail", "EngineInstallFail")]
		EngineInstallFail,

		/// <summary>
		/// Parameters: Old active version, new active version.
		/// </summary>
		[EventScript("ByondActiveVersionChange", "EngineActiveVersionChange")]
		EngineActiveVersionChange,

		/// <summary>
		/// After the repo is copied, before CodeModifications are applied. Parameters: Game directory path, origin commit sha, engine version string, repository reference (or "(no branch)" if there is no reference).
		/// </summary>
		[EventScript("PreCompile")]
		CompileStart,

		/// <summary>
		/// No parameters.
		/// </summary>
		[EventScript("CompileCancelled")]
		CompileCancelled,

		/// <summary>
		/// Parameters: Game directory path, "1" if compile succeeded and api validation failed, "0" otherwise, engine version string.
		/// </summary>
		[EventScript("CompileFailure")]
		CompileFailure,

		/// <summary>
		/// Parameters: Game directory path, engine version string.
		/// </summary>
		[EventScript("PostCompile")]
		CompileComplete,

		/// <summary>
		/// No parameters.
		/// </summary>
		[EventScript("InstanceAutoUpdateStart")]
		InstanceAutoUpdateStart,

		/// <summary>
		/// Parameters: Base sha, target sha, base reference, target reference, all conflicting files.
		/// </summary>
		[EventScript("RepoMergeConflict")]
		RepoMergeConflict,

		/// <summary>
		/// No parameters.
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
		/// Watchdog event when DM code calls TgsInitializationsComplete(). No parameters.
		/// </summary>
		[EventScript("WorldPrime")]
		WorldPrime,

		/// <summary>
		/// After DD has launched. Not the same as WatchdogLaunch. Parameters: PID of DreamDaemon.
		/// </summary>
		[EventScript("DreamDaemonLaunch")]
		DreamDaemonLaunch,

		/// <summary>
		/// After a single submodule update is performed. Parameters: Updated submodule name.
		/// </summary>
		[EventScript("RepoSubmoduleUpdate")]
		RepoSubmoduleUpdate,

		/// <summary>
		/// After CodeModifications are applied, before DreamMaker is run. Parameters: Game directory path, origin commit sha, engine version string.
		/// </summary>
		[EventScript("PreDreamMaker")]
		PreDreamMaker,

		/// <summary>
		/// Whenever a deployment folder is deleted from disk. Parameters: Game directory path.
		/// </summary>
		[EventScript("DeploymentCleanup")]
		DeploymentCleanup,

		/// <summary>
		/// Whenever a deployment is about to be used by the game server. May fire multiple times per deployment. Parameters: Game directory path.
		/// </summary>
		[EventScript("DeploymentActivation")]
		DeploymentActivation,

		/// <summary>
		/// Parameters: Version being installed.
		/// </summary>
		[EventScript("EngineInstallComplete")]
		EngineInstallComplete,

		/// <summary>
		/// Before game server process is created. No parameters.
		/// </summary>
		[EventScript("DreamDaemonPreLaunch")]
		DreamDaemonPreLaunch,
	}
}
