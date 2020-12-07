using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	#pragma warning disable CA1724 // naming conflict with gitlab package
	public sealed class Job : Api.Models.Internal.Job
	#pragma warning restore CA1724
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
			ErrorCode = ErrorCode,
			StartedBy = StartedBy.ToApi(false)
		};
	}
}
