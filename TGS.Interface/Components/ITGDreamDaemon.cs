using System.Threading.Tasks;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Interface for managing the actual BYOND game server
	/// </summary>
	public interface ITGDreamDaemon : ITGComponent
	{
		/// <summary>
		/// Gets the status of DreamDaemon
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in the appropriate <see cref="DreamDaemonStatus"/></returns>
		Task<DreamDaemonStatus> DaemonStatus();

		/// <summary>
		/// Returns a human readable string of the current server status
		/// </summary>
		/// <param name="includeMetaInfo">If <see langword="true"/>, the status will include the server's current visibility and security levels</param>
		/// <returns>A <see cref="Task"/> resulting in a human readable <see cref="string"/> of the current server status</returns>
		Task<string> StatusString(bool includeMetaInfo);

		/// <summary>
		/// Starts the server if it isn't running
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in <see langword="null"/> on success or error message on failure</returns>
		Task<string> Start();

		/// <summary>
		/// Immediately kills the server
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in <see langword="null"/> on success or error message on failure</returns>
		Task<string> Stop();

		/// <summary>
		/// Immediately kills and restarts the server
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in <see langword="null"/> on success or error message on failure</returns>
		Task<string> Restart();

		/// <summary>
		/// Restart the server after the currently running world reboots. Has no effect if the server isn't running
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the operation</returns>
		Task RequestRestart();

		/// <summary>
		/// Stop the server after the currently running world reboots.  Has no effect if the server isn't running
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the operation</returns>
		Task RequestStop();

		/// <summary>
		/// Get the configured (not necessarily running) security level
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in the configured (not necessarily running) <see cref="DreamDaemonSecurity"/></returns>
		Task<DreamDaemonSecurity> SecurityLevel();

		/// <summary>
		/// Sets the security level of the server. Requires server reboot to apply. Calls <see cref="RequestRestart"/>. Note that anything higher than Trusted will disable interop from DD
		/// </summary>
		/// <param name="level">The new security level</param>
		/// <returns>A <see cref="Task"/> resulting in <see langword="true"/> if the change was immediately applied, <see langword="false"/> otherwise and a call to <see cref="RequestRestart"/> was made</returns>
		Task<bool> SetSecurityLevel(DreamDaemonSecurity level);

		/// <summary>
		/// Get the configured port. Not necessarily the running port if it has since changed
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in the configured port</returns>
		Task<ushort> Port();

		/// <summary>
		/// Set the port to host DD on. Requires reboot to apply. Calls <see cref="RequestRestart"/>.
		/// </summary>
		/// <param name="new_port">The new port</param>
		/// <returns>A <see cref="Task"/> representing the operation</returns>
		Task SetPort(ushort new_port);

		/// <summary>
		/// Check if the watchdog will start when the service starts
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in <see langword="true"/> if autostart is enabled, <see langword="false"/> otherwise</returns>
		Task<bool> Autostart();

		/// <summary>
		/// Set the autostart config
		/// </summary>
		/// <param name="on">A <see cref="Task"/> resulting in <see langword="true"/> to start the watchdog with the service, <see langword="false"/> to disable that functionality</param>
		Task SetAutostart(bool on);

		/// <summary>
		/// Check if the BYOND webclient is currently enabled for the server
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in <see langword="true"/> if the webclient is enabled, <see langword="false"/> otherwise</returns>
		Task<bool> Webclient();

		/// <summary>
		/// Set the webclient config. Calls <see cref="RequestRestart"/>
		/// </summary>
		/// <param name="on">A <see cref="Task"/> resulting in <see langword="true"/> to enable the byond webclient for the server, <see langword="false"/> otherwise</param>
		Task SetWebclient(bool on);

		/// <summary>
		/// Checks if a server stop has been requested
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in <see langword="true"/> if <see cref="RequestStop"/> has been called since the last server start, <see langword="false"/> otherwise</returns>
		Task<bool> ShutdownInProgress();

		/// <summary>
		/// Sends a message to everyone on the server
		/// </summary>
		/// <param name="msg">The message to send</param>
		/// <returns>A <see cref="Task"/> resulting in <see langword="null"/> on success, error message on failure</returns>
		Task<string> WorldAnnounce(string msg);

		/// <summary>
		/// Returns the number of connected players
		/// </summary>
		/// <returns>A <see cref="Task"/> resulting in the number of connected players or -1 on error</returns>
		Task<int> PlayerCount();
	}
}
