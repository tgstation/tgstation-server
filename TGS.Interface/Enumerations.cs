using System;

namespace TGS.Interface
{
	/// <summary>
	/// The status of a BYOND update job
	/// </summary>
	public enum ByondStatus
	{
		/// <summary>
		/// No byond update in progress
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
		/// <summary>
		/// Running game code is being recompiled under staged update
		/// </summary>
		CompilingStaged,
	}
	/// <summary>
	/// Type of byond version
	/// </summary>
	public enum ByondVersion
	{
		/// <summary>
		/// The highest version from http://www.byond.com/download/build/LATEST/
		/// </summary>
		Latest,
		/// <summary>
		/// The version in the staging directory
		/// </summary>
		Staged,
		/// <summary>
		/// The installed version
		/// </summary>
		Installed,
	}
	/// <summary>
	/// The type of chat provider
	/// </summary>
	public enum ChatProvider : int
	{
		/// <summary>
		/// IRC chat provider
		/// </summary>
		IRC = 0,
		/// <summary>
		/// Discord chat provider
		/// </summary>
		Discord = 1,
	}

	/// <summary>
	/// Supported irc permission modes
	/// </summary>
	public enum IRCMode : int
	{
		/// <summary>
		/// +
		/// </summary>
		Voice,
		/// <summary>
		/// %
		/// </summary>
		Halfop,
		/// <summary>
		/// @
		/// </summary>
		Op,
		/// <summary>
		/// ~
		/// </summary>
		Owner,
	}
	/// <summary>
	/// The status of the compiler
	/// </summary>
	public enum CompilerStatus
	{
		/// <summary>
		/// Game folder is broken or does not exist
		/// </summary>
		Uninitialized,
		/// <summary>
		/// Game folder is being created
		/// </summary>
		Initializing,
		/// <summary>
		/// Game folder is setup, does not imply the dmb is compiled
		/// </summary>
		Initialized,
		/// <summary>
		/// Game is being compiled
		/// </summary>
		Compiling,
	}

	/// <summary>
	/// The status of the DD instance
	/// </summary>
	public enum DreamDaemonStatus
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
	public enum DreamDaemonSecurity
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
}
