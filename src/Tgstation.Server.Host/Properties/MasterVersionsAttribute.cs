using System;
using System.Reflection;

namespace Tgstation.Server.Host.Properties
{
	/// <summary>
	/// Attribute for bringing in the master versions list from MSBuild that aren't embedded into assemblies by default.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	sealed class MasterVersionsAttribute : Attribute
	{
		/// <summary>
		/// Return the <see cref="Assembly"/>'s instance of the <see cref="MasterVersionsAttribute"/>.
		/// </summary>
		public static MasterVersionsAttribute Instance => Assembly
			.GetExecutingAssembly()
			.GetCustomAttribute<MasterVersionsAttribute>()!;

		/// <summary>
		/// The <see cref="Version"/> <see cref="string"/> of the <see cref="Configuration"/> version built.
		/// </summary>
		public string RawConfigurationVersion { get; }

		/// <summary>
		/// The <see cref="Version"/> <see cref="string"/> of the DMAPI interop version used.
		/// </summary>
		public string RawInteropVersion { get; }

		/// <summary>
		/// The <see cref="Version"/> <see cref="string"/> of the control panel version built.
		/// </summary>
		public string RawWebpanelVersion { get; }

		/// <summary>
		/// The <see cref="Version"/> <see cref="string"/> of the control panel version built.
		/// </summary>
		public string RawHostWatchdogVersion { get; }

		/// <summary>
		/// The <see cref="Version"/> <see cref="string"/> of the MariaDB server bundled with TGS installs.
		/// </summary>
		public string RawMariaDBRedistVersion { get; }

		/// <summary>
		/// The <see cref="Version"/> <see cref="string"/> of the MariaDB server bundled with TGS installs.
		/// </summary>
		public string RawSwarmProtocolVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MasterVersionsAttribute"/> class.
		/// </summary>
		/// <param name="rawConfigurationVersion">The value of <see cref="RawConfigurationVersion"/>.</param>
		/// <param name="rawInteropVersion">The value of <see cref="RawInteropVersion"/>.</param>
		/// <param name="rawWebpanelVersion">The value of <see cref="RawWebpanelVersion"/>.</param>
		/// <param name="rawHostWatchdogVersion">The value of <see cref="RawHostWatchdogVersion"/>.</param>
		/// <param name="rawMariaDBRedistVersion">The value of <see cref="RawMariaDBRedistVersion"/>.</param>
		/// <param name="rawSwarmProtocolVersion">The value of <see cref="RawSwarmProtocolVersion"/>.</param>
		public MasterVersionsAttribute(
			string rawConfigurationVersion,
			string rawInteropVersion,
			string rawWebpanelVersion,
			string rawHostWatchdogVersion,
			string rawMariaDBRedistVersion,
			string rawSwarmProtocolVersion)
		{
			RawConfigurationVersion = rawConfigurationVersion ?? throw new ArgumentNullException(nameof(rawConfigurationVersion));
			RawInteropVersion = rawInteropVersion ?? throw new ArgumentNullException(nameof(rawInteropVersion));
			RawWebpanelVersion = rawWebpanelVersion ?? throw new ArgumentNullException(nameof(rawWebpanelVersion));
			RawHostWatchdogVersion = rawHostWatchdogVersion ?? throw new ArgumentNullException(nameof(rawHostWatchdogVersion));
			RawMariaDBRedistVersion = rawMariaDBRedistVersion ?? throw new ArgumentNullException(nameof(rawMariaDBRedistVersion));
			RawSwarmProtocolVersion = rawSwarmProtocolVersion ?? throw new ArgumentNullException(nameof(rawSwarmProtocolVersion));
		}
	}
}
