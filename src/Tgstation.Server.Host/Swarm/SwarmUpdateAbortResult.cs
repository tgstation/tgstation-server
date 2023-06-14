namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Result of attempting to abort a <see cref="SwarmUpdateOperation"/>.
	/// </summary>
	public enum SwarmUpdateAbortResult
	{
		/// <summary>
		/// The operation was successfully aborted by the caller and followup actions should be perform.
		/// </summary>
		Aborted,

		/// <summary>
		/// The operation was already successfully aborted by another caller.
		/// </summary>
		AlreadyAborted,

		/// <summary>
		/// The operation cannot abort because it has committed.
		/// </summary>
		CantAbortCommitted,
	}
}
