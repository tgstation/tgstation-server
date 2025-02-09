using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a deployment run.
	/// </summary>
	public abstract class CompileJob : EntityId
	{
		/// <summary>
		/// The .dme file used for compilation.
		/// </summary>
		/// <example>tgstation.dme</example>
		[Required]
		public string? DmeName { get; set; }

		/// <summary>
		/// Textual output of DM.
		/// </summary>
		[Required]
		public string? Output { get; set; }

		/// <summary>
		/// The Game folder the results were compiled into.
		/// </summary>
		[Required]
		public Guid? DirectoryName { get; set; }

		/// <summary>
		/// The minimum <see cref="DreamDaemonSecurity"/> required to run the <see cref="CompileJob"/>'s output.
		/// </summary>
		[ResponseOptions]
		public DreamDaemonSecurity? MinimumSecurityLevel { get; set; }

		/// <summary>
		/// The DMAPI <see cref="Version"/>.
		/// </summary>
		/// <example>7.3.0</example>
		[NotMapped]
		[ResponseOptions]
		public virtual Version? DMApiVersion { get; set; }
	}
}
