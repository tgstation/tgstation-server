using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.DreamDaemon"/>
	/// </summary>
	[Flags]
	public enum DreamDaemonRights : ulong
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,

		/// <summary>
		/// User can read <see cref="Models.DreamDaemon.ActiveCompileJob"/> and <see cref="Models.DreamDaemon.StagedCompileJob"/>
		/// </summary>
		ReadRevision = 1,

		/// <summary>
		/// User can change the port DreamDaemon runs on.
		/// </summary>
		SetPort = 2,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonSettings.AutoStart"/>
		/// </summary>
		SetAutoStart = 4,

		/// <summary>
		/// User set <see cref="Models.Internal.DreamDaemonLaunchParameters.SecurityLevel"/>
		/// </summary>
		SetSecurity = 8,

		/// <summary>
		/// User can read every propery of <see cref="Models.DreamDaemon"/> except <see cref="Models.DreamDaemon.ActiveCompileJob"/> and <see cref="Models.DreamDaemon.StagedCompileJob"/>.
		/// </summary>
		ReadMetadata = 16,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.AllowWebClient"/>
		/// </summary>
		SetWebClient = 32,

		/// <summary>
		/// User can change <see cref="Models.DreamDaemon.SoftRestart"/>.
		/// </summary>
		SoftRestart = 64,

		/// <summary>
		/// User can change <see cref="Models.DreamDaemon.SoftShutdown"/>.
		/// </summary>
		SoftShutdown = 128,

		/// <summary>
		/// User can immediately restart the Watchdog.
		/// </summary>
		Restart = 256,

		/// <summary>
		/// User can immediately shutdown the Watchdog.
		/// </summary>
		Shutdown = 512,

		/// <summary>
		/// User can start the Watchdog.
		/// </summary>
		Start = 1024,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.StartupTimeout"/>
		/// </summary>
		SetStartupTimeout = 2048,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.HeartbeatSeconds"/>
		/// </summary>
		SetHeartbeatInterval = 4096,

		/// <summary>
		/// User can create DreamDaemon process dumps.
		/// </summary>
		CreateDump = 8192,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.TopicRequestTimeout"/>.
		/// </summary>
		SetTopicTimeout = 16384,
	}
}
