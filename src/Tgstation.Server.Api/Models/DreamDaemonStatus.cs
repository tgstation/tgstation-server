namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// The status of <see cref="DreamDaemon"/>
	/// </summary>
#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names
	public enum DreamDaemonStatus
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names
	{
		/// <summary>
		/// Server is not running
		/// </summary>
		Offline,

		/// <summary>
		/// Server is being rebooted
		/// </summary>
		HardRebooting,

		/// <summary>
		/// Server is running
		/// </summary>
		Online,
	}
}