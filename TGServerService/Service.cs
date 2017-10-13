using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceProcess;
using TGServiceInterface;

namespace TGServerService
{
	public partial class TGServerService : ServiceBase, ITGConnectivity, ITGSService
	{ 
		//only deprecate events, do not reuse them
		public enum EventID
		{
			ChatCommand = 100,
			ChatConnectFail = 200,
			ChatProviderStartFail = 300,
			InvalidChatProvider = 400,
			UpdateRequest = 500,
			BYONDUpdateFail = 600,
			BYONDUpdateStaged = 700,
			BYONDUpdateComplete = 800,
			ServerMoveFailed = 900,
			ServerMovePartial = 1000,
			ServerMoveComplete = 1100,
			DMCompileCrash = 1200,
			DMInitializeCrash = 1300,
			DMCompileError = 1400,
			DMCompileSuccess = 1500,
			DMCompileCancel = 1600,
			DDReattachFail = 1700,
			DDReattachSuccess = 1800,
			DDWatchdogCrash = 1900,
			DDWatchdogExit = 2000,
			DDWatchdogRebootedServer = 2100,
			DDWatchdogRebootingServer = 2200,
			DDWatchdogRestart = 2300,
			DDWatchdogRestarted = 2400,
			DDWatchdogStarted = 2500,
			ChatSend = 2600,
			ChatBroadcast = 2700,
			//ChatAdminBroadcast = 2800,
			ChatDisconnectFail = 2900,
			//TopicSent = 3000,
			//TopicFailed = 3100,
			CommsKeySet = 3200,
			NudgeStartFail = 3300,
			NudgeCrash = 3400,
			RepoClone = 3500,
			RepoCloneFail = 3600,
			RepoCheckout = 3700,
			RepoCheckoutFail = 3800,
			RepoHardUpdate = 3900,
			RepoHardUpdateFail = 4000,
			RepoMergeUpdate = 4100,
			RepoMergeUpdateFail = 4200,
			RepoBackupTag = 4300,
			RepoBackupTagFail = 4400,
			RepoResetTracked = 4500,
			RepoResetTrackedFail = 4600,
			RepoReset = 4700,
			RepoResetFail = 4800,
			RepoPRListError = 4900,
			RepoPRMerge = 5000,
			RepoPRMergeFail = 5100,
			RepoCommit = 5200,
			RepoCommitFail = 5300,
			RepoPush = 5400,
			RepoPushFail = 5500,
			RepoChangelog = 5600,
			RepoChangelogFail = 5700,
			ServiceShutdownFail = 6100,
			WorldReboot = 6200,
			ServerUpdateApplied = 6300,
			ChatBroadcastFail = 6400,
			IRCLogModes = 6500,
			SubmoduleReclone = 6600,
			Authentication = 6700,
			PreactionEvent = 6800,
			PreactionFail = 6900,
			InteropCallException = 7000,
			APIVersionMismatch = 7100,
			RepoConfigurationFail = 7200,
			StaticRead = 7300,
			StaticWrite = 7400,
			StaticDelete = 7500,
			InstanceInitializationFailure = 7600,
		}

		static TGServerService ActiveService;   //So everyone else can write to our eventlog

		public static readonly string VersionString = "/tg/station 13 Server Service v" + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

		/// <summary>
		/// You can't write to logs while impersonating, call this to cancel WCF's impersonation first
		/// </summary>
		public static void CancelImpersonation()
		{
			WindowsIdentity.Impersonate(IntPtr.Zero);
		}

