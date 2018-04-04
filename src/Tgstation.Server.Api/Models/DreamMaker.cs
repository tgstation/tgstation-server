using System;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
    /// <summary>
    /// Represents the state of the DreamMaker compiler. Create action starts a new compile. Delete action cancels the current compile
    /// </summary>
	[Model(RightsType.DreamMaker, ReadRight = DreamMakerRights.Read, CanCrud = true, RequiresInstance = true)]
    public sealed class DreamMaker
	{
		/// <summary>
		/// When the compilation started
		/// </summary>
		[Permissions(DenyWrite = true)]
        public DateTimeOffset StartedAt { get; set; }
		/// <summary>
		/// When the compilation finished
		/// </summary>
		[Permissions(DenyWrite = true)]
		public DateTimeOffset FinishedAt { get; set; }
		/// <summary>
		/// The <see cref="CompilerStatus"/> of the compiler
		/// </summary>
		[Permissions(DenyWrite = true)]
		public CompilerStatus Status { get; set; }
		/// <summary>
		/// If the compiler targeted the primary directory
		/// </summary>
		[Permissions(DenyWrite = true)]
		public bool? TargetedPrimaryDirectory { get; set; }
		/// <summary>
		/// Textual output of DM
		/// </summary>
		[Permissions(DenyWrite = true)]
		public string Output { get; set; }
		/// <summary>
		/// Exit code of DM
		/// </summary>
		[Permissions(DenyWrite = true)]
		public int? ExitCode { get; set; }
		/// <summary>
		/// Git revision the compiler ran on. Not modifiable
		/// </summary>
		[Permissions(DenyWrite = true)]
		public string Revision { get; set; }
		/// <summary>
		/// Git revision of the origin branch the compiler ran on. Not modifiable
		/// </summary>
		[Permissions(DenyWrite = true)]
		public string OriginRevision { get; set; }

		/// <summary>
		/// How often the <see cref="DreamMaker"/> automatically compiles in minutes
		/// </summary>
		[Permissions(WriteRight = DreamMakerRights.SetAutoCompile)]
		public int? AutoCompileInterval { get; set; }
	}
}
