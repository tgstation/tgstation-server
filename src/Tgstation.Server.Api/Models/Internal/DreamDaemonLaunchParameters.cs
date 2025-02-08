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
		/// If the BYOND web client can be used to connect to the game server. No-op for <see cref="EngineType.OpenDream"/>.
		/// </summary>
		/// <example>false</example>
		[Required]
		[ResponseOptions]
		public bool? AllowWebClient { get; set; }

		/// <summary>
		/// If -profile is passed in on the DreamDaemon command line. No-op for <see cref="EngineType.OpenDream"/>.
		/// </summary>
		[Required]
		[ResponseOptions]
		public bool? StartProfiler { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonVisibility"/> level of DreamDaemon. No-op for <see cref="EngineType.OpenDream"/>.
		/// </summary>
		/// <example>2</example>
		[Required]
		[ResponseOptions]
		[EnumDataType(typeof(DreamDaemonVisibility))]
		public DreamDaemonVisibility? Visibility { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of DreamDaemon. No-op for <see cref="EngineType.OpenDream"/>.
		/// </summary>
		/// <example>1</example>
		[Required]
		[ResponseOptions]
		[EnumDataType(typeof(DreamDaemonSecurity))]
		public DreamDaemonSecurity? SecurityLevel { get; set; }

		/// <summary>
		/// The port DreamDaemon uses. This should be publically accessible.
		/// </summary>
		/// <example>1337</example>
		[Required]
		[ResponseOptions]
		[Range(1, UInt16.MaxValue)]
		public ushort? Port { get; set; }

		/// <summary>
		/// The port used by <see cref="EngineType.OpenDream"/> for its topic port.
		/// </summary>
		/// <example>2337</example>
		[Required]
		[ResponseOptions]
		public ushort? OpenDreamTopicPort { get; set; }

		/// <summary>
		/// The DreamDaemon startup timeout in seconds.
		/// </summary>
		/// <example>5</example>
		[Required]
		[ResponseOptions]
		[Range(1, UInt32.MaxValue)]
		public uint? StartupTimeout { get; set; }

		/// <summary>
		/// The number of seconds between each watchdog health check. 0 disables.
		/// </summary>
		/// <example>5</example>
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
		/// <example>500</example>
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
		/// If DreamDaemon supports it, the value added as the -map-threads parameter. 0 uses the default BYOND value. No-op for <see cref="EngineType.OpenDream"/>.
		/// </summary>
		[Required]
		[ResponseOptions]
		public uint? MapThreads { get; set; }

		/// <summary>
		/// If minidumps should be taken instead of full dumps.
		/// </summary>
		[Required]
		[ResponseOptions]
		public bool? Minidumps { get; set; }

		/// <summary>
		/// Check if we match a given set of <paramref name="otherParameters"/>. <see cref="StartupTimeout"/> is excluded.
		/// </summary>
		/// <param name="otherParameters">The <see cref="DreamDaemonLaunchParameters"/> to compare against.</param>
		/// <param name="engineType">The <see cref="EngineType"/> currently running.</param>
		/// <returns><see langword="true"/> if they match, <see langword="false"/> otherwise.</returns>
		public bool CanApplyWithoutReboot(DreamDaemonLaunchParameters otherParameters, EngineType engineType)
		{
			if (otherParameters == null)
				throw new ArgumentNullException(nameof(otherParameters));

			return AllowWebClient == otherParameters.AllowWebClient
				&& SecurityLevel == otherParameters.SecurityLevel
				&& Visibility == otherParameters.Visibility
				&& Port == otherParameters.Port
				&& (OpenDreamTopicPort == otherParameters.OpenDreamTopicPort || engineType != EngineType.OpenDream)
				&& TopicRequestTimeout == otherParameters.TopicRequestTimeout
				&& AdditionalParameters == otherParameters.AdditionalParameters
				&& StartProfiler == otherParameters.StartProfiler
				&& LogOutput == otherParameters.LogOutput
				&& MapThreads == otherParameters.MapThreads; // We intentionally don't check StartupTimeout, Minidumps, health check seconds, or health check dump as they don't matter in terms of the watchdog
		}
	}
}
