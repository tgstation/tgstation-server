using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Launch settings for DreamDaemon.
	/// </summary>
	public class DreamDaemonLaunchParameters
	{
		/// <summary>
		/// If the BYOND web client can be used to connect to the game server.
		/// </summary>
		[Required]
		[ResponseOptions]
		public bool? AllowWebClient { get; set; }

		/// <summary>
		/// If -profile is passed in on the DreamDaemon command line.
		/// </summary>
		[Required]
		[ResponseOptions]
		public bool? StartProfiler { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonVisibility"/> level of DreamDaemon.
		/// </summary>
		[Required]
		[ResponseOptions]
		[EnumDataType(typeof(DreamDaemonVisibility))]
		public DreamDaemonVisibility? Visibility { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of DreamDaemon.
		/// </summary>
		[Required]
		[ResponseOptions]
		[EnumDataType(typeof(DreamDaemonSecurity))]
		public DreamDaemonSecurity? SecurityLevel { get; set; }

		/// <summary>
		/// The port DreamDaemon uses. This should be publically accessible.
		/// </summary>
		[Required]
		[ResponseOptions]
		[Range(1, UInt16.MaxValue)]
		public ushort? Port { get; set; }

		/// <summary>
		/// The DreamDaemon startup timeout in seconds.
		/// </summary>
		[Required]
		[ResponseOptions]
		[Range(1, UInt32.MaxValue)]
		public uint? StartupTimeout { get; set; }

		/// <summary>
		/// The number of seconds between each watchdog health check. 0 disables.
		/// </summary>
		[Required]
		[ResponseOptions]
		public uint? HealthCheckSeconds { get; set; }

		/// <summary>
		/// If a process core dump should be created prior to restarting the watchdog due to health check failure.
		/// </summary>
		[Required]
		[ResponseOptions]
		public bool? DumpOnHealthCheckRestart { get; set; }

		/// <summary>
		/// The timeout for sending and receiving BYOND topics in milliseconds.
		/// </summary>
		[Required]
		[ResponseOptions]
		[Range(1, UInt32.MaxValue)]
		public uint? TopicRequestTimeout { get; set; }

		/// <summary>
		/// Parameters string for DreamDaemon.
		/// </summary>
		[Required]
		[ResponseOptions]
		[StringLength(Limits.MaximumStringLength)]
		public string? AdditionalParameters { get; set; }

		/// <summary>
		/// If process output/error text should be logged.
		/// </summary>
		[Required]
		[ResponseOptions]
		public bool? LogOutput { get; set; }

		/// <summary>
		/// Check if we match a given set of <paramref name="otherParameters"/>. <see cref="StartupTimeout"/> is excluded.
		/// </summary>
		/// <param name="otherParameters">The <see cref="DreamDaemonLaunchParameters"/> to compare against.</param>
		/// <returns><see langword="true"/> if they match, <see langword="false"/> otherwise.</returns>
		public bool CanApplyWithoutReboot(DreamDaemonLaunchParameters otherParameters)
		{
			if (otherParameters == null)
				throw new ArgumentNullException(nameof(otherParameters));

			return AllowWebClient == otherParameters.AllowWebClient
				&& SecurityLevel == otherParameters.SecurityLevel
				&& Visibility == otherParameters.Visibility
				&& Port == otherParameters.Port
				&& TopicRequestTimeout == otherParameters.TopicRequestTimeout
				&& AdditionalParameters == otherParameters.AdditionalParameters
				&& StartProfiler == otherParameters.StartProfiler
				&& LogOutput == otherParameters.LogOutput; // We intentionally don't check StartupTimeout, health check seconds, or health check dump as they don't matter in terms of the watchdog
		}
	}
}
