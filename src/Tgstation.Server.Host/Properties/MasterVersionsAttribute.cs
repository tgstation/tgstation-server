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
		/// The <see cref="Version"/> <see cref="string"/> of the DMAPI version built.
		/// </summary>
		public string RawDMApiVersion { get; }

		/// <summary>
		/// The <see cref="Version"/> <see cref="string"/> of the control panel version built.
		/// </summary>
		public string RawControlPanelVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MasterVersionsAttribute"/> <see langword="class"/>.
		/// </summary>
		/// <param name="rawConfigurationVersion">The value of <see cref="RawConfigurationVersion"/>.</param>
		/// <param name="rawDMApiVersion">The value of <see cref="RawDMApiVersion"/>.</param>
		/// <param name="rawControlPanelVersion">The value of <see cref="RawControlPanelVersion"/>.</param>
		public MasterVersionsAttribute(
			string rawConfigurationVersion,
			string rawDMApiVersion,
			string rawControlPanelVersion)
		{
			RawConfigurationVersion = rawConfigurationVersion ?? throw new ArgumentNullException(nameof(rawConfigurationVersion));
			RawDMApiVersion = rawDMApiVersion ?? throw new ArgumentNullException(nameof(rawDMApiVersion));
			RawControlPanelVersion = rawControlPanelVersion ?? throw new ArgumentNullException(nameof(rawControlPanelVersion));
		}
	}
}
