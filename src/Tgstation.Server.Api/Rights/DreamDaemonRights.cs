using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for managing DreamDaemon.
	/// </summary>
	[Flags]
	public enum DreamDaemonRights : ulong
	{
		/// <summary>
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User can read <see cref="Models.Response.DreamDaemonResponse.ActiveCompileJob"/> and <see cref="Models.Response.DreamDaemonResponse.StagedCompileJob"/>.
		/// </summary>
		ReadRevision = 1 << 0,

		/// <summary>
		/// User can change the port DreamDaemon runs on.
		/// </summary>
		SetPort = 1 << 1,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonSettings.AutoStart"/>.
		/// </summary>
		SetAutoStart = 1 << 2,

		/// <summary>
		/// User set <see cref="Models.Internal.DreamDaemonLaunchParameters.SecurityLevel"/>.
		/// </summary>
		SetSecurity = 1 << 3,

		/// <summary>
		/// User can read every property of <see cref="Models.Response.DreamDaemonResponse"/> except <see cref="Models.Response.DreamDaemonResponse.ActiveCompileJob"/> and <see cref="Models.Response.DreamDaemonResponse.StagedCompileJob"/>.
		/// </summary>
		ReadMetadata = 1 << 4,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.AllowWebClient"/>.
		/// </summary>
		SetWebClient = 1 << 5,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonApiBase.SoftRestart"/>.
		/// </summary>
		SoftRestart = 1 << 6,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonApiBase.SoftShutdown"/>.
		/// </summary>
		SoftShutdown = 1 << 7,

		/// <summary>
		/// User can immediately restart the Watchdog.
		/// </summary>
		Restart = 1 << 8,

		/// <summary>
		/// User can immediately shutdown the Watchdog.
		/// </summary>
		Shutdown = 1 << 9,

		/// <summary>
		/// User can start the Watchdog.
		/// </summary>
		Start = 1 << 10,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.StartupTimeout"/>.
		/// </summary>
		SetStartupTimeout = 1 << 11,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.HealthCheckSeconds"/>
		/// </summary>
		SetHealthCheckInterval = 1 << 12,

		/// <summary>
		/// User can create DreamDaemon process dumps or change <see cref="Models.Internal.DreamDaemonLaunchParameters.DumpOnHealthCheckRestart"/>.
		/// </summary>
		CreateDump = 1 << 13,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.TopicRequestTimeout"/>.
		/// </summary>
		SetTopicTimeout = 1 << 14,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.AdditionalParameters"/>.
		/// </summary>
		SetAdditionalParameters = 1 << 15,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.Visibility"/>.
		/// </summary>
		SetVisibility = 1 << 16,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.StartProfiler"/>.
		/// </summary>
		SetProfiler = 1 << 17,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.LogOutput"/>.
		/// </summary>
		SetLogOutput = 1 << 18,

		/// <summary>
		/// User can change <see cref="Models.Internal.DreamDaemonLaunchParameters.MapThreads"/>.
		/// </summary>
		SetMapThreads = 1 << 19,

		/// <summary>
		/// User can use <see cref="Models.Request.DreamDaemonRequest.BroadcastMessage"/>.
		/// </summary>
		BroadcastMessage = 1 << 20,
	}
}
