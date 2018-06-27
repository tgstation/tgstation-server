using Newtonsoft.Json;
using System.IO;
using TGS.Interface;

namespace TGS.Server
{
	/// <summary>
	/// Configuration settings for a <see cref="Instance"/>
	/// </summary>
	public interface IInstanceConfig
	{
		/// <summary>
		/// The <see cref="Instance"/> directory this <see cref="IInstanceConfig"/> is for
		/// </summary>
		string Directory { get; }

		/// <summary>
		/// Actual version of the <see cref="IInstanceConfig"/>. Migrated up via <see cref="DeprecatedInstanceConfig"/>
		/// </summary>
		ulong Version { get; }

		/// <summary>
		/// The name of the <see cref="Instance"/>
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// If the <see cref="Instance"/> is active
		/// </summary>
		bool Enabled { get; set; }

		/// <summary>
		/// The name of the .dme/.dmb the <see cref="Instance"/> uses
		/// </summary>
		string ProjectName { get; set; }

		/// <summary>
		/// The port the <see cref="Instance"/> runs on
		/// </summary>
		ushort Port { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level for the <see cref="Instance"/>
		/// </summary>
		DreamDaemonSecurity Security { get; set; }

		/// <summary>
		/// Whether or not the <see cref="Instance"/> should immediately start DreamDaemon when activated
		/// </summary>
		bool Autostart { get; set; }

		/// <summary>
		/// Whether or not DreamDaemon allows connections from webclients
		/// </summary>
		bool Webclient { get; set; }

		/// <summary>
		/// Author and committer name for synchronize commits
		/// </summary>
		string CommitterName { get; set; }
		/// <summary>
		/// Author and committer e-mail for synchronize commits
		/// </summary>
		string CommitterEmail { get; set; }

		/// <summary>
		/// Encrypted serialized <see cref="ChatSetupInfo"/>s
		/// </summary>
		string ChatProviderData { get; set; }

		/// <summary>
		/// Entropy for <see cref="ChatProviderData"/>
		/// </summary>
		string ChatProviderEntropy { get; set; }

		/// <summary>
		/// If the <see cref="Instance"/> should reattach to a running DreamDaemon <see cref="System.Diagnostics.Process"/>
		/// </summary>
		bool ReattachRequired { get; set; }

		/// <summary>
		/// The <see cref="System.Diagnostics.Process.Id"/> of the runnning DreamDaemon <see cref="System.Diagnostics.Process"/>
		/// </summary>
		int ReattachProcessID { get; set; }

		/// <summary>
		/// The port the runnning DreamDaemon <see cref="System.Diagnostics.Process"/> was launched on
		/// </summary>
		ushort ReattachPort { get; set; }

		/// <summary>
		/// The serviceCommsKey the runnning DreamDaemon <see cref="System.Diagnostics.Process"/> was launched on
		/// </summary>
		string ReattachCommsKey { get; set; }

		/// <summary>
		/// The API version of the runnning DreamDaemon <see cref="System.Diagnostics.Process"/>
		/// </summary>
		string ReattachAPIVersion { get; set; }

		/// <summary>
		/// The user group allowed to use the <see cref="Instance"/>
		/// </summary>
		string AuthorizedUserGroupSID { get; set; }

		/// <summary>
		/// The auto update interval for the <see cref="Instance"/>
		/// </summary>
		ulong AutoUpdateInterval { get; set; }

		/// <summary>
		/// Whether or not testmerge commits are published to a temporary remote branch
		/// </summary>
		bool PushTestmergeCommits { get; set; }

		/// <summary>
		/// Time in seconds before DD is considered dead in the water
		/// </summary>
		int ServerStartupTimeout { get; set; }

		/// <summary>
		/// Saves the <see cref="IInstanceConfig"/> to it's <see cref="Instance"/> <see cref="Directory"/>
		/// </summary>
		void Save();
	}

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
		protected const ulong CurrentVersion = 1;   //Literally any time you add/deprecated a field, this number needs to be bumped

		/// <inheritdoc />
		[JsonIgnore]
		public string Directory { get; private set; }

		/// <inheritdoc />
		public ulong Version { get; protected set; } = CurrentVersion;

