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
		/// Parameters: Reference name, commit sha
		/// </summary>
		RepoCheckout = 1,
		/// <summary>
		/// No parameters
		/// </summary>
		RepoFetch = 2,
		/// <summary>
		/// Parameters: Comma separated list in form of "#{Pull Request Number} @ {7 character SHA}
		/// </summary>
		RepoMergePullRequests = 3,

		/// <summary>
		/// Parameters: Current version, new version
		/// </summary>
		ByondChangeStart = 4,
		/// <summary>
		/// No parameters
		/// </summary>
		ByondChangeCancelled = 5,
		/// <summary>
		/// Parameters: Error string
		/// </summary>
		ByondFail = 6,
		/// <summary>
		/// No parameters
		/// </summary>
		ByondStageComplete = 7,
		/// <summary>
		/// No parameters
		/// </summary>
		ByondChangeComplete = 8,

		/// <summary>
		/// Parameters: Origin commit sha
		/// </summary>
		CompileStart = 9,
		/// <summary>
		/// No parameters
		/// </summary>
		CompileCancelled = 10,
		/// <summary>
		/// Parameters: "1" if compile succeeded and api validation failed, "0" otherwise
		/// </summary>
		CompileFailure = 11,
		/// <summary>
		/// No parameters
		/// </summary>
		CompileComplete = 12,
		
		/// <summary>
		/// Parameters: Exit code
		/// </summary>
		DDOtherCrash = 13,
		/// <summary>
		/// No parameters
		/// </summary>
		DDOtherExit = 14,
	}
}
