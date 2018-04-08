using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tgstation.Server.Api.Models
{
	public class CompileJob
	{
		/// <summary>
		/// The ID of the job
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="User"/> that triggered the job
		/// </summary>
		public User TriggeredBy { get; set; }

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
		
		/// <summary>
		/// Git revision the compiler ran on. Not modifiable
		/// </summary>
		public RevisionInformation RevisionInformation { get; set; }
	}
}
