using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceProcess;
using TGServiceInterface;

namespace TGServerService
{
	public partial class TGServerService : ServiceBase
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
		}

		static TGServerService ActiveService;   //So everyone else can write to our eventlog

		public static readonly string Version = "/tg/station 13 Server Service v" + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

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

		ServiceHost host;   //the WCF host

		void MigrateSettings(int oldVersion, int newVersion)
		{
			if (oldVersion == newVersion && newVersion == 0)	//chat refactor
				Properties.Settings.Default.ChatProviderData = "NEEDS INITIALIZING";	//reset chat settings to be safe
		}
	
		//you should seriously not add anything here
		//Use OnStart instead
		public TGServerService()
		{
			try
			{
				if (Properties.Settings.Default.UpgradeRequired)
				{
					var newVersion = Properties.Settings.Default.SettingsVersion;
					Properties.Settings.Default.Upgrade();
					var oldVersion = Properties.Settings.Default.SettingsVersion;
					Properties.Settings.Default.SettingsVersion = newVersion;

					MigrateSettings(oldVersion, newVersion);

					Properties.Settings.Default.UpgradeRequired = false;
					Properties.Settings.Default.Save();
				}
				InitializeComponent();
				ActiveService = this;
				Run(this);
			}
			finally
			{
				Properties.Settings.Default.Save();
			}
		}

		//when babby is formed
		protected override void OnStart(string[] args)
		{
			var Config = Properties.Settings.Default;
			if (!Directory.Exists(Config.ServerDirectory))
			{
				EventLog.WriteEntry("Creating server directory: " + Config.ServerDirectory);
				Directory.CreateDirectory(Config.ServerDirectory);
			}
			Environment.CurrentDirectory = Config.ServerDirectory;

			var instance = new TGStationServer();

			for (var I = 0; I < args.Length - 1; ++I)
				if (args[I].ToLower() == "-port")
				{
					Config.RemoteAccessPort = Convert.ToUInt16(args[I + 1]);
					Config.Save();
					break;
				}

			host = new ServiceHost(instance, new Uri[] { new Uri("net.pipe://localhost"), new Uri(String.Format("https://localhost:{0}", Config.RemoteAccessPort)) })
			{
				CloseTimeout = new TimeSpan(0, 0, 5)
			};

			foreach (var I in Server.ValidInterfaces)
				AddEndpoint(I);
			
			host.Authorization.ServiceAuthorizationManager = instance;

			try
			{
				host.Open();
			}
			catch (AddressAlreadyInUseException e)
			{
				throw new Exception("Can't start the service due to the configured remote access port being in use. To fix this change it by starting the service with the \"-port <port>\" argument.", e);
			}
		}

		//shorthand for adding the WCF endpoint
		void AddEndpoint(Type typetype)
		{
			var bindingName = Server.MasterInterfaceName + "/" + typetype.Name;
			host.AddServiceEndpoint(typetype, new NetNamedPipeBinding(), bindingName);
			var httpsBinding = new WSHttpBinding();
			var requireAuth = typetype.Name != typeof(ITGConnectivity).Name;
			httpsBinding.Security.Mode = requireAuth ? SecurityMode.TransportWithMessageCredential : SecurityMode.Transport;	//do not require auth for a connectivity check
			httpsBinding.Security.Message.ClientCredentialType = requireAuth ? MessageCredentialType.UserName : MessageCredentialType.None;
			host.AddServiceEndpoint(typetype, httpsBinding, bindingName);
		}

		//when we is kill
		protected override void OnStop()
		{
			try
			{
				TGStationServer instance = (TGStationServer)host.SingletonInstance;
				host.Close();
				instance.Dispose();
			}
			catch (Exception e)
			{
				WriteError(e.ToString(), EventID.ServiceShutdownFail);
			}
		}
	}
}
