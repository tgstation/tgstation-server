using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	public class Job
	{
		/// <summary>
		/// The <see cref="Job"/> ID
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// Human readable description of the <see cref="Job"/>
		/// </summary>
		[Required]
		public string Description { get; set; }

		/// <summary>
		/// When the <see cref="Job"/> was started
		/// </summary>
		[Required]
		public DateTimeOffset StartedAt { get; set; }

		public User StartedBy { get; set; }

		/// <summary>
		/// When the <see cref="Job"/> stopped
		/// </summary>
		public DateTimeOffset StoppedAt { get; set; }

		/// <summary>
		/// If the <see cref="Job"/> was cancelled
		/// </summary>
		public bool Cancelled { get; set; }
	}
}