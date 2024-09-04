using System;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Base class for DreamDaemon API models.
	/// </summary>
	public abstract class DreamDaemonApiBase : DreamDaemonSettings
	{
		/// <summary>
		/// An incrementing ID for representing current server execution.
		/// </summary>
		[ResponseOptions]
		public long? SessionId { get; set; }

		/// <summary>
		/// When the current server execution was started.
		/// </summary>
		[ResponseOptions]
		public DateTimeOffset? LaunchTime { get; set; }

		/// <summary>
		/// The last known count of connected players. Requires <see cref="DreamDaemonLaunchParameters.HealthCheckSeconds"/> to not be 0 and a game server interop version >= 5.10.0 to populate.
		/// </summary>
		[ResponseOptions]
		public uint? ClientCount { get; set; }

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
	}
}
