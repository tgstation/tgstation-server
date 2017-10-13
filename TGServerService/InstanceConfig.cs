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
		const ulong CurrentVersion = 0;	//Literally any time you add/deprecated a field, this number needs to be bumped
		[ScriptIgnore]
		string InstanceDir;

		public ulong Version { get; private set; } = CurrentVersion;
		public Guid ID { get; private set; } = Guid.NewGuid();

		public string Name { get; set; } = "TG Station Server";
		public bool Enabled { get; set; } = true;

		public string ProjectName { get; set; } = "tgstation";

		public ushort Port { get; set; } = 1337;
		public TGDreamDaemonSecurity Security { get; set; } = TGDreamDaemonSecurity.Trusted;
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

		public void Save()
		{
			File.WriteAllText(Path.Combine(InstanceDir, JSONFilename), new JavaScriptSerializer().Serialize(this));
		}

		public static InstanceConfig Load(string path)
		{
			var configtext = File.ReadAllText(Path.Combine(path, JSONFilename));
			var res = new JavaScriptSerializer().Deserialize<DeprecatedInstanceConfig>(configtext);
			res.InstanceDir = path;
			for (; res.Version < CurrentVersion; ++res.Version)
				res.Migrate();
			return res;
		}
		
		public void ConvertNETConfigToInstanceConfig()
		{
			var Config = Properties.Settings.Default;
			new InstanceConfig()
			{
				InstanceDir = (string)Config.GetPreviousVersion("ServerDirectory"),
				ProjectName = (string)Config.GetPreviousVersion("ProjectName"),
				Port = (ushort)Config.GetPreviousVersion("ServerPort"),
				CommitterName = (string)Config.GetPreviousVersion("CommitterName"),
				CommitterEmail = (string)Config.GetPreviousVersion("CommitterEmail"),
				Security = (TGDreamDaemonSecurity)Config.GetPreviousVersion("ServerSecurity"),
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
			}.Save();
		}
	}
}
