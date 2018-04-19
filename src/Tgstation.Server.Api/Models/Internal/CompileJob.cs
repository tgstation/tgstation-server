using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a run of <see cref="DreamMaker"/>
	/// </summary>
	public class CompileJob
	{
		/// <summary>
		/// The ID of the job
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// When the compilation started
		/// </summary>
		[Required]
		public DateTimeOffset StartedAt { get; set; }

		/// <summary>
		/// When the compilation finished
		/// </summary>
		public DateTimeOffset FinishedAt { get; set; }

		/// <summary>
		/// If the compiler targeted the primary directory
		/// </summary>
		public bool? TargetedPrimaryDirectory { get; set; }

		/// <summary>
		/// Textual output of DM
		/// </summary>
		public string Output { get; set; }

		/// <summary>
		/// Exit code of DM. If <see langword="null"/>, the job was cancelled
		/// </summary>
		public int? ExitCode { get; set; }
	}
}
