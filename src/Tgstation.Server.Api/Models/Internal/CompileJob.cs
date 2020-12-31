using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a run of <see cref="DreamMaker"/>
	/// </summary>
	public class CompileJob : EntityId
	{
		/// <summary>
		/// The .dme file used for compilation
		/// </summary>
		[Required]
		public string? DmeName { get; set; }

		/// <summary>
		/// Textual output of DM
		/// </summary>
		[Required]
		public string? Output { get; set; }

		/// <summary>
		/// The Game folder the results were compiled into
		/// </summary>
		[Required]
		public Guid? DirectoryName { get; set; }

		/// <summary>
		/// The minimum <see cref="DreamDaemonSecurity"/> required to run the <see cref="CompileJob"/>'s output
		/// </summary>
		public DreamDaemonSecurity? MinimumSecurityLevel { get; set; }

		/// <summary>
		/// The DMAPI <see cref="Version"/>.
		/// </summary>
		[NotMapped]
		public virtual Version? DMApiVersion { get; set; }
	}
}
