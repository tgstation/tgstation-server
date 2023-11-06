namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Allows manually triggering jobs hub updates.
	/// </summary>
	interface IJobsHubUpdater
	{
		/// <summary>
		/// Queue a message to be sent to all clients with the current state of active jobs.
		/// </summary>
		void QueueActiveJobUpdates();
	}
}
