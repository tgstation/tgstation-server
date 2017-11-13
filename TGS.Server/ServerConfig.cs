using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace TGS.Server
{
	/// <inheritdoc />
	public sealed class ServerConfig : IServerConfig
	{
		/// <summary>
		/// The directory to load and save <see cref="ServerConfig"/>s to
		/// </summary>
		[JsonIgnore]
		static readonly string DefaultConfigDirectory = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TGS.Server")).FullName;

		/// <summary>
		/// The directory to use when importing a .NET settings based config
		/// </summary>
		[JsonIgnore]
		public const string MigrationConfigDirectory = "C:\\TGSSettingUpgradeTempDir";

		/// <summary>
		/// The filename to save the <see cref="ServerConfig"/> as
		/// </summary>
		[JsonIgnore]
		const string JSONFilename = "ServerConfig.json";
		/// <summary>
		/// The most recent version of the config file
		/// </summary>
		[JsonIgnore]
		const ulong CurrentVersion = 8;

		/// <inheritdoc />
		public ulong Version { get; private set; } = CurrentVersion;

		/// <inheritdoc />
		public IList<string> InstancePaths { get; private set; } = new List<string>();

		/// <inheritdoc />
		public ushort RemoteAccessPort { get; set; } = 38607;

		/// <inheritdoc />
		public string PythonPath { get; set; } = "C:\\Python27";

		/// <inheritdoc />
		public void Save()
		{
			Save(DefaultConfigDirectory);
		}

		/// <inheritdoc />
		public void Save(string directory)
		{
			var data = JsonConvert.SerializeObject(this);
			var path = Path.Combine(directory, JSONFilename);
			File.WriteAllText(path, data);
		}

		/// <summary>
		/// Load a <see cref="ServerConfig"/> from a given <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">The directory containing the <see cref="JSONFilename"/></param>
		/// <returns>The loaded <see cref="ServerConfig"/></returns>
		static ServerConfig Load(string directory)
		{
			var configtext = File.ReadAllText(Path.Combine(directory, JSONFilename));
			return JsonConvert.DeserializeObject<ServerConfig>(configtext);
		}

		/// <summary>
		/// Loads the correct <see cref="ServerConfig"/>
		/// </summary>
		/// <returns>The correct <see cref="ServerConfig"/></returns>
		public static IServerConfig LoadServerConfig()
		{
			try
			{
				return Load(DefaultConfigDirectory);
			}
			catch
			{
				try
				{
					//assume we're upgrading
					var res = Load(MigrationConfigDirectory);
					res.Save(DefaultConfigDirectory);
					Helpers.DeleteDirectory(MigrationConfigDirectory);
					return res;
				}
				catch
				{
					//new baby
					return new ServerConfig();
				}
			}
		}
	}
}
