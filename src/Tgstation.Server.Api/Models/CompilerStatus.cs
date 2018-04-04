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
        /// The directiory is being targeted
        /// </summary>
		Targeting,
        /// <summary>
        /// The <see cref="Repository"/> is being copied
        /// </summary>
        Copying,
        /// <summary>
        /// DreamMaker is running
        /// </summary>
        Compiling
	}
}