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
		/// <example>1</example>
		[ResponseOptions]
		public long? SessionId { get; set; }

		/// <summary>
		/// A incrementing ID for representing current iteration of servers world (i.e. after calling /world/proc/Reboot). Only unique within the current <see cref="SessionId"/>. Only tracked in game sessions with the DMAPI enabled.
		/// </summary>
		/// <example>1</example>
		[ResponseOptions]
		public long? WorldIteration { get; set; }

		/// <summary>
		/// When the current server execution was started.
		/// </summary>
		[ResponseOptions]
		public DateTimeOffset? LaunchTime { get; set; }

		/// <summary>
		/// The last known count of connected players. Requires <see cref="DreamDaemonLaunchParameters.HealthCheckSeconds"/> to not be 0 and a game server interop version >= 5.10.0 to populate.
		/// </summary>
		/// <example>30</example>
		[ResponseOptions]
		public uint? ClientCount { get; set; }

		/// <summary>
		/// If the server is undergoing a soft reset. This may be automatically set by changes to other fields.
		/// </summary>
		/// <example>false</example>
		[ResponseOptions]
		public bool? SoftRestart { get; set; }

		/// <summary>
		/// If the server is undergoing a soft shutdown.
		/// </summary>
		[ResponseOptions]
		public bool? SoftShutdown { get; set; }
	}
}
