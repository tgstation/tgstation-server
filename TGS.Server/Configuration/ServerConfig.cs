using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TGS.Server.IO;

namespace TGS.Server.Configuration
{
	/// <inheritdoc />
	public sealed class ServerConfig : IServerConfig
	{
		/// <summary>
		/// The directory to load and save <see cref="ServerConfig"/>s to
		/// </summary>
		[JsonIgnore]
		public static readonly string DefaultConfigDirectory = IOManager.ConcatPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(TGS.Server));
		/// <summary>
		/// The directory to use when importing a .NET settings based config
		/// </summary>
		[JsonIgnore]
		public static readonly string MigrationConfigDirectory = IOManager.ConcatPath(@"C:\", "TGSSettingUpgradeTempDir");

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
		public void Save(IIOManager IO)
		{
			Save(DefaultConfigDirectory, IO);
		}

		/// <inheritdoc />
		public void Save(string directory, IIOManager IO)
		{
			IO.CreateDirectory(directory).Wait();
			var data = JsonConvert.SerializeObject(this, Formatting.Indented);
			var path = IOManager.ConcatPath(directory, JSONFilename);
			IO.WriteAllText(path, data).Wait();
		}

		/// <summary>
		/// Load a <see cref="ServerConfig"/> from a given <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">The directory containing the <see cref="JSONFilename"/></param>
		/// <param name="IO">The <see cref="IIOManager"/> to use</param>
		/// <returns>The loaded <see cref="ServerConfig"/></returns>
		public static ServerConfig Load(string directory, IIOManager IO)
		{
			var configtext = IO.ReadAllText(IOManager.ConcatPath(directory, JSONFilename)).Result;
			return JsonConvert.DeserializeObject<ServerConfig>(configtext);
		}

		/// <summary>
		/// Loads the correct <see cref="ServerConfig"/>
		/// </summary>
		/// <param name="IO">The <see cref="IIOManager"/> to use</param>
		/// <returns>The correct <see cref="ServerConfig"/></returns>
		public static IServerConfig Load(IIOManager IO)
		{
			try
			{
				return Load(DefaultConfigDirectory, IO);
			}
			catch
			{
				try
				{
					//assume we're upgrading
					var res = Load(MigrationConfigDirectory, IO);
					res.Save(DefaultConfigDirectory, IO);
					Task.Factory.StartNew(() =>
					{
						try
						{
							IO.DeleteDirectory(MigrationConfigDirectory);   //safe to let this one fall out of scope
						}
						catch { }
					});
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
