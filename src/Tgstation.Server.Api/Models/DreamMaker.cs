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
		/// The last <see cref="CompileJob"/> ran
		/// </summary>
		public CompileJob LastJob { get; set; }

		/// <summary>
		/// The <see cref="CompilerStatus"/> of the compiler
		/// </summary>
		[Permissions(DenyWrite = true)]
		public CompilerStatus Status { get; set; }

		/// <summary>
		/// How often the <see cref="DreamMaker"/> automatically compiles in minutes
		/// </summary>
		[Permissions(WriteRight = DreamMakerRights.SetAutoCompile)]
		public int? AutoCompileInterval { get; set; }

		/// <summary>
		/// The .dme file <see cref="DreamMaker"/> tries to compile with
		/// </summary>
		[Permissions(WriteRight = DreamMakerRights.SetDme)]
		public string TargetDme { get; set; }
	}
}
