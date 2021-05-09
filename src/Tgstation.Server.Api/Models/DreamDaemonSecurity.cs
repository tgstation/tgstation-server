namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// DreamDaemon's security level.
	/// </summary>
	public enum DreamDaemonSecurity
	{
		/// <summary>
		/// Server is unrestricted in terms of file access and shell commands.
		/// </summary>
		Trusted,

		/// <summary>
		/// Server will not be able to run shell commands or access files outside it's working directory.
		/// </summary>
		Safe,

		/// <summary>
		/// Server will not be able to run shell commands or access anything but temporary files.
		/// </summary>
		Ultrasafe,
	}
}
