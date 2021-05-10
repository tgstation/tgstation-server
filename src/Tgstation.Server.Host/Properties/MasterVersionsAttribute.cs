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
			.GetCustomAttribute<MasterVersionsAttribute>();

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
		public string RawControlPanelVersion { get; }

		/// <summary>
		/// The <see cref="Version"/> <see cref="string"/> of the control panel version built.
		/// </summary>
		public string RawHostWatchdogVersion { get; }

		/// <summary>
		/// The compile time default.
		/// </summary>
		public string DefaultControlPanelChannel { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MasterVersionsAttribute"/> class.
		/// </summary>
		/// <param name="rawConfigurationVersion">The value of <see cref="RawConfigurationVersion"/>.</param>
		/// <param name="rawInteropVersion">The value of <see cref="RawInteropVersion"/>.</param>
		/// <param name="rawControlPanelVersion">The value of <see cref="RawControlPanelVersion"/>.</param>
		/// <param name="rawHostWatchdogVersion">The value of <see cref="RawHostWatchdogVersion"/>.</param>
		public MasterVersionsAttribute(
			string rawConfigurationVersion,
			string rawInteropVersion,
			string rawControlPanelVersion,
			string rawHostWatchdogVersion)
		{
			RawConfigurationVersion = rawConfigurationVersion ?? throw new ArgumentNullException(nameof(rawConfigurationVersion));
			RawInteropVersion = rawInteropVersion ?? throw new ArgumentNullException(nameof(rawInteropVersion));
			RawControlPanelVersion = rawControlPanelVersion ?? throw new ArgumentNullException(nameof(rawControlPanelVersion));
			RawHostWatchdogVersion = rawHostWatchdogVersion ?? throw new ArgumentNullException(nameof(rawHostWatchdogVersion));
		}
	}
}
