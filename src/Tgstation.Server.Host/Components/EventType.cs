namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Types of events 
	/// </summary>
	public enum EventType
	{
		/// <summary>
		/// Parameters: Reference name, commit sha
		/// </summary>
		RepoResetOrigin = 0,
		/// <summary>
		/// Parameters: Checkout target
		/// </summary>
		RepoCheckout = 1,
		/// <summary>
		/// No parameters
		/// </summary>
		RepoFetch = 2,
		/// <summary>
		/// Parameters: Pull request number, pull request sha, merger name, merger message
		/// </summary>
		RepoMergePullRequest = 3,
		/// <summary>
		/// Parameters: Absolute path to repository root
		/// </summary>
		RepoPreSynchronize = 4,

		/// <summary>
		/// Parameters: Current version, new version
		/// </summary>
		ByondChangeStart = 5,
		/// <summary>
		/// Parameters: Error string
		/// </summary>
		ByondFail = 6,
		/// <summary>
		/// No parameters
		/// </summary>
		ByondChangeComplete = 7,
		/// <summary>
		/// Parameters: Game directory path, origin commit sha
		/// </summary>
		CompileStart = 8,
		/// <summary>
		/// No parameters
		/// </summary>
		CompileCancelled = 9,
		/// <summary>
		/// Parameters: Game directory path, "1" if compile succeeded and api validation failed, "0" otherwise
		/// </summary>
		CompileFailure = 10,
		/// <summary>
		/// Parameters: Game directory path
		/// </summary>
		CompileComplete = 11,

		/// <summary>
		/// Parameters: Exit code
		/// </summary>
		DDOtherCrash = 12,
		/// <summary>
		/// No parameters
		/// </summary>
		DDOtherExit = 13,

		/// <summary>
		/// No parameters
		/// </summary>
		InstanceAutoUpdateStart = 14,

		/// <summary>
		/// Parameters: Base sha, target sha, base reference, target reference
		/// </summary>
		RepoMergeConflict = 15,
	}
}