		public static void WriteInfo(string message, EventID id)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Information, (int)id);
		}
		public static void WriteError(string message, EventID id)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Error, (int)id);
		}
		public static void WriteWarning(string message, EventID id)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Warning, (int)id);
		}

		public static void WriteAccess(string username, bool authSuccess)
		{
			ActiveService.EventLog.WriteEntry(String.Format("Access from: {0}", username), authSuccess ? EventLogEntryType.SuccessAudit : EventLogEntryType.FailureAudit, (int)EventID.Authentication);
		}
		
		ServiceHost serviceHost;
		IDictionary<string, ServiceHost> hosts;

		void MigrateSettings(int oldVersion, int newVersion)
		{
			//TODO
		}
	
		//you should seriously not add anything here
		//Use OnStart instead
		public TGServerService()
		{
			var Config = Properties.Settings.Default;
			try
			{
				Environment.CurrentDirectory = Directory.CreateDirectory(Path.GetTempPath() + "/TGStationServerService").FullName;  //MOVE THIS POINTER BECAUSE ONE TIME I ALMOST ACCIDENTALLY NUKED MYSELF BY REFACTORING! http://imgur.com/zvGEpJD.png
				if (Properties.Settings.Default.UpgradeRequired)
				{
					var newVersion = Config.SettingsVersion;
					Config.Upgrade();
					var oldVersion = Config.SettingsVersion;
					Config.SettingsVersion = newVersion;

					MigrateSettings(oldVersion, newVersion);

					Config.UpgradeRequired = false;
					Config.Save();
				}
				ActiveService = this;
				InitializeComponent();
				Run(this);
			}
			finally
			{
				Config.Save();
			}
		}

		void ChangePortFromCommandLine(string[] args)
		{
			var Config = Properties.Settings.Default;

			for (var I = 0; I < args.Length - 1; ++I)
				if (args[I].ToLower() == "-port")
				{
					try
					{
						var res = Convert.ToUInt16(args[I + 1]);
						if (res == 0)
							throw new Exception("Cannot bind to port 0");
						Config.RemoteAccessPort = res;
					}
					catch (Exception e)
					{
						throw new Exception("Invalid argument for \"-port\"", e);
					}
					Config.Save();
					break;
				}
		}
		//when babby is formed
		protected override void OnStart(string[] args)
		{
			ChangePortFromCommandLine(args);

			SetupService();

			SetupInstances();

			OnlineAllHosts();
		}

		void SetupService()
		{
			serviceHost = CreateHost(this);
			AddEndpoint(serviceHost, typeof(ITGSService), Server.MasterInterfaceName);
			serviceHost.Authorization.ServiceAuthorizationManager = new AdministrativeAuthorizationManager();	//only admins can diddle us
		}

		void OnlineAllHosts()
		{
			serviceHost.Open();
			foreach (var I in hosts)
				I.Value.Open();
		}

		static ServiceHost CreateHost(object singleton)
		{
			return new ServiceHost(singleton, new Uri[] { new Uri("net.pipe://localhost"), new Uri(String.Format("https://localhost:{0}", Properties.Settings.Default.RemoteAccessPort)) })
			{
				CloseTimeout = new TimeSpan(0, 0, 5)
			};
		}

		void SetupInstances()
		{
			var pathsToRemove = new List<string>();
			foreach (var I in Properties.Settings.Default.InstancePaths)
				if (SetupInstance(I) != null)
					pathsToRemove.Add(I);
		}
		ServiceHost SetupInstance(string path)
		{
			TGStationServer instance;
			try
			{
				var config = InstanceConfig.Load(path);
				if (hosts.ContainsKey(path))
				{
					var datInstance = ((TGStationServer)hosts[path].SingletonInstance);
					WriteError(String.Format("Unable to start instance at path {0}. Has the same name as instance at path {1} ({2}). Detaching...", path, datInstance.ServerDirectory(), datInstance.Config.Name), EventID.InstanceInitializationFailure);
					return null;
				}
				if (!config.Enabled)
					return null;
				instance = new TGStationServer(config);
			}
			catch (Exception e)
			{
				WriteError(String.Format("Unable to start instance at path {0}. Detaching... Error: {1}", path, e.ToString()), EventID.InstanceInitializationFailure);
				return null;
			}

			var host = CreateHost(instance);
			hosts.Add(instance.Config.Name, host);

			var endpointPrefix = String.Format("{0}/{1}", Server.MasterInterfaceName, instance.Config.Name);
			foreach (var J in Server.InstanceInterfaces)
				AddEndpoint(host, J, endpointPrefix);

			host.Authorization.ServiceAuthorizationManager = instance;
			return host;
		}

		//shorthand for adding the WCF endpoint
		void AddEndpoint(ServiceHost host, Type typetype, string PipePrefix)
		{
			var bindingName = PipePrefix + "/" + typetype.Name;
			host.AddServiceEndpoint(typetype, new NetNamedPipeBinding() { SendTimeout = new TimeSpan(0, 0, 30), MaxReceivedMessageSize = Server.TransferLimitLocal }, bindingName);
			var httpsBinding = new WSHttpBinding()
			{
				SendTimeout = new TimeSpan(0, 0, 40),
				MaxReceivedMessageSize = Server.TransferLimitRemote
			};
			var requireAuth = typetype.Name != typeof(ITGConnectivity).Name;
			httpsBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
			httpsBinding.Security.Mode = requireAuth ? SecurityMode.TransportWithMessageCredential : SecurityMode.Transport;	//do not require auth for a connectivity check
			httpsBinding.Security.Message.ClientCredentialType = requireAuth ? MessageCredentialType.UserName : MessageCredentialType.None;
			host.AddServiceEndpoint(typetype, httpsBinding, bindingName);
		}

		//when we is kill
		protected override void OnStop()
		{
			try
			{
				foreach (var I in hosts)
				{
					var host = I.Value;
					TGStationServer instance = (TGStationServer)host.SingletonInstance;
					host.Close();
					instance.Dispose();
				}
			}
			catch (Exception e)
			{
				WriteError(e.ToString(), EventID.ServiceShutdownFail);
			}
		}

		/// <inheritdoc />
		public void VerifyConnection() { }

		/// <inheritdoc />
		public void PrepareForUpdate()
		{
			foreach (var I in hosts)
				((TGStationServer)I.Value.SingletonInstance).Reattach(false);
		}

		/// <inheritdoc />
		public ushort RemoteAccessPort()
		{
			return Properties.Settings.Default.RemoteAccessPort;
		}

		/// <inheritdoc />
		public string SetRemoteAccessPort(ushort port)
		{
			if (port == 0)
				return "Cannot bind to port 0";
			Properties.Settings.Default.RemoteAccessPort = port;
			return null;
		}

		/// <inheritdoc />
		public string Version()
		{
			return VersionString;
		}

		/// <inheritdoc />
		public IDictionary<string, string> ListInstances()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string CreateInstance(string Name, string path)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string ImportInstance(string path)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool InstanceEnabled(string Name)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string SetInstanceEnabled(string Name, bool enabled)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string RenameInstance(string name, string new_name)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string DetachInstance(string name)
		{
			throw new NotImplementedException();
		}
	}
}
