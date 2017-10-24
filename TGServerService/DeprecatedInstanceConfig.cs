using System;
using TGServiceInterface;

namespace TGServerService
{
	/// <summary>
	/// Used to migrate old config settings
	/// </summary>
	class DeprecatedInstanceConfig : InstanceConfig
	{
		/// <summary>
		/// Convert the settings version 6 .NET settings file to a config json
		/// </summary>
		/// <returns>An <see cref="InstanceConfig"/> based off the old .NET setting file</returns>
		public static InstanceConfig CreateFromNETSettings()
		{
			var Config = Properties.Settings.Default;
			var result = new DeprecatedInstanceConfig((string)Config.GetPreviousVersion("ServerDirectory"))
			{
				ProjectName = (string)Config.GetPreviousVersion("ProjectName"),
				Port = (ushort)Config.GetPreviousVersion("ServerPort"),
				CommitterName = (string)Config.GetPreviousVersion("CommitterName"),
				CommitterEmail = (string)Config.GetPreviousVersion("CommitterEmail"),
				Security = (DreamDaemonSecurity)Config.GetPreviousVersion("ServerSecurity"),
				Autostart = (bool)Config.GetPreviousVersion("DDAutoStart"),
				ChatProviderData = (string)Config.GetPreviousVersion("ChatProviderData"),
				ChatProviderEntropy = (string)Config.GetPreviousVersion("ChatProviderEntropy"),
				ReattachRequired = (bool)Config.GetPreviousVersion("ReattachToDD"),
				ReattachProcessID = (int)Config.GetPreviousVersion("ReattachPID"),
				ReattachPort = (ushort)Config.GetPreviousVersion("ReattachPort"),
				ReattachCommsKey = (string)Config.GetPreviousVersion("ReattachCommsKey"),
				ReattachAPIVersion = (string)Config.GetPreviousVersion("ReattachAPIVersion"),
				AutoUpdateInterval = (ulong)Config.GetPreviousVersion("AutoUpdateInterval"),
				AuthorizedUserGroupSID = (string)Config.GetPreviousVersion("AuthorizedGroupSID")
			};
			result.MigrateToCurrentVersion();
			return result;
		}

		/// <summary>
		/// Construct a <see cref="DeprecatedInstanceConfig"/>. Used by the deserializer
		/// </summary>
		[Obsolete("This method is for use by the deserializer only.", true)]
		public DeprecatedInstanceConfig() : base(null) { }

		/// <summary>
		/// Construct a <see cref="DeprecatedInstanceConfig"/> for a <see cref="ServerInstance"/> at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="ServerInstance"/></param>
		public DeprecatedInstanceConfig(string path) : base(path) { }

		/// <summary>
		/// Migrates the <see cref="DeprecatedInstanceConfig"/> from <see cref="Version"/> to <see cref="MigrateToCurrentVersion"/>
		/// </summary>
		public void MigrateToCurrentVersion()
		{
			for (; Version < CurrentVersion; ++Version)
				Migrate();
		}

		/// <summary>
		/// Migrates the <see cref="DeprecatedInstanceConfig"/> from <see cref="Version"/> to <see cref="Version"/> + 1
		/// </summary>
		void Migrate()
		{
			//Not needed so far
		}
	}
}
