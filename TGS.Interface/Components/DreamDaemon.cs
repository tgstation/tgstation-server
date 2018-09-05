using System.ServiceModel;

namespace TGS.Interface.Components
{
	
	/// <summary>
	/// Interface for managing the actual BYOND game server
	/// </summary>
	[ServiceContract]
	public interface ITGDreamDaemon
	{
		/// <summary>
		/// Gets the status of DreamDaemon
		/// </summary>
		/// <returns>The appropriate <see cref="DreamDaemonStatus"/></returns>
		[OperationContract]
		DreamDaemonStatus DaemonStatus();

		/// <summary>
		/// Returns a human readable string of the current server status
		/// </summary>
		/// <param name="includeMetaInfo">If <see langword="true"/>, the status will include the server's current visibility and security levels</param>
		/// <returns>A human readable <see cref="string"/> of the current server status</returns>
		[OperationContract]
		string StatusString(bool includeMetaInfo);

		/// <summary>
		/// Check if a call to <see cref="Start"/> will fail. Of course, be aware of race conditions with other interfaces
		/// </summary>
		/// <returns>The error that would occur, <see langword="null"/> otherwise</returns>
		[OperationContract]
		string CanStart();

		/// <summary>
		/// Starts the server if it isn't running
		/// </summary>
		/// <returns><see langword="null"/> on success or error message on failure</returns>
		[OperationContract]
		string Start();

		/// <summary>
		/// Immediately kills the server
		/// </summary>
		/// <returns><see langword="null"/> on success or error message on failure</returns>
		[OperationContract]
		string Stop();

		/// <summary>
		/// Immediately kills and restarts the server
		/// </summary>
		/// <returns><see langword="null"/> on success or error message on failure</returns>
		[OperationContract]
		string Restart();

		/// <summary>
		/// Restart the server after the currently running world reboots. Has no effect if the server isn't running
		/// </summary>
		[OperationContract]
		void RequestRestart();

		/// <summary>
		/// Stop the server after the currently running world reboots.  Has no effect if the server isn't running
		/// </summary>
		[OperationContract]
		void RequestStop();

		/// <summary>
		/// Get the configured (not necessarily running) security level
		/// </summary>
		/// <returns>The configured (not necessarily running) <see cref="DreamDaemonSecurity"/></returns>
		[OperationContract]
		DreamDaemonSecurity SecurityLevel();

		/// <summary>
		/// Sets the security level of the server. Requires server reboot to apply. Calls <see cref="RequestRestart"/>. Note that anything higher than Trusted will disable interop from DD
		/// </summary>
		/// <param name="level">The new security level</param>
		/// <returns><see langword="true"/> if the change was immediately applied, <see langword="false"/> otherwise and a call to <see cref="RequestRestart"/> was made</returns>
		[OperationContract]
		bool SetSecurityLevel(DreamDaemonSecurity level);

		/// <summary>
		/// Get the configured port. Not necessarily the running port if it has since changed
		/// </summary>
		/// <returns>The configured port</returns>
		[OperationContract]
		ushort Port();

		/// <summary>
		/// Set the port to host DD on. Requires reboot to apply. Calls <see cref="RequestRestart"/>.
		/// </summary>
		/// <param name="new_port">The new port</param>
		[OperationContract]
		void SetPort(ushort new_port);

		/// <summary>
		/// Check if the watchdog will start when the service starts
		/// </summary>
		/// <returns><see langword="true"/> if autostart is enabled, <see langword="false"/> otherwise</returns>
		[OperationContract]
		bool Autostart();

		/// <summary>
		/// Set the autostart config
		/// </summary>
		/// <param name="on"><see langword="true"/> to start the watchdog with the service, <see langword="false"/> to disable that functionality</param>
		[OperationContract]
		void SetAutostart(bool on);

		/// <summary>
		/// Check if the BYOND webclient is currently enabled for the server
		/// </summary>
		/// <returns><see langword="true"/> if the webclient is enabled, <see langword="false"/> otherwise</returns>
		[OperationContract]
		bool Webclient();

		/// <summary>
		/// Set the webclient config. Calls <see cref="RequestRestart"/>
		/// </summary>
		/// <param name="on"><see langword="true"/> to enable the byond webclient for the server, <see langword="false"/> otherwise</param>
		[OperationContract]
		void SetWebclient(bool on);

		/// <summary>
		/// Checks if a server stop has been requested
		/// </summary>
		/// <returns><see langword="true"/> if <see cref="RequestStop"/> has been called since the last server start, <see langword="false"/> otherwise</returns>
		[OperationContract]
		bool ShutdownInProgress();

		/// <summary>
		/// Sends a message to everyone on the server
		/// </summary>
		/// <param name="msg">The message to send</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string WorldAnnounce(string msg);

		/// <summary>
		/// Returns the number of connected players. Requires game to use API version >= 3.1.0.1
		/// </summary>
		/// <returns>The number of connected players or -1 on error</returns>
		[OperationContract]
		int PlayerCount();

		/// <summary>
		/// Creates a full memory minidump of the current DD process under Diagnostics/Minidumps
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string CreateMinidump();
	}
}
