using System;
using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a long running job
	/// </summary>
	public class Job : EntityId
	{
		/// <summary>
		/// English description of the <see cref="Job"/>
		/// </summary>
		[Required]
		public string? Description { get; set; }

		/// <summary>
		/// The <see cref="Models.ErrorCode"/> associated with the <see cref="Job"/> if any.
		/// </summary>
		public ErrorCode? ErrorCode { get; set; }

		/// <summary>
		/// Details of any exceptions caught during the <see cref="Job"/>
		/// </summary>
		public string? ExceptionDetails { get; set; }

		/// <summary>
		/// When the <see cref="Job"/> was started
		/// </summary>
		[Required]
		public DateTimeOffset? StartedAt { get; set; }

		/// <summary>
		/// When the <see cref="Job"/> stopped
		/// </summary>
		public DateTimeOffset? StoppedAt { get; set; }

		/// <summary>
		/// If the <see cref="Job"/> was cancelled
		/// </summary>
		[Required]
		public bool? Cancelled { get; set; }

		/// <summary>
		/// The <see cref="RightsType"/> of <see cref="CancelRight"/> if it can be cancelled
		/// </summary>
		public RightsType? CancelRightsType { get; set; }

		/// <summary>
		/// The <see cref="Rights"/> required to cancel the <see cref="Job"/>
		/// </summary>
		public ulong? CancelRight { get; set; }
	}
}