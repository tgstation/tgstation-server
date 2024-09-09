using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents the state of the DreamMaker compiler. Create action starts a new compile. Delete action cancels the current compile.
	/// </summary>
	public abstract class DreamMakerSettings
	{
		/// <summary>
		/// The name of the .dme file the server tries to compile with without the extension.
		/// </summary>
		[StringLength(Limits.MaximumStringLength)]
		[ResponseOptions]
		public string? ProjectName { get; set; }

		/// <summary>
		/// The port used during compilation to validate the DMAPI.
		/// </summary>
		[Required]
		[Range(1, 65535)]
		public ushort? ApiValidationPort { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level used to validate the DMAPI.
		/// </summary>
		[Required]
		public DreamDaemonSecurity? ApiValidationSecurityLevel { get; set; }

		/// <summary>
		/// If API validation should be required for a deployment to succeed. Must not be set on mutation if <see cref="DMApiValidationMode"/> is set.
		/// </summary>
		[Required]
		[NotMapped]
		[Obsolete($"Use {nameof(DMApiValidationMode)} instead.")]
		public bool? RequireDMApiValidation { get; set; }

		/// <summary>
		/// The current <see cref="Models.DMApiValidationMode"/>. Must not be set on mutation if <see cref="RequireDMApiValidation"/> is set.
		/// </summary>
		[Required]
		public DMApiValidationMode? DMApiValidationMode { get; set; }

		/// <summary>
		/// Amount of time before an in-progress deployment is cancelled.
		/// </summary>
		[Required]
		public TimeSpan? Timeout { get; set; }

		/// <summary>
		/// Additional arguments added to the compiler command line.
		/// </summary>
		[StringLength(Limits.MaximumStringLength)]
		[ResponseOptions]
		public string? CompilerAdditionalArguments { get; set; }
	}
}
