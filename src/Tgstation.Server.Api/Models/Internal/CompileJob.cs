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
		[Required]
		public DateTimeOffset FinishedAt { get; set; }

		/// <summary>
		/// The detected DMAPI version
		/// </summary>
		public Version DMApiVersion { get; set; }

		/// <summary>
		/// The .dme file used for compilation
		/// </summary>
		public string DmePath { get; set; }

		/// <summary>
		/// Textual output of DM
		/// </summary>
		public string Output { get; set; }

		/// <summary>
		/// The Game folder the results were compiled into
		/// </summary>
		public Guid? OutputGuid { get; set; }

		/// <summary>
		/// Exit code of DM. If <see langword="null"/>
		/// </summary>
		public int? ExitCode { get; set; }
	}
}
