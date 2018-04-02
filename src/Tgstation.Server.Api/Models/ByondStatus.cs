namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// The status of a BYOND update job
	/// </summary>
#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names
	public enum ByondStatus
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names
	{
		/// <summary>
		/// No update in progress
		/// </summary>
		Idle,
		/// <summary>
		/// Preparing to update
		/// </summary>
		Starting,
		/// <summary>
		/// Revision is downloading
		/// </summary>
		Downloading,
		/// <summary>
		/// Revision is deflating
		/// </summary>
		Staging,
		/// <summary>
		/// Revision is ready and waiting for DreamDaemon reboot
		/// </summary>
		Staged,
		/// <summary>
		/// Revision is being applied
		/// </summary>
		Updating,
	}
}
