using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
    /// <summary>
    /// Represents the state of the DreamMaker compiler. Create action starts a new compile. Delete action cancels the current compile
    /// </summary>
	[Model(RightsType.DreamMaker, ReadRight = DreamMakerRights.Read, CanCrud = true, RequiresInstance = true)]
    public sealed class DreamMaker : DreamMakerSettings
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
	}
}
