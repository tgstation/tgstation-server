using System.Collections.Generic;
using System.ServiceModel;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <summary>
	/// <see langword="interface"/> aggregator allowing for all to fall under one <see cref="ServiceHost"/> while remaining seperate in implementation
	/// </summary>
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	sealed class WCFContractRelay : ITGAdministration, ITGByond, ITGChat, ITGCompiler, ITGConnectivity, ITGDreamDaemon, ITGInstance, ITGInterop, ITGRepository, ITGStatic
	{
		/// <summary>
		/// The <see cref="ITGAdministration"/> for the <see cref="WCFContractRelay"/>
		/// </summary>
		readonly ITGAdministration Administration;
		/// <summary>
		/// The <see cref="ITGByond"/> for the <see cref="WCFContractRelay"/>
		/// </summary>
		readonly ITGByond Byond;
		/// <summary>
		/// The <see cref="ITGChat"/> for the <see cref="WCFContractRelay"/>
		/// </summary>
		readonly ITGChat Chat;
		/// <summary>
		/// The <see cref="ITGCompiler"/> for the <see cref="WCFContractRelay"/>
		/// </summary>
		readonly ITGCompiler Compiler;
		/// <summary>
		/// The <see cref="ITGConnectivity"/> for the <see cref="WCFContractRelay"/>
		/// </summary>
		readonly ITGConnectivity Connectivity;
		/// <summary>
		/// The <see cref="ITGDreamDaemon"/> for the <see cref="WCFContractRelay"/>
		/// </summary>
		readonly ITGDreamDaemon DreamDaemon;
		/// <summary>
		/// The <see cref="IInstance"/> for the <see cref="WCFContractRelay"/>
		/// </summary>
		readonly IInstance Instance;
		/// <summary>
		/// The <see cref="ITGInterop"/> for the <see cref="WCFContractRelay"/>
		/// </summary>
		readonly ITGInterop Interop;
		/// <summary>
		/// The <see cref="ITGRepository"/> for the <see cref="WCFContractRelay"/>
		/// </summary>
		readonly ITGRepository Repository;
		/// <summary>
		/// The <see cref="ITGStatic"/> for the <see cref="WCFContractRelay"/>
		/// </summary>
		readonly ITGStatic Static;

		/// <summary>
		/// Construct a <see cref="WCFContractRelay"/>
		/// </summary>
		/// <param name="administration">The value of <see cref="Administration"/></param>
		/// <param name="byond">The value of <see cref="Byond"/></param>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		/// <param name="compiler">The value of <see cref="Compiler"/></param>
		/// <param name="connectivity">The value of <see cref="Connectivity"/></param>
		/// <param name="dreamDaemon">The value of <see cref="DreamDaemon"/></param>
		/// <param name="instance">The value of <see cref="Instance"/></param>
		/// <param name="interop">The value of <see cref="Interop"/></param>
		/// <param name="repository">The value of <see cref="Repository"/></param>
		/// <param name="_static">The value of <see cref="Static"/></param>
		public WCFContractRelay(IAdministrationManager administration, IByondManager byond, IChatManager chat, ICompilerManager compiler, IConnectivityManager connectivity, IDreamDaemonManager dreamDaemon, IInstance instance, IInteropManager interop, IRepositoryManager repository, IStaticManager _static)
		{
			Administration = administration;
			Byond = byond;
			Chat = chat;
			Compiler = compiler;
			Connectivity = connectivity;
			DreamDaemon = dreamDaemon;
			Instance = instance;
			Interop = interop;
			Repository = repository;
			Static = _static;
		}

		/// <summary>
		/// Access the underlying <see cref="IInstance"/>
		/// </summary>
		/// <returns><see cref="Instance"/></returns>
		public IInstance GetInstance()
		{
			return Instance;
		}

		/// <inheritdoc />
		public bool Autostart()
		{
			return DreamDaemon.Autostart();
		}

		/// <inheritdoc />
		public ulong AutoUpdateInterval()
		{
			return Instance.AutoUpdateInterval();
		}

		/// <inheritdoc />
		public string Cancel()
		{
			return Compiler.Cancel();
		}

		/// <inheritdoc />
		public string Checkout(string objectName)
		{
			return Repository.Checkout(objectName);
		}

		/// <inheritdoc />
		public int CheckoutProgress()
		{
			return Repository.CheckoutProgress();
		}

		/// <inheritdoc />
		public string Clone(string remote, string branch = "master")
		{
			return Repository.Clone(remote, branch);
		}

		/// <inheritdoc />
		public bool Compile(bool silent = false)
		{
			return Compiler.Compile(silent);
		}

		/// <inheritdoc />
		public string CompileError()
		{
			return Compiler.CompileError();
		}

		/// <inheritdoc />
		public bool Connected(ChatProvider providerType)
		{
			return Chat.Connected(providerType);
		}

		/// <inheritdoc />
		public ByondStatus CurrentStatus()
		{
			return Byond.CurrentStatus();
		}

		/// <inheritdoc />
		public DreamDaemonStatus DaemonStatus()
		{
			return DreamDaemon.DaemonStatus();
		}

		/// <inheritdoc />
		public string DeleteFile(string staticRelativePath, out bool unauthorized)
		{
			return Static.DeleteFile(staticRelativePath, out unauthorized);
		}

		/// <inheritdoc />
		public bool Exists()
		{
			return Repository.Exists();
		}

		/// <inheritdoc />
		public string GenerateChangelog(out string error)
		{
			return Repository.GenerateChangelog(out error);
		}

		/// <inheritdoc />
		public string GetBranch(out string error)
		{
			return Repository.GetBranch(out error);
		}

		/// <inheritdoc />
		public string GetCommitterEmail()
		{
			return Repository.GetCommitterEmail();
		}

		/// <inheritdoc />
		public string GetCommitterName()
		{
			return Repository.GetCommitterName();
		}

		/// <inheritdoc />
		public string GetCurrentAuthorizedGroup()
		{
			return Administration.GetCurrentAuthorizedGroup();
		}

		/// <inheritdoc />
		public string GetError()
		{
			return Byond.GetError();
		}

		/// <inheritdoc />
		public string GetHead(bool useTracked, out string error)
		{
			return Repository.GetHead(useTracked, out error);
		}

		/// <inheritdoc />
		public string GetRemote(out string error)
		{
			return Repository.GetRemote(out error);
		}

		/// <inheritdoc />
		public CompilerStatus GetStatus()
		{
			return Compiler.GetStatus();
		}

		/// <inheritdoc />
		public string GetVersion(ByondVersion type)
		{
			return Byond.GetVersion(type);
		}

		/// <inheritdoc />
		public bool Initialize()
		{
			return Compiler.Initialize();
		}

		/// <inheritdoc />
		public bool InteropMessage(string command)
		{
			return Interop.InteropMessage(command);
		}

		/// <inheritdoc />
		public IDictionary<string, string> ListBackups(out string error)
		{
			return Repository.ListBackups(out error);
		}

		/// <inheritdoc />
		public IList<string> ListStaticDirectory(string subpath, out string error, out bool unauthorized)
		{
			return Static.ListStaticDirectory(subpath, out error, out unauthorized);
		}

		/// <inheritdoc />
		public List<PullRequestInfo> MergedPullRequests(out string error)
		{
			return Repository.MergedPullRequests(out error);
		}

		/// <inheritdoc />
		public string MergePullRequest(int PRnumber, string atSHA = null)
		{
			return Repository.MergePullRequest(PRnumber, atSHA);
		}

		/// <inheritdoc />
		public bool OperationInProgress()
		{
			return Repository.OperationInProgress();
		}

		/// <inheritdoc />
		public int PlayerCount()
		{
			return Interop.PlayerCount();
		}

		/// <inheritdoc />
		public ushort Port()
		{
			return DreamDaemon.Port();
		}

		/// <inheritdoc />
		public string ProjectName()
		{
			return Compiler.ProjectName();
		}

		/// <inheritdoc />
		public IList<ChatSetupInfo> ProviderInfos()
		{
			return Chat.ProviderInfos();
		}

		/// <inheritdoc />
		public bool PushTestmergeCommits()
		{
			return Repository.PushTestmergeCommits();
		}

		/// <inheritdoc />
		public string ReadText(string staticRelativePath, bool repo, out string error, out bool unauthorized)
		{
			return Static.ReadText(staticRelativePath, repo, out error, out unauthorized);
		}

		/// <inheritdoc />
		public string Reconnect(ChatProvider providerType)
		{
			return Chat.Reconnect(providerType);
		}

		/// <inheritdoc />
		public string RecreateStaticFolder()
		{
			return Administration.RecreateStaticFolder();
		}

		/// <inheritdoc />
		public void RequestRestart()
		{
			DreamDaemon.RequestRestart();
		}

		/// <inheritdoc />
		public void RequestStop()
		{
			DreamDaemon.RequestStop();
		}

		/// <inheritdoc />
		public string Reset(bool tracked)
		{
			return Repository.Reset(tracked);
		}

		/// <inheritdoc />
		public string Restart()
		{
			return DreamDaemon.Restart();
		}

		/// <inheritdoc />
		public DreamDaemonSecurity SecurityLevel()
		{
			return DreamDaemon.SecurityLevel();
		}

		/// <inheritdoc />
		public string ServerDirectory()
		{
			return Instance.ServerDirectory();
		}

		/// <inheritdoc />
		public string SetAuthorizedGroup(string groupName)
		{
			return Administration.SetAuthorizedGroup(groupName);
		}

		/// <inheritdoc />
		public void SetAutostart(bool on)
		{
			DreamDaemon.SetAutostart(on);
		}

		/// <inheritdoc />
		public void SetAutoUpdateInterval(ulong newInterval)
		{
			Instance.SetAutoUpdateInterval(newInterval);
		}

		/// <inheritdoc />
		public void SetCommitterEmail(string newEmail)
		{
			Repository.SetCommitterEmail(newEmail);
		}

		/// <inheritdoc />
		public void SetCommitterName(string newName)
		{
			Repository.SetCommitterName(newName);
		}

		/// <inheritdoc />
		public void SetPort(ushort new_port)
		{
			DreamDaemon.SetPort(new_port);
		}

		/// <inheritdoc />
		public void SetProjectName(string projectName)
		{
			Compiler.SetProjectName(projectName);
		}

		/// <inheritdoc />
		public string SetProviderInfo(ChatSetupInfo info)
		{
			return Chat.SetProviderInfo(info);
		}

		/// <inheritdoc />
		public void SetPushTestmergeCommits(bool newValue)
		{
			Repository.SetPushTestmergeCommits(newValue);
		}

		/// <inheritdoc />
		public bool SetSecurityLevel(DreamDaemonSecurity level)
		{
			return DreamDaemon.SetSecurityLevel(level);
		}

		/// <inheritdoc />
		public void SetWebclient(bool on)
		{
			DreamDaemon.SetWebclient(on);
		}

		/// <inheritdoc />
		public bool ShutdownInProgress()
		{
			return DreamDaemon.ShutdownInProgress();
		}

		/// <inheritdoc />
		public string Start()
		{
			return DreamDaemon.Start();
		}

		/// <inheritdoc />
		public string StatusString(bool includeMetaInfo)
		{
			return DreamDaemon.StatusString(includeMetaInfo);
		}

		/// <inheritdoc />
		public string Stop()
		{
			return DreamDaemon.Stop();
		}

		/// <inheritdoc />
		public string SynchronizePush()
		{
			return Repository.SynchronizePush();
		}

		/// <inheritdoc />
		public string Update(bool reset)
		{
			return Repository.Update(reset);
		}

		/// <inheritdoc />
		public string UpdateTGS3Json()
		{
			return Instance.UpdateTGS3Json();
		}

		/// <inheritdoc />
		public bool UpdateToVersion(int major, int minor)
		{
			return Byond.UpdateToVersion(major, minor);
		}

		/// <inheritdoc />
		public void VerifyConnection()
		{
			Connectivity.VerifyConnection();
		}

		/// <inheritdoc />
		public string Version()
		{
			return Instance.Version();
		}

		/// <inheritdoc />
		public bool Webclient()
		{
			return DreamDaemon.Webclient();
		}

		/// <inheritdoc />
		public void WorldAnnounce(string msg)
		{
			Interop.WorldAnnounce(msg);
		}

		/// <inheritdoc />
		public string WriteText(string staticRelativePath, string data, out bool unauthorized)
		{
			return Static.WriteText(staticRelativePath, data, out unauthorized);
		}
	}
}
