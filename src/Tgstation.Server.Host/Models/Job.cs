using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
#pragma warning disable CA1724 // naming conflict with gitlab package
	public sealed class Job : Api.Models.Internal.Job, IApiTransformable<JobResponse>
#pragma warning restore CA1724
	{
		/// <summary>
		/// <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		[NotMapped]
		public new long Id
		{
			get => base.Id ?? throw new InvalidOperationException("Id was null!");
			set => base.Id = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Internal.Job.Description"/>.
		/// </summary>
		[NotMapped]
		public new string Description
		{
			get => base.Description ?? throw new InvalidOperationException("Description was null!");
			set => base.Description = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Internal.Job.Cancelled"/>.
		/// </summary>
		[NotMapped]
		public new bool Cancelled
		{
			get => base.Cancelled ?? throw new InvalidOperationException("Cancelled was null!");
			set => base.Cancelled = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Internal.Job.StartedAt"/>.
		/// </summary>
		[NotMapped]
		public new DateTimeOffset StartedAt
		{
			get => base.StartedAt ?? throw new InvalidOperationException("StartedAt was null!");
			set => base.StartedAt = value;
		}

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
