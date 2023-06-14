using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Base class for DreamDaemon API models.
	/// </summary>
	public abstract class DreamDaemonApiBase : DreamDaemonSettings
	{
		/// <summary>
		/// If the server is undergoing a soft reset. This may be automatically set by changes to other fields.
		/// </summary>
		[ResponseOptions]
		public bool? SoftRestart { get; set; }

		/// <summary>
		/// If the server is undergoing a soft shutdown.
		/// </summary>
		[ResponseOptions]
		public bool? SoftShutdown { get; set; }

		/// <summary>
		/// Deprecated, use <see cref="DreamDaemonLaunchParameters.HealthCheckSeconds"/>.
		/// </summary>
		[Required]
		[ResponseOptions]
		[Obsolete("Use HealthCheckSeconds")]
		public uint? HeartbeatSeconds { get; set; }

		/// <summary>
		/// Deprecated, use <see cref="DreamDaemonLaunchParameters.DumpOnHealthCheckRestart"/>.
		/// </summary>
		[Required]
		[ResponseOptions]
		[Obsolete("Use DumpOnHealthCheckRestart")]
		public bool? DumpOnHeartbeatRestart { get; set; }
	}
}
