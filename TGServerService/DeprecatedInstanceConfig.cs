using TGServiceInterface;

namespace TGServerService
{
	/// <summary>
	/// Used to migrate old config settings
	/// </summary>
	class DeprecatedInstanceConfig : InstanceConfig
	{
		/// <summary>
		/// Convert the 3.1 .NET settings file to a config json
		/// </summary>
		/// <returns>A saved <see cref="InstanceConfig"/> based off the old .NET setting file</returns>
		public static InstanceConfig CreateFromNETSettings()
		{
			var Config = Properties.Settings.Default;
			var result = new DeprecatedInstanceConfig()
			{
				InstanceDir = (string)Config.GetPreviousVersion("ServerDirectory"),
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
			result.Save();
			return result;
		}

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
