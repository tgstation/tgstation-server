namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Types of events 
	/// </summary>
	enum EventType
	{
		/// <summary>
		/// Parameters: Reference name, commit sha
		/// </summary>
		RepoResetOrigin,
		/// <summary>
		/// Parameters: Reference name, commit sha
		/// </summary>
		RepoCheckout,
		/// <summary>
		/// No parameters
		/// </summary>
		RepoFetch,
		/// <summary>
		/// Parameters: Comma separated list in form of "#{Pull Request Number} @ {7 character SHA}
		/// </summary>
		RepoMergePullRequests,

		/// <summary>
		/// Parameters: Current version, new version
		/// </summary>
		ByondChangeStart,
		/// <summary>
		/// No parameters
		/// </summary>
		ByondChangeCancelled,
		/// <summary>
		/// Parameters: Error string
		/// </summary>
		ByondFail,
		/// <summary>
		/// No parameters
		/// </summary>
		ByondStageComplete,
		/// <summary>
		/// No parameters
		/// </summary>
		ByondChangeComplete,

		/// <summary>
		/// Parameters: Commit sha, parameter of <see cref="RepoMergePullRequests"/>
		/// </summary>
		CompileStart,
		/// <summary>
		/// No parameters
		/// </summary>
		CompileCancelled,
		/// <summary>
		/// Parameters: Error string
		/// </summary>
		CompileFailure,
		/// <summary>
		/// No parameters
		/// </summary>
		CompileComplete,

		/// <summary>
		/// Parameters: Access token
		/// </summary>
		DDLaunched,
		/// <summary>
		/// Parameters: Exit code
		/// </summary>
		DDCrash,
		/// <summary>
		/// No parameters
		/// </summary>
		DDExit,
		/// <summary>
		/// No parameters
		/// </summary>
		DDRestart,
		/// <summary>
		/// No parameters
		/// </summary>
		DDBeginGracefulRestart,
		/// <summary>
		/// No parameters
		/// </summary>
		DDBeginGracefulShutdown,
		/// <summary>
		/// No parameters
		/// </summary>
		DDCancelGraceful,
		/// <summary>
		/// No parameters
		/// </summary>
		DDTerminated,
	}
}
