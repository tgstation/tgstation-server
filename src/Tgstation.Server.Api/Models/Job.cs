namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a long running job on the server. Model is read-only, updates attempt to cancel the job
	/// </summary>
	[Model]
	public sealed class Job : Internal.Job
	{
		/// <summary>
		/// If the <see cref="Job"/> has incremental progress, this will range from 1 - 100. 0 otherwise
		/// </summary>
		public int Progress { get; set; }

		/// <summary>
		/// If the current user has permission to cancel the job
		/// </summary>
		public bool UserCanCancel { get; set; }
	}
}
