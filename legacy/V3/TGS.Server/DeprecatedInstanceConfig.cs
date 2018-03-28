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
		/// The default <see cref="Instance"/> directory
		/// </summary>
		const string DefaultInstallationPath = "C:\\TGSTATION-SERVER-3";
		
		/// <summary>
		/// Construct a <see cref="DeprecatedInstanceConfig"/>. Used by the deserializer
		/// </summary>
		[Obsolete("This method is for use by the deserializer only.", true)]
		public DeprecatedInstanceConfig() : base(DefaultInstallationPath) { }

		/// <summary>
		/// Construct a <see cref="DeprecatedInstanceConfig"/> for a <see cref="Instance"/> at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="Instance"/></param>
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
