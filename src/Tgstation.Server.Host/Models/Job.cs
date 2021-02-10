using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	#pragma warning disable CA1724 // naming conflict with gitlab package
	public sealed class Job : Api.Models.Internal.Job, IApiTransformable<Api.Models.JobResponse>
	#pragma warning restore CA1724
	{
		/// <summary>
		/// See <see cref="Api.Models.JobResponse.StartedBy"/>
		/// </summary>
		[Required]
		public User StartedBy { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.JobResponse.CancelledBy"/>
		/// </summary>
		public User CancelledBy { get; set; }

		/// <summary>
		/// The <see cref="Models.Instance"/> the job belongs to if any
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <inheritdoc />
		public Api.Models.JobResponse ToApi() => new Api.Models.JobResponse
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