		/// <inheritdoc />
		public string Name
        {
            get { return _name; }
            set { _name = value; Save(); }
        }
        private string _name = "TG Station Server";

		/// <inheritdoc />
		public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; Save(); }
        }
        private bool _enabled = true;

		/// <inheritdoc />
		public string ProjectName
        {
            get { return _projectname; }
            set { _projectname = value; Save(); }
        }
        private string _projectname = "tgstation";

		/// <inheritdoc />
		public ushort Port
        {
            get { return _port; }
            set { _port = value; Save(); }
        }
        private ushort _port = 1337;

		/// <inheritdoc />
		public DreamDaemonSecurity Security
        {
            get { return _security; }
            set { _security = value; Save(); }
        }
        private DreamDaemonSecurity _security = DreamDaemonSecurity.Trusted;

		/// <inheritdoc />
		public bool Autostart
        {
            get { return _autostart; }
            set { _autostart = value; Save(); }
        }
        private bool _autostart = false;

		/// <inheritdoc />
		public bool Webclient
        {
            get { return _webclient; }
            set { _webclient = value; Save(); }
        }
        private bool _webclient = false;

		/// <inheritdoc />
		public string CommitterName
        {
            get { return _committername; }
            set { _committername = value; Save(); }
        }
        private string _committername = "tgstation-server";

		/// <inheritdoc />
		public string CommitterEmail
        {
            get { return _committeremail; }
            set { _committeremail = value; Save(); }
        }
        private string _committeremail = "tgstation-server@tgstation13.org";

		/// <inheritdoc />
		public string ChatProviderData
        {
            get { return _chatproviderdata; }
            set { _chatproviderdata = value; Save(); }
        }
        private string _chatproviderdata = Instance.UninitializedString;

		/// <inheritdoc />
		public string ChatProviderEntropy
        {
            get { return _chatproviderentropy; }
            set { _chatproviderentropy = value; Save(); }
        }
        private string _chatproviderentropy;

        /// <inheritdoc />
        public bool ReattachRequired
        {
            get { return _reattachrequired; }
            set { _reattachrequired = value; Save(); }
        }
        private bool _reattachrequired = false;

		/// <inheritdoc />
		public int ReattachProcessID
        {
            get { return _reattachprocessid; }
            set { _reattachprocessid = value; Save(); }
        }
        private int _reattachprocessid;

        /// <inheritdoc />
        public ushort ReattachPort
        {
            get { return _reattachport; }
            set { _reattachport = value; Save(); }
        }
        private ushort _reattachport;

        /// <inheritdoc />
        public string ReattachCommsKey
        {
            get { return _reattachcommskey; }
            set { _reattachcommskey = value; Save(); }
        }
        private string _reattachcommskey;

        /// <inheritdoc />
        public string ReattachAPIVersion
        {
            get { return _reattachapiversion; }
            set { _reattachapiversion = value; Save(); }
        }
        private string _reattachapiversion;

        /// <inheritdoc />
        public string AuthorizedUserGroupSID
        {
            get { return _authorizedusergroupsid; }
            set { _authorizedusergroupsid = value; Save(); }
        }
        private string _authorizedusergroupsid = null;

		/// <inheritdoc />
		public ulong AutoUpdateInterval
        {
            get { return _autoupdateinterval; }
            set { _autoupdateinterval = value; Save(); }
        }
        private ulong _autoupdateinterval = 0;

		/// <inheritdoc />
		public bool PushTestmergeCommits
        {
            get { return _pushtestmergecommits; }
            set { _pushtestmergecommits = value; Save(); }
        }
        private bool _pushtestmergecommits = false;

		/// <inheritdoc />
		public int ServerStartupTimeout
        {
            get { return _serverstartuptimeout; }
            set { _serverstartuptimeout = value; Save(); }
        }
        private int _serverstartuptimeout = 60;

		/// <summary>
		/// Construct a <see cref="InstanceConfig"/> for a <see cref="Instance"/> at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="Instance"/></param>
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
		/// Loads and migrates an <see cref="IInstanceConfig"/> from a <see cref="Instance"/> at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="Instance"/> directory</param>
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
