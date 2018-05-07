using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class Job : Api.Models.Internal.Job, IApiConvertable<Api.Models.Job>
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

		/// <inheritdoc />
		public Api.Models.Job ToApi() => new Api.Models.Job
		{
			Id = Id,
			StartedAt = StartedAt,
			StoppedAt = StoppedAt,
			Cancelled = Cancelled,
			CancelledBy = CancelledBy.ToApi(),
			CancelRight = CancelRight,
			CancelRightsType = CancelRightsType,
			Description = Description,
			ExceptionDetails = ExceptionDetails,
			StartedBy = StartedBy.ToApi()
		};
	}
}
