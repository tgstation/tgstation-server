using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace TGS.Server
{
	/// <summary>
	/// Class for storing server wide settings
	/// </summary>
	public sealed class ServerConfig
	{
		/// <summary>
		/// The filename to save the <see cref="ServerConfig"/> as
		/// </summary>
		[ScriptIgnore]
		const string JSONFilename = "ServerConfig.json";
		/// <summary>
		/// The most recent version of the config file
		/// </summary>
		[ScriptIgnore]
		const ulong CurrentVersion = 8;

		/// <summary>
		/// The version of the <see cref="ServerConfig"/>
		/// </summary>
		public ulong Version { get; private set; } = CurrentVersion;

		/// <summary>
		/// List of paths that contain <see cref="Instance"/>s
		/// </summary>
		public List<string> InstancePaths { get; private set; } = new List<string>();

		/// <summary>
		/// Port used to access the <see cref="Server"/> remotely
		/// </summary>
		public ushort RemoteAccessPort { get; set; } = 38607;

		/// <summary>
		/// Path to the directory containing the Python2.7 installation
		/// </summary>
		public string PythonPath { get; set; } = "C:\\Python27";

		/// <summary>
		/// Saves the <see cref="ServerConfig"/> to a target <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">The directory in which to save the <see cref="ServerConfig"/></param>
		public void Save(string directory)
		{
			var data = new JavaScriptSerializer().Serialize(this);
			var path = Path.Combine(directory, JSONFilename);
			File.WriteAllText(path, data);
		}

		/// <summary>
		/// Load a <see cref="ServerConfig"/> from a given <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">The directory containing the <see cref="JSONFilename"/></param>
		/// <returns>The loaded <see cref="ServerConfig"/></returns>
		public static ServerConfig Load(string directory)
		{
			var configtext = File.ReadAllText(Path.Combine(directory, JSONFilename));
			return new JavaScriptSerializer().Deserialize<ServerConfig>(configtext);
		}
	}
}
