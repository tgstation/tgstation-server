using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.DreamDaemon"/>
	/// </summary>
	[Flags]
	public enum DreamDaemonRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// User can read <see cref="Models.DreamDaemon.PullRequests"/>, <see cref="Models.DreamDaemon.Sha"/>, and <see cref="Models.DreamDaemon.OriginSha"/>
		/// </summary>
		ReadRevision = 1,
		/// <summary>
		/// User can change both primary and secondary ports
		/// </summary>
		SetPorts = 2,
		/// <summary>
		/// User can change <see cref="Models.DreamDaemon.AutoStart"/>
		/// </summary>
		SetAutoStart = 4,
		/// <summary>
		/// User set <see cref="Models.DreamDaemon.SecurirtyLevel"/>
		/// </summary>
		SetSecurity = 8,
		/// <summary>
		/// User can read all ports, <see cref="Models.DreamDaemon.SoftRestart"/>, <see cref="Models.DreamDaemon.SoftShutdown"/>, <see cref="Models.DreamDaemon.Status"/>, <see cref="Models.DreamDaemon.AllowWebClient"/>, and <see cref="Models.DreamDaemon.AutoStart"/>
		/// </summary>
		ReadMetadata = 16,
		/// <summary>
		/// User can change <see cref="Models.DreamDaemon.AllowWebClient"/>
		/// </summary>
		SetWebClient = 32,
		/// <summary>
		/// User can enable <see cref="Models.DreamDaemon.SoftRestart"/>
		/// </summary>
		SoftRestart = 64,
		/// <summary>
		/// User can enable <see cref="Models.DreamDaemon.SoftShutdown"/>
		/// </summary>
		SoftShutdown = 128,
		/// <summary>
		/// User can immediately restart <see cref="Models.DreamDaemon"/>
		/// </summary>
		Restart = 256,
		/// <summary>
		/// User can immediately shutdown <see cref="Models.DreamDaemon"/>
		/// </summary>
		Shutdown = 512,
		/// <summary>
		/// User can start <see cref="Models.DreamDaemon"/> and disable <see cref="Models.DreamDaemon.SoftRestart"/> and <see cref="Models.DreamDaemon.SoftShutdown"/>
		/// </summary>
		Start = 1024
	}
}
