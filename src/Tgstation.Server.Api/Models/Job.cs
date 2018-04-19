namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a long running job on the server. Model is read-only, updates attempt to cancel the job
	/// </summary>
	public sealed class Job : Internal.Job
	{
		/// <summary>
		/// If the <see cref="Job"/> has incremental progress, this will range from 1 - 100. 0 otherwise
		/// </summary>
		[Permissions(DenyWrite = true)]
		public int Progress { get; set; }

		/// <summary>
		/// If the current user has permission to cancel the job
		/// </summary>
		[Permissions(DenyWrite = true)]
		public bool UserCanCancel { get; set; }

		/// <summary>
		/// The <see cref="User"/> that started the job
		/// </summary>
		[Permissions(DenyWrite = true)]
		public User StartedBy { get; set; }
	}
}
