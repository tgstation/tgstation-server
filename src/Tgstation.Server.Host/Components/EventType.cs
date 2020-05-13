namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Types of events. Mirror in tgs.dm
	/// </summary>
	public enum EventType
	{
		/// <summary>
		/// Parameters: Reference name, commit sha
		/// </summary>
		RepoResetOrigin,

		/// <summary>
		/// Parameters: Checkout target
		/// </summary>
		RepoCheckout,

		/// <summary>
		/// No parameters
		/// </summary>
		RepoFetch,

		/// <summary>
		/// Parameters: Pull request number, pull request sha, merger message
		/// </summary>
		RepoMergePullRequest,

		/// <summary>
		/// Parameters: Absolute path to repository root
		/// </summary>
		RepoPreSynchronize,

		/// <summary>
		/// Parameters: Version being installed
		/// </summary>
		ByondInstallStart,

		/// <summary>
		/// Parameters: Error string
		/// </summary>
		ByondInstallFail,

		/// <summary>
		/// Parameters: Old active version, new active version
		/// </summary>
		ByondActiveVersionChange,

		/// <summary>
		/// Parameters: Game directory path, origin commit sha
		/// </summary>
		CompileStart,

		/// <summary>
		/// No parameters
		/// </summary>
		CompileCancelled,

		/// <summary>
		/// Parameters: Game directory path, "1" if compile succeeded and api validation failed, "0" otherwise
		/// </summary>
		CompileFailure,

		/// <summary>
		/// Parameters: Game directory path
		/// </summary>
		CompileComplete,

		/// <summary>
		/// No parameters
		/// </summary>
		InstanceAutoUpdateStart,

		/// <summary>
		/// Parameters: Base sha, target sha, base reference, target reference
		/// </summary>
		RepoMergeConflict,

		/// <summary>
		/// No parameters
		/// </summary>
		DeploymentComplete,

		/// <summary>
		/// Before the watchdog shutsdown. Not sent for graceful shutdowns. No parameters.
		/// </summary>
		WatchdogShutdown,

		/// <summary>
		/// Before the watchdog detaches. No parameters.
		/// </summary>
		WatchdogDetach,
	}
}
