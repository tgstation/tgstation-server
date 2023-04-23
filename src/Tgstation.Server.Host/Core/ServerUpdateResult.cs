namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// The result of a call to start a server update.
	/// </summary>
	public enum ServerUpdateResult
	{
		/// <summary>
		/// The update process was started successfully.
		/// </summary>
		Started,

		/// <summary>
		/// The requested release version was not found.
		/// </summary>
		ReleaseMissing,

		/// <summary>
		/// Another update is already in progress.
		/// </summary>
		UpdateInProgress,

		/// <summary>
		/// The server swarm does not contain the expected amount of nodes.
		/// </summary>
		SwarmIntegrityCheckFailed,
	}
}
