using System;

namespace Tgstation.Server.Api.Models
{
    /// <summary>
    /// Represents the state of the DreamMaker compiler
    /// </summary>
    public sealed class Compiler
    {
        /// <summary>
        /// When the compilation started
        /// </summary>
        public DateTimeOffset StartedAt { get; set; }
        /// <summary>
        /// When the compilation finished
        /// </summary>
        public DateTimeOffset FinishedAt { get; set; }
        /// <summary>
        /// The <see cref="CompilerStatus"/> of the compiler
        /// </summary>
        public CompilerStatus Status { get; set; }
        /// <summary>
        /// If the compiler targeted the primary directory
        /// </summary>
        public bool? TargetedPrimaryDirectory { get; set; }
        /// <summary>
        /// Textual output of DM
        /// </summary>
        public string Output { get; set; }
        /// <summary>
        /// Exit code of DM
        /// </summary>
        public int? ExitCode { get; set; }
		/// <summary>
		/// Git revision the compiler ran on. Not modifiable
		/// </summary>
		public string Revision { get; set; }
        /// <summary>
        /// Git revision of the origin branch the compiler ran on. Not modifiable
        /// </summary>
        public string OriginRevision { get; set; }

		/// <summary>
		/// How often the <see cref="Compiler"/> automatically compiles in minutes
		/// </summary>
		public int? AutoCompileInterval { get; set; }
	}
}
