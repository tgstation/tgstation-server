using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class Job : Api.Models.Internal.Job
	{
		/// <summary>
		/// See <see cref="Api.Models.Job.StartedBy"/>
		/// </summary>
		[Required]
		public User StartedBy { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.Job.CancelledBy"/>
		/// </summary>
		public User CancelledBy { get; set; }

		/// <summary>
		/// The <see cref="Models.Instance"/> the job belongs to if any
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <summary>
		/// A <see cref="Task"/> to run after the job completes. This will not affect the <see cref="Api.Models.Internal.Job.StoppedAt"/> time, unless it is cancelled or errors
		/// </summary>
		/// <remarks>This should only be used where there are database dependencies that also rely on the Job itself completing A.K.A. manually initiated <see cref="CompileJob"/>s</remarks>
		[NotMapped]
		public Func<CancellationToken, Task> PostComplete { get; set; }

		/// <summary>
		/// Convert the <see cref="Job"/> to it's API form
		/// </summary>
		/// <returns>A new <see cref="Api.Models.Job"/></returns>
		public Api.Models.Job ToApi() => new Api.Models.Job
		{
			Id = Id,
			StartedAt = StartedAt,
			StoppedAt = StoppedAt,
			Cancelled = Cancelled,
			CancelledBy = CancelledBy?.ToApi(false),
			CancelRight = CancelRight,
			CancelRightsType = CancelRightsType,
			Description = Description,
			ExceptionDetails = ExceptionDetails,
			StartedBy = StartedBy.ToApi(false)
		};
	}
}
