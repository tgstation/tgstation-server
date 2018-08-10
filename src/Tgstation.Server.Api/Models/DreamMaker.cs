using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
    /// <summary>
    /// Represents the state of the DreamMaker compiler. Create action starts a new compile. Delete action cancels the current compile
    /// </summary>
    public sealed class DreamMaker : DreamMakerSettings
	{
		/// <summary>
		/// The last <see cref="CompileJob"/> ran
		/// </summary>
		[Permissions(DenyWrite = true)]
		public CompileJob LastJob { get; set; }

		/// <summary>
		/// The <see cref="CompilerStatus"/> of the compiler
		/// </summary>
		[Permissions(DenyWrite = true)]
		public CompilerStatus Status { get; set; }
	}
}
