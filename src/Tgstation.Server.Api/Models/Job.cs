namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a long running job on the server. Model is read-only, updates attempt to cancel the job
	/// </summary>
	public sealed class Job : Internal.Job
	{
		/// <summary>
		/// The <see cref="User"/> that started the job
		/// </summary>
		[Permissions(DenyWrite = true)]
		public User StartedBy { get; set; }

		/// <summary>
		/// The <see cref="User"/> that cancelled the job
		/// </summary>
		[Permissions(DenyWrite = true)]
		public User CancelledBy { get; set; }

		/// <summary>
		/// Optional progress between 0 and 100 inclusive
		/// </summary>
		public int? Progress { get; set; }
	}
}
