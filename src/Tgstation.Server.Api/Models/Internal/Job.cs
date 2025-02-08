using System;
using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a long running job.
	/// </summary>
	public class Job : EntityId
	{
		/// <summary>
		/// The <see cref="Models.JobCode"/>.
		/// </summary>
		[Required]
		public JobCode? JobCode { get; set; }

		/// <summary>
		/// English description of the <see cref="Job"/>.
		/// </summary>
		/// <remarks>May not match the <see cref="System.ComponentModel.DescriptionAttribute"/> listed on the <see cref="Models.JobCode"/>.</remarks>
		/// <example>Installing and configuring important objects.</example>
		[Required]
		public string? Description { get; set; }

		/// <summary>
		/// The <see cref="Models.ErrorCode"/> associated with the <see cref="Job"/> if any.
		/// </summary>
		[ResponseOptions]
		public ErrorCode? ErrorCode { get; set; }

		/// <summary>
		/// Details of any exceptions caught during the <see cref="Job"/>.
		/// </summary>
		[ResponseOptions]
		public string? ExceptionDetails { get; set; }

		/// <summary>
		/// When the <see cref="Job"/> was started.
		/// </summary>
		[Required]
		public DateTimeOffset? StartedAt { get; set; }

		/// <summary>
		/// When the <see cref="Job"/> stopped.
		/// </summary>
		[ResponseOptions]
		public DateTimeOffset? StoppedAt { get; set; }

		/// <summary>
		/// If the <see cref="Job"/> was cancelled.
		/// </summary>
		/// <example>false</example>
		[Required]
		public bool? Cancelled { get; set; }

		/// <summary>
		/// The <see cref="RightsType"/> of <see cref="CancelRight"/> if it can be cancelled.
		/// </summary>
		[ResponseOptions]
		public RightsType? CancelRightsType { get; set; }

		/// <summary>
		/// The <see cref="Rights"/> required to cancel the <see cref="Job"/>.
		/// </summary>
		[ResponseOptions]
		public ulong? CancelRight { get; set; }
	}
}
