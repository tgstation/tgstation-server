using System;
using System.IO;
using System.Web.Script.Serialization;
using TGServiceInterface;

namespace TGServerService
{
	class InstanceConfig
	{
		//tell javascriptserializer to ignore these fields
		[ScriptIgnore]
		const string JSONFilename = "Instance.json";
		[ScriptIgnore]
		protected const ulong CurrentVersion = 0;   //Literally any time you add/deprecated a field, this number needs to be bumped
		[ScriptIgnore]
		public string InstanceDirectory { get; private set; }

		public ulong Version { get; protected set; } = CurrentVersion;

		public string Name { get; set; } = "TG Station Server";
		public bool Enabled { get; set; } = true;

		public string ProjectName { get; set; } = "tgstation";

		public ushort Port { get; set; } = 1337;
		public DreamDaemonSecurity Security { get; set; } = DreamDaemonSecurity.Trusted;
		public bool Autostart { get; set; } = false;
		public bool Webclient { get; set; } = false;

		public string CommitterName { get; set; } = "tgstation-server";
		public string CommitterEmail { get; set; } = "tgstation-server@tgstation13.org";

		public string ChatProviderData { get; set; } = "NEEDS INITIALIZING";
		public string ChatProviderEntropy { get; set; }

		public bool ReattachRequired { get; set; } = false;
		public int ReattachProcessID { get; set; }
		public ushort ReattachPort { get; set; }
		public string ReattachCommsKey { get; set; }
		public string ReattachAPIVersion { get; set; }

		public string AuthorizedUserGroupSID { get; set; } = null;

		public ulong AutoUpdateInterval { get; set; } = 0;

		/// <summary>
		/// Construct a <see cref="InstanceConfig"/> for a <see cref="ServerInstance"/> at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="ServerInstance"/></param>
		public InstanceConfig(string path)
		{
			InstanceDirectory = path;
		}

		public void Save()
		{
			var data = new JavaScriptSerializer().Serialize(this);
			var path = Path.Combine(InstanceDirectory, JSONFilename);
			File.WriteAllText(path, data);
		}

		public static InstanceConfig Load(string path)
		{
			var configtext = File.ReadAllText(Path.Combine(path, JSONFilename));
			var res = new JavaScriptSerializer().Deserialize<DeprecatedInstanceConfig>(configtext);
			res.InstanceDirectory = path;
			res.MigrateToCurrentVersion();
			return res;
		}
	}
}
