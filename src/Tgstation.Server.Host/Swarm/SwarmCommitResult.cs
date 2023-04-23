namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// How to proceed on the commit step of an update.
	/// </summary>
	public enum SwarmCommitResult
	{
		/// <summary>
		/// The update should be aborted.
		/// </summary>
		AbortUpdate,

		/// <summary>
		/// The update should be committed.
		/// </summary>
		ContinueUpdateNonCommitted,

		/// <summary>
		/// The update MUST be committed.
		/// </summary>
		MustCommitUpdate,
	}
}
