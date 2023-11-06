namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents a long running job on the server. Model is read-only, updates attempt to cancel the job.
	/// </summary>
	public sealed class JobResponse : Internal.Job
	{
		/// <summary>
		/// The <see cref="EntityId.Id"/> of the <see cref="Instance"/>.
		/// </summary>
		public long? InstanceId { get; set; }

		/// <summary>
		/// The <see cref="UserResponse"/> that started the job.
		/// </summary>
		public UserName? StartedBy { get; set; }

		/// <summary>
		/// The <see cref="UserResponse"/> that cancelled the job.
		/// </summary>
		[ResponseOptions]
		public UserName? CancelledBy { get; set; }

		/// <summary>
		/// Optional progress between 0 and 100 inclusive.
		/// </summary>
		[ResponseOptions]
		public int? Progress { get; set; }

		/// <summary>
		/// Optional description of the job's current .
		/// </summary>
		[ResponseOptions]
		public string? Stage { get; set; }
	}
}
