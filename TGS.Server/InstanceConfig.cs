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
        string Name
        {
            get { return _name; }
            set { _committername = value; Save(); }
        }
        private string _name;

        /// <summary>
        /// If the <see cref="Instance"/> is active
        /// </summary>
		bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; Save(); }
        }
        private bool _enabled;

        /// <summary>
        /// The name of the .dme/.dmb the <see cref="Instance"/> uses
        /// </summary>
        string ProjectName
        {
            get { return _projectname; }
            set { _projectname = value; Save(); }
        }
        private string _projectname;

        /// <summary>
        /// The port the <see cref="Instance"/> runs on
        /// </summary>
        ushort Port
        {
            get { return _port; }
            set { _port = value; Save(); }
        }
        private ushort _port;

        /// <summary>
        /// The <see cref="DreamDaemonSecurity"/> level for the <see cref="Instance"/>
        /// </summary>
        DreamDaemonSecurity Security
        {
            get { return _dreamdaemonsecurity; }
            set { _dreamdaemonsecurity = value; Save(); }
        }
        private DreamDaemonSecurity _security;

        /// <summary>
        /// Whether or not the <see cref="Instance"/> should immediately start DreamDaemon when activated
        /// </summary>
        bool Autostart
        {
            get { return _autostart; }
            set { _autostart = value; Save(); }
        }
        private bool _autostart;

        /// <summary>
        /// Whether or not DreamDaemon allows connections from webclients
        /// </summary>
        bool Webclient
        {
            get { return _webclient; }
            set { _webclient = value; Save(); }
        }
        private bool _webclient;

        /// <summary>
        /// Author and committer name for synchronize commits
        /// </summary>
		string CommitterName
        {
            get { return _committername; }
            set { _committername = value;  Save(); }
        }
        private string _committername;

        /// <summary>
        /// Author and committer e-mail for synchronize commits
        /// </summary>
		string CommitterEmail
        {
            get { return _committeremail; }
            set { _committeremail = value; Save(); }
        }
        private string _committeremail;

        /// <summary>
        /// Encrypted serialized <see cref="ChatSetupInfo"/>s
        /// </summary>
        string ChatProviderData
        {
            get { return _chatproviderdata; }
            set { _chatproviderdata = value; Save(); }
        }
        private string _chatproviderdata;

        /// <summary>
        /// Entropy for <see cref="ChatProviderData"/>
        /// </summary>
        string ChatProviderEntropy
        {
            get { return _chatproviderentropy; }
            set { _chatproviderentropy = value; Save(); }
        }
        private string _chatproviderentropy;

        /// <summary>
        /// If the <see cref="Instance"/> should reattach to a running DreamDaemon <see cref="System.Diagnostics.Process"/>
        /// </summary>
        bool ReattachRequired
        {
            get { return _reattachrequired; }
            set { _reattachrequired = value; Save(); }
        }
        private bool _reattachrequired;

        /// <summary>
        /// The <see cref="System.Diagnostics.Process.Id"/> of the runnning DreamDaemon <see cref="System.Diagnostics.Process"/>
        /// </summary>
        int ReattachProcessID
        {
            get { return _reattachprocessid; }
            set { _reattachprocessid = value; Save(); }
        }
        private int _reattachprocessid;

        /// <summary>
        /// The port the runnning DreamDaemon <see cref="System.Diagnostics.Process"/> was launched on
        /// </summary>
        ushort ReattachPort
        {
            get { return _reattachport; }
            set { _reattachport = value; Save(); }
        }
        private ushort _reattachport;

        /// <summary>
        /// The serviceCommsKey the runnning DreamDaemon <see cref="System.Diagnostics.Process"/> was launched on
        /// </summary>
        string ReattachCommsKey
        {
            get { return _reattachcommskey; }
            set { _reattachcommskey = value; Save(); }
        }
        private string _reattachcommskey;

        /// <summary>
        /// The API version of the runnning DreamDaemon <see cref="System.Diagnostics.Process"/>
        /// </summary>
        string ReattachAPIVersion
        {
            get { return _reattachapiversion; }
            set { _reattachapiversion = value; Save(); }
        }
        private string _reattachapiversion;

        /// <summary>
        /// The user group allowed to use the <see cref="Instance"/>
        /// </summary>
        string AuthorizedUserGroupSID
        {
            get { return _authorizedusergroupsid; }
            set { _authorizedusergroupsid = value; Save(); }
        }
        private string _authorizedusergroupsid;

        /// <summary>
        /// The auto update interval for the <see cref="Instance"/>
        /// </summary>
        ulong AutoUpdateInterval
        {
            get { return _autoupdateinterval; }
            set { _autoupdateinterval = value; Save(); }
        }
        private private ulong _autoupdateinterval;

        /// <summary>
        /// Whether or not testmerge commits are published to a temporary remote branch
        /// </summary>
        bool PushTestmergeCommits
        {
            get { return _pushtestmergecommits; }
            set { _pushtestmergecommits = value; Save(); }
        }
        private bool _pushtestmergecommits;

        /// <summary>
        /// Time in seconds before DD is considered dead in the water
        /// </summary>
        int ServerStartupTimeout
        {
            get { return _serverstartuptimeout; }
            set { _serverstartuptimeout = value; Save(); }
        }
        private int _serverstartuptimeout;

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
		public string ChatProviderData { get; set; } = Instance.UninitializedString;

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

		/// <inheritdoc />
		public int ServerStartupTimeout { get; set; } = 60;

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
