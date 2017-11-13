using System;
using System.Configuration;

namespace TGS.Server
{
	/// <summary>
	/// Used to migrate old config settings
	/// </summary>
	class DeprecatedInstanceConfig : InstanceConfig
	{
		/// <summary>
		/// The default <see cref="ServerInstance"/> directory
		/// </summary>
		const string DefaultInstallationPath = "C:\\tgstation-server-3";

		/// <summary>
		/// Convert the settings version 6 .NET settings file to a config json
		/// </summary>
		/// <returns>An <see cref="IInstanceConfig"/> based off the old .NET setting file</returns>
		public static IInstanceConfig CreateFromNETSettings()
		{
			var Config = Properties.Settings.Default;
			var result = new DeprecatedInstanceConfig(Helpers.NormalizePath(LoadPreviousNetPropertyOrDefault("ServerDirectory", "C:\\tgstation-server-3")));
			// using nameof for sanity where possible
			result.ProjectName = LoadPreviousNetPropertyOrDefault(nameof(ProjectName), result.ProjectName);
			result.Port = LoadPreviousNetPropertyOrDefault("ServerPort", result.Port);
			result.CommitterName = LoadPreviousNetPropertyOrDefault(nameof(CommitterName), result.CommitterName);
			result.CommitterEmail = LoadPreviousNetPropertyOrDefault(nameof(CommitterEmail), result.CommitterEmail);
			result.Security = LoadPreviousNetPropertyOrDefault("ServerSecurity", result.Security);
			result.Autostart = LoadPreviousNetPropertyOrDefault("DDAutoStart", result.Autostart);
			result.ChatProviderData = LoadPreviousNetPropertyOrDefault(nameof(ChatProviderData), result.ChatProviderData);
			result.ChatProviderEntropy = LoadPreviousNetPropertyOrDefault(nameof(ChatProviderEntropy), result.ChatProviderEntropy);
			result.ReattachRequired = LoadPreviousNetPropertyOrDefault("ReattachToDD", result.ReattachRequired);
			result.ReattachProcessID = LoadPreviousNetPropertyOrDefault("ReattachPID", result.ReattachProcessID);
			result.ReattachPort = LoadPreviousNetPropertyOrDefault(nameof(ReattachPort), result.ReattachPort);
			result.ReattachCommsKey = LoadPreviousNetPropertyOrDefault(nameof(ReattachCommsKey), result.ReattachCommsKey);
			result.ReattachAPIVersion = LoadPreviousNetPropertyOrDefault(nameof(ReattachAPIVersion), result.ReattachAPIVersion);
			result.AutoUpdateInterval = LoadPreviousNetPropertyOrDefault(nameof(AutoUpdateInterval), result.AutoUpdateInterval);
			result.AuthorizedUserGroupSID = LoadPreviousNetPropertyOrDefault("AuthorizedGroupSID", result.AuthorizedUserGroupSID);
			result.MigrateToCurrentVersion();
			return result;
		}

		/// <summary>
		/// Loads a previous .NET config <paramref name="property"/> and returns it or some <paramref name="defaultValue"/> if it wasn't set
		/// </summary>
		/// <typeparam name="T">The type of the <paramref name="property"/></typeparam>
		/// <param name="property">The config key of the property</param>
		/// <param name="defaultValue">The default value of the <paramref name="property"/></param>
		/// <returns>The <paramref name="property"/> if it exists, otherwise the <paramref name="defaultValue"/></returns>
		static T LoadPreviousNetPropertyOrDefault<T>(string property, T defaultValue)
		{
			//try it the simple way first
			try
			{
				var result = (T)Properties.Settings.Default.GetPreviousVersion(property);
				return result == null ? defaultValue : result;
			}
			catch
			{
				try
				{
					//.NET is fucking stupid
					//If we don't have the correct property in our *CURRENT* .settings file it will automatically throw an exception when it tries to load it
					//Which means we can never fucking delete config settings
					//Which is fucking retarded
					//This hooks into the settings provider and forces it to load it anyway
					var Config = Properties.Settings.Default;
					var Provider = Config.Properties[nameof(Config.SettingsVersion)].Provider;	//nameof for sanity

					var sp = new SettingsProperty(property)
					{
						PropertyType = typeof(T),
						DefaultValue = defaultValue,
						Provider = Provider
					};

					var ProviderInterface = Provider as IApplicationSettingsProvider;

					var result = ProviderInterface.GetPreviousVersion(Config.Context, sp);
					if (result != null && result.PropertyValue != null)
						return (T)result.PropertyValue;
				}
				catch { }
				//f u c k   i t
				return defaultValue;
			}
		}

		/// <summary>
		/// Construct a <see cref="DeprecatedInstanceConfig"/>. Used by the deserializer
		/// </summary>
		[Obsolete("This method is for use by the deserializer only.", true)]
		public DeprecatedInstanceConfig() : base(DefaultInstallationPath) { }

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
