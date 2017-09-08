using System.ServiceModel;

namespace TGServiceInterface
{
	/// <summary>
	/// The status of the DD instance
	/// </summary>
	public enum TGDreamDaemonStatus
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

	/// <summary>
	/// DreamDaemon's security level
	/// </summary>
	public enum TGDreamDaemonSecurity
	{
		/// <summary>
		/// Server is unrestricted in terms of file access and shell commands
		/// </summary>
		Trusted = 0,
		/// <summary>
		/// Server will not be able to run shell commands or access files outside it's working directory
		/// </summary>
		Safe,
		/// <summary>
		/// Server will not be able to run shell commands or access anything but temporary files
		/// </summary>
		Ultrasafe
	}
	
	/// <summary>
	/// Interface for managing the actual BYOND game server
	/// </summary>
	[ServiceContract]
	public interface ITGDreamDaemon
	{
		/// <summary>
		/// Gets the status of DreamDaemon
		/// </summary>
		/// <returns>The appropriate TGDreamDaemonStatus</returns>
		[OperationContract]
		TGDreamDaemonStatus DaemonStatus();

		/// <summary>
		/// Returns a human readable string of the current server status
		/// </summary>
		/// <param name="includeMetaInfo">If true, the status will include the server's current visibility and security levels</param>
		/// <returns>A human readable string of the current server status</returns>
		[OperationContract]
		string StatusString(bool includeMetaInfo);

		/// <summary>
		/// Check if a call to Start will fail
		/// Of course, be aware of race conditions with other control panels
		/// </summary>
		/// <returns>returns the error that would occur, null otherwise</returns>
		[OperationContract]
		string CanStart();

		/// <summary>
		/// Starts the server if it isn't running
		/// </summary>
		/// <returns>null on success or error message on failure</returns>
		[OperationContract]
		string Start();

		/// <summary>
		/// Immediately kills the server
		/// </summary>
		/// <returns>null on success or error message on failure</returns>
		[OperationContract]
		string Stop();

		/// <summary>
		/// Immediately kills and restarts the server
		/// </summary>
		/// <returns>null on success or error message on failure</returns>
		[OperationContract]
		string Restart();

		/// <summary>
		/// Restart the server after the currently running round ends
		/// Has no effect if the server isn't running
		/// </summary>
		[OperationContract]
		void RequestRestart();

		/// <summary>
		/// Stop the server after the currently running round ends
		/// Has no effect if the server isn't running
		/// </summary>
		[OperationContract]
		void RequestStop();

		/// <summary>
		/// Get the configured (not running) security level
		/// </summary>
		/// <returns>The configured (not running) security level</returns>
		[OperationContract]
		TGDreamDaemonSecurity SecurityLevel();

		/// <summary>
		/// Sets the security level of the server. Requires reboot to apply
		/// Implies a call to RequestRestart()
		/// note that anything higher than Trusted will disable interop from DD
		/// </summary>
		/// <param name="level">The new security level</param>
		/// <returns>True if the change was immediately applied, false if a graceful restart was queued</returns>
		[OperationContract]
		bool SetSecurityLevel(TGDreamDaemonSecurity level);

		/// <summary>
		/// Get the configured port. Not necessarily the running port if it has since changed
		/// </summary>
		/// <returns>The configured port</returns>
		[OperationContract]
		ushort Port();

		/// <summary>
		/// Set the port to host DD on. Requires reboot to apply
		/// Implies a call to RequestRestart()
		/// </summary>
		/// <param name="new_port">The new port</param>
		[OperationContract]
		void SetPort(ushort new_port);

		/// <summary>
		/// Check if the watchdog will start when the service starts
		/// </summary>
		/// <returns>true if autostart is enabled, false otherwise</returns>
		[OperationContract]
		bool Autostart();

		/// <summary>
		/// Set the autostart config
		/// </summary>
		/// <param name="on">true to start the watchdog with the service, false otherwise</param>
		[OperationContract]
		void SetAutostart(bool on);

		/// <summary>
		/// Check if the byond webclient is currently enabled for the server
		/// </summary>
		/// <returns>true if the webclient is enabled, false otherwise</returns>
		[OperationContract]
		bool Webclient();

		/// <summary>
		/// Set the webclient config. Calls <see cref="RequestRestart"/>
		/// </summary>
		/// <param name="on">true to enable the byond webclient for the server, false otherwise</param>
		[OperationContract]
		void SetWebclient(bool on);

		/// <summary>
		/// Checks if a server stop has bee requested
		/// </summary>
		/// <returns>true if RequestStop has been called since the last server start, false otherwise</returns>
		[OperationContract]
		bool ShutdownInProgress();

		/// <summary>
		/// Sends a message to everyone on the server
		/// </summary>
		/// <param name="msg">The message to send</param>
		/// <returns>null on success, error message on failure</returns>
		[OperationContract]
		string WorldAnnounce(string msg);
	}
}
