using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
    /// <summary>
    /// Represents the state of the DreamMaker compiler. Create action starts a new compile. Delete action cancels the current compile
    /// </summary>
    public sealed class DreamMaker : DreamMakerSettings
	{
		/// <summary>
		/// The <see cref="CompilerStatus"/> of the compiler
		/// </summary>
		public CompilerStatus Status { get; set; }
	}
}
