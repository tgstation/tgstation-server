using Newtonsoft.Json;
using System.IO;
using TGS.Interface;

namespace TGS.Server.Configuration
{
	/// <inheritdoc />
	public class InstanceConfig : IInstanceConfig
	{
		/// <summary>
		/// The name the file is saved as in the <see cref="Directory"/>
		/// </summary>
		[JsonIgnore]
		public const string JSONFilename = "Instance.json";

		/// <summary>
		/// The current version of the config
		/// </summary>
		[JsonIgnore]
		protected const ulong CurrentVersion = 0;   //Literally any time you add/deprecated a field, this number needs to be bumped

		/// <inheritdoc />
		[JsonIgnore]
		public string Directory { get; private set; }

		/// <inheritdoc />
		public ulong Version { get; protected set; } = CurrentVersion;

		/// <inheritdoc />
		public string Name { get; set; } = "TG Station Server";

		/// <inheritdoc />
		public bool Enabled { get; set; } = true;

		/// <inheritdoc />
		public string ProjectName { get; set; } = "tgstation";

		/// <inheritdoc />
		public ushort Port { get; set; } = 1337;

		/// <inheritdoc />
		public DreamDaemonSecurity Security { get; set; } = DreamDaemonSecurity.Trusted;

		/// <inheritdoc />
		public bool Autostart { get; set; } = false;

		/// <inheritdoc />
		public bool Webclient { get; set; } = false;

		/// <inheritdoc />
		public string CommitterName { get; set; } = "tgstation-server";

		/// <inheritdoc />
		public string CommitterEmail { get; set; } = "tgstation-server@tgstation13.org";

		/// <inheritdoc />
		public string ChatProviderData { get; set; } = Components.ChatManager.UninitializedString;

		/// <inheritdoc />
		public string ChatProviderEntropy { get; set; }

		/// <inheritdoc />
		public bool ReattachRequired { get; set; } = false;

		/// <inheritdoc />
		public int ReattachProcessID { get; set; }

		/// <inheritdoc />
		public ushort ReattachPort { get; set; }

		/// <inheritdoc />
		public string ReattachCommsKey { get; set; }

		/// <inheritdoc />
		public string ReattachAPIVersion { get; set; }

		/// <inheritdoc />
		public string AuthorizedUserGroupSID { get; set; } = null;

		/// <inheritdoc />
		public ulong AutoUpdateInterval { get; set; } = 0;

		/// <inheritdoc />
		public bool PushTestmergeCommits { get; set; } = false;

		/// <summary>
		/// Construct a <see cref="InstanceConfig"/> for an <see cref="Components.Instance"/> at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="Components.Instance"/></param>
		public InstanceConfig(string path)
		{
			Directory = path;
		}

		/// <inheritdoc />
		public void Save()
		{
			var data = JsonConvert.SerializeObject(this, Formatting.Indented);
			var path = Path.Combine(Directory, JSONFilename);
			File.WriteAllText(path, data);
		}

		/// <summary>
		/// Loads and migrates an <see cref="IInstanceConfig"/> from a <see cref="Components.Instance"/> at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="Components.Instance"/> directory</param>
		/// <returns>The migrated <see cref="IInstanceConfig"/></returns>
		public static IInstanceConfig Load(string path)
		{
			var configtext = File.ReadAllText(Path.Combine(path, JSONFilename));
			var res = JsonConvert.DeserializeObject<DeprecatedInstanceConfig>(configtext);
			res.Directory = path;
			res.MigrateToCurrentVersion();
			return res;
		}
	}
}
