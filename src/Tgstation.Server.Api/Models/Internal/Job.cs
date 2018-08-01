using System;
using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a long running job
	/// </summary>
	[Model(RequiresInstance = true)]
	public class Job
	{
		/// <summary>
		/// The <see cref="Job"/> ID
		/// </summary>
		[Permissions(DenyWrite = true)]
		public long Id { get; set; }

		/// <summary>
		/// English description of the <see cref="Job"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		[Required]
		public string Description { get; set; }

		/// <summary>
		/// Details of any exceptions caught during the <see cref="Job"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public string ExceptionDetails { get; set; }

		/// <summary>
		/// When the <see cref="Job"/> was started
		/// </summary>
		[Permissions(DenyWrite = true)]
		[Required]
		public DateTimeOffset? StartedAt { get; set; }

		/// <summary>
		/// When the <see cref="Job"/> stopped
		/// </summary>
		[Permissions(DenyWrite = true)]
		public DateTimeOffset? StoppedAt { get; set; }

		/// <summary>
		/// If the <see cref="Job"/> was cancelled
		/// </summary>
		[Permissions(DenyWrite = true)]
		[Required]
		public bool? Cancelled { get; set; }

		/// <summary>
		/// The <see cref="RightsType"/> of <see cref="CancelRight"/> if it can be cancelled
		/// </summary>
		[Permissions(DenyWrite = true)]
		public RightsType? CancelRightsType { get; set; }

		/// <summary>
		/// The <see cref="Rights"/> required to cancel the <see cref="Job"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public int? CancelRight { get; set; }
	}
}