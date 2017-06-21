using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using TGServiceInterface;

namespace TGServerService
{
	public partial class TGServerService : ServiceBase
	{ 
		//only deprecate events, do not reuse them
		public enum EventID
		{
			ChatCommand = 1000,
			ChatConnectFail = 2000,
			ChatProviderStartFail = 3000,
			InvalidChatProvider = 4000,
			UpdateRequest = 5000,
			BYONDUpdateFail = 6000,
			BYONDUpdateStaged = 7000,
			BYONDUpdateComplete = 8000,
			ServerMoveFailed = 9000,
			ServerMovePartial = 10000,
			ServerMoveComplete = 11000,
			DMCompileCrash = 12000,
			DMInitializeCrash = 13000,
			DMCompileError = 14000,
			DMCompileSuccess = 15000,
			DMCompileCancel = 16000,
			DDReattachFail = 17000,
			DDReattachSuccess = 18000,
			DDWatchdogCrash = 19000,
			DDWatchdogExit = 20000,
			DDWatchdogRebootedServer = 21000,
			DDWatchdogRebootingServer = 22000,
			DDWatchdogRestart = 23000,
			DDWatchdogRestarted = 24000,
			DDWatchdogStarted = 25000,
			ChatSend = 26000,
			ChatBroadcast = 27000,
			ChatAdminBroadcast = 28000,
			ChatDisconnectFail = 29000,
			TopicSent = 300000,
			TopicFailed = 31000,
			CommsKeySet = 32000,
			NudgeStartFail = 33000,
			NudgeCrash = 34000,
			RepoClone = 35000,
			RepoCloneFail = 36000,
			RepoCheckout = 37000,
			RepoCheckoutFail = 38000,
			RepoHardUpdate = 39000,
			RepoHardUpdateFail = 40000,
			RepoMergeUpdate = 41000,
			RepoMergeUpdateFail = 42000,
			RepoBackupTag = 43000,
			RepoBackupTagFail = 44000,
			RepoResetTracked = 45000,
			RepoResetTrackedFail = 46000,
			RepoReset = 47000,
			RepoResetFail = 48000,
			RepoPRListError = 49000,
			RepoPRMerge = 50000,
			RepoPRMergeFail = 51000,
			RepoCommit = 52000,
			RepoCommitFail = 53000,
			RepoPush = 54000,
			RepoPushFail = 55000,
			RepoChangelog = 56000,
			RepoChangelogFail = 57000,
			ServiceShutdownFail = 61000,
			WorldReboot = 62000,
			ServerUpdateApplied = 63000,
		}

		static TGServerService ActiveService;   //So everyone else can write to our eventlog

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
    
		public static void LocalStop()
		{
			ThreadPool.QueueUserWorkItem(_ => { ActiveService.Stop(); });
		}

		ServiceHost host;	//the WCF host
    
		//you should seriously not add anything here
		//Use OnStart instead
		public TGServerService()
		{
			try
			{
				if (Properties.Settings.Default.UpgradeRequired)
				{
					Properties.Settings.Default.Upgrade();
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

			host = new ServiceHost(typeof(TGStationServer), new Uri[] { new Uri("net.pipe://localhost") })
			{
				CloseTimeout = new TimeSpan(0, 0, 5)
			}; //construction runs here

			foreach (var I in Server.ValidInterfaces)
				AddEndpoint(I);

			host.Open();    //...or maybe here, doesn't really matter
		}

		//shorthand for adding the WCF endpoint
		void AddEndpoint(Type typetype)
		{
			host.AddServiceEndpoint(typetype, new NetNamedPipeBinding(), Server.MasterPipeName + "/" + typetype.Name);
		}

		//when we is kill
		protected override void OnStop()
		{
			try
			{
				host.Close();   //where TGStationServer.Dispose() is called
				host = null;
			}
			catch (Exception e)
			{
				WriteError(e.ToString(), EventID.ServiceShutdownFail);
			}
		}
	}
}
