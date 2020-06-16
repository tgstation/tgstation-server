using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Launch settings for DreamDaemon
	/// </summary>
	public class DreamDaemonLaunchParameters
	{
		/// <summary>
		/// If the BYOND web client can be used to connect to the game server
		/// </summary>
		[Required]
		public bool? AllowWebClient { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level of <see cref="DreamDaemon"/>
		/// </summary>
		[Required]
		[EnumDataType(typeof(DreamDaemonSecurity))]
		public DreamDaemonSecurity? SecurityLevel { get; set; }

		/// <summary>
		/// The first port <see cref="DreamDaemon"/> uses. This should be the publically advertised port
		/// </summary>
		[Required]
		[Range(1, UInt16.MaxValue)]
		public ushort? PrimaryPort { get; set; }

		/// <summary>
		/// The second port <see cref="DreamDaemon"/> uses
		/// </summary>
		[Required]
		[Range(1, UInt16.MaxValue)]
		public ushort? SecondaryPort { get; set; }

		/// <summary>
		/// The DreamDaemon startup timeout in seconds
		/// </summary>
		[Required]
		[Range(1, UInt32.MaxValue)]
		public uint? StartupTimeout { get; set; }

		/// <summary>
		/// The number of seconds between each watchdog heartbeat. 0 disables.
		/// </summary>
		[Required]
		public uint? HeartbeatSeconds { get; set; }

		/// <summary>
		/// The timeout for sending and receiving BYOND topics in milliseconds.
		/// </summary>
		[Required]
		[Range(1, UInt32.MaxValue)]
		public uint? TopicRequestTimeout { get; set; }

		/// <summary>
		/// Check if we match a given set of <paramref name="otherParameters"/>. <see cref="StartupTimeout"/> is excluded.
		/// </summary>
		/// <param name="otherParameters">The <see cref="DreamDaemonLaunchParameters"/> to compare against</param>
		/// <returns><see langword="true"/> if they match, <see langword="false"/> otherwise</returns>
		public bool CanApplyWithoutReboot(DreamDaemonLaunchParameters otherParameters) =>
			AllowWebClient == (otherParameters?.AllowWebClient ?? throw new ArgumentNullException(nameof(otherParameters)))
				&& SecurityLevel == otherParameters.SecurityLevel
				&& PrimaryPort == otherParameters.PrimaryPort
				&& SecondaryPort == otherParameters.SecondaryPort
				&& TopicRequestTimeout == otherParameters.TopicRequestTimeout; // We intentionally don't check StartupTimeout or heartbeat seconds as it doesn't matter in terms of the watchdog
	}
}