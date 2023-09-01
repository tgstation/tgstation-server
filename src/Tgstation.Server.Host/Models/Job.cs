using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Tgstation.Server.Api.Models.Internal.Job" />
#pragma warning disable CA1724 // naming conflict with gitlab package
	public sealed class Job : Api.Models.Internal.Job, IApiTransformable<JobResponse>
#pragma warning restore CA1724
	{
		/// <summary>
		/// See <see cref="JobResponse.StartedBy"/>.
		/// </summary>
		[Required]
		public User StartedBy { get; set; }

		/// <summary>
		/// See <see cref="JobResponse.CancelledBy"/>.
		/// </summary>
		public User CancelledBy { get; set; }

		/// <summary>
		/// The <see cref="Models.Instance"/> the job belongs to if any.
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <inheritdoc />
		public JobResponse ToApi() => new ()
		{
			Id = Id,
			StartedAt = StartedAt,
			StoppedAt = StoppedAt,
			Cancelled = Cancelled,
			CancelledBy = CancelledBy?.CreateUserName(),
			CancelRight = CancelRight,
			CancelRightsType = CancelRightsType,
			Description = Description,
			ExceptionDetails = ExceptionDetails,
			ErrorCode = ErrorCode,
			StartedBy = StartedBy.CreateUserName(),
		};
	}
}
