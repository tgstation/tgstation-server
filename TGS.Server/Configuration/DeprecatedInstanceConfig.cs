﻿using System;
using Newtonsoft.Json;

namespace TGS.Server.Configuration
{
	/// <summary>
	/// Used to migrate old config settings
	/// </summary>
	sealed class DeprecatedInstanceConfig : InstanceConfig
	{
		/// <summary>
		/// The default <see cref="Components.Instance"/> directory
		/// </summary>
		const string DefaultInstallationPath = "C:\\TGSTATION-SERVER-3";
		
		/// <summary>
		/// Construct a <see cref="DeprecatedInstanceConfig"/>. Used by the deserializer
		/// </summary>
		[JsonConstructor]
		DeprecatedInstanceConfig() : base(DefaultInstallationPath) { }

		/// <summary>
		/// Construct a <see cref="DeprecatedInstanceConfig"/> for a <see cref="Components.Instance"/> at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="Components.Instance"/></param>
		public DeprecatedInstanceConfig(string path) : base(path) { }

		/// <summary>
		/// Migrates the <see cref="DeprecatedInstanceConfig"/> from <see cref="Version"/> to <see cref="MigrateToCurrentVersion"/>
		/// </summary>
		public void MigrateToCurrentVersion()
		{
			//for (; Version < CurrentVersion; ++Version)
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
