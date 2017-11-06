using System.IO;
using System.Web.Script.Serialization;
using TGServiceInterface;

namespace TGServerService
{
	class InstanceConfig
	{
		/// <summary>
		/// The name the file is saved as in the <see cref="Directory"/>
		/// </summary>
		//tell javascriptserializer to ignore these fields
		[ScriptIgnore]
		public const string JSONFilename = "Instance.json";
		/// <summary>
		/// The current version of the config
		/// </summary>
		[ScriptIgnore]
		protected const ulong CurrentVersion = 0;   //Literally any time you add/deprecated a field, this number needs to be bumped
		/// <summary>
		/// The <see cref="ServerInstance"/> directory this <see cref="InstanceConfig"/> is for
		/// </summary>
		[ScriptIgnore]
		public string Directory { get; private set; }

		/// <summary>
		/// Actual version of the <see cref="InstanceConfig"/>. Migrated up via <see cref="DeprecatedInstanceConfig"/>
		/// </summary>
		public ulong Version { get; protected set; } = CurrentVersion;

		/// <summary>
		/// The name of the <see cref="ServerInstance"/>
		/// </summary>
		public string Name { get; set; } = "TG Station Server";

		/// <summary>
		/// If the <see cref="ServerInstance"/> is active
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// The name of the .dme/.dmb the <see cref="ServerInstance"/> uses
		/// </summary>
		public string ProjectName { get; set; } = "tgstation";

		/// <summary>
		/// The port the <see cref="ServerInstance"/> runs on
		/// </summary>
		public ushort Port { get; set; } = 1337;

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level for the <see cref="ServerInstance"/>
		/// </summary>
		public DreamDaemonSecurity Security { get; set; } = DreamDaemonSecurity.Trusted;

		/// <summary>
		/// Whether or not the <see cref="ServerInstance"/> should immediately start DreamDaemon when activated
		/// </summary>
		public bool Autostart { get; set; } = false;

		/// <summary>
		/// Whether or not DreamDaemon allows connections from webclients
		/// </summary>
		public bool Webclient { get; set; } = false;

		/// <summary>
		/// Author and committer name for synchronize commits
		/// </summary>
		public string CommitterName { get; set; } = "tgstation-server";
		/// <summary>
		/// Author and committer e-mail for synchronize commits
		/// </summary>
		public string CommitterEmail { get; set; } = "tgstation-server@tgstation13.org";

		/// <summary>
		/// Encrypted serialized <see cref="ChatSetupInfo"/>s
		/// </summary>
		public string ChatProviderData { get; set; } = ServerInstance.UninitializedString;

		/// <summary>
		/// Entropy for <see cref="ChatProviderData"/>
		/// </summary>
		public string ChatProviderEntropy { get; set; }

		/// <summary>
		/// If the <see cref="ServerInstance"/> should reattach to a running DreamDaemon <see cref="System.Diagnostics.Process"/>
		/// </summary>
		public bool ReattachRequired { get; set; } = false;

		/// <summary>
		/// The <see cref="System.Diagnostics.Process.Id"/> of the runnning DreamDaemon <see cref="System.Diagnostics.Process"/>
		/// </summary>
		public int ReattachProcessID { get; set; }

		/// <summary>
		/// The port the runnning DreamDaemon <see cref="System.Diagnostics.Process"/> was launched on
		/// </summary>
		public ushort ReattachPort { get; set; }

		/// <summary>
		/// The serviceCommsKey the runnning DreamDaemon <see cref="System.Diagnostics.Process"/> was launched on
		/// </summary>
		public string ReattachCommsKey { get; set; }

		/// <summary>
		/// The API version of the runnning DreamDaemon <see cref="System.Diagnostics.Process"/>
		/// </summary>
		public string ReattachAPIVersion { get; set; }

		/// <summary>
		/// The user group allowed to use the <see cref="ServerInstance"/>
		/// </summary>
		public string AuthorizedUserGroupSID { get; set; } = null;

		/// <summary>
		/// The auto update interval for the <see cref="ServerInstance"/>
		/// </summary>
		public ulong AutoUpdateInterval { get; set; } = 0;

		/// <summary>
		/// Whether or not testmerge commits are published to a temporary remote branch
		/// </summary>
		public bool PushTestmergeCommits { get; set; } = false;

		/// <summary>
		/// Construct a <see cref="InstanceConfig"/> for a <see cref="ServerInstance"/> at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="ServerInstance"/></param>
		public InstanceConfig(string path)
		{
			Directory = path;
		}

		/// <summary>
		/// Saves the <see cref="InstanceConfig"/> to it's <see cref="ServerInstance"/> <see cref="Directory"/>
		/// </summary>
		public void Save()
		{
			var data = new JavaScriptSerializer().Serialize(this);
			var path = Path.Combine(Directory, JSONFilename);
			File.WriteAllText(path, data);
		}

		/// <summary>
		/// Loads and migrates an <see cref="InstanceConfig"/> from a <see cref="ServerInstance"/> at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="ServerInstance"/> directory</param>
		/// <returns>The migrated <see cref="InstanceConfig"/></returns>
		public static InstanceConfig Load(string path)
		{
			var configtext = File.ReadAllText(Path.Combine(path, JSONFilename));
			var res = new JavaScriptSerializer().Deserialize<DeprecatedInstanceConfig>(configtext);
			res.Directory = path;
			res.MigrateToCurrentVersion();
			return res;
		}
	}
}
