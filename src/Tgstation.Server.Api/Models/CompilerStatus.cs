namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Status of the <see cref="DreamMaker"/> for an <see cref="Instance"/>
	/// </summary>
#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names
	public enum CompilerStatus
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names
	{
		/// <summary>
		/// The <see cref="DreamMaker"/> is idle
		/// </summary>
		Idle,
		/// <summary>
		/// The <see cref="Repository"/> is being copied
		/// </summary>
		Copying,
		/// <summary>
		/// Pre-compile scripts are running
		/// </summary>
		PreCompile,
		/// <summary>
		/// The .dme is having it's server side modifications applied
		/// </summary>
		Modifying,
		/// <summary>
		/// DreamMaker is running
		/// </summary>
		Compiling,
		/// <summary>
		/// The DMAPI is being verified
		/// </summary>
		Verifying,
		/// <summary>
		/// Post-compile scripts are running
		/// </summary>
		PostCompile,
		/// <summary>
		/// The compile results are being duplicated
		/// </summary>
		Duplicating,
		/// <summary>
		/// The configuration is being linked to the compile results
		/// </summary>
		Symlinking,
		/// <summary>
		/// A failed compile job is being erased
		/// </summary>
		Cleanup
	}
}