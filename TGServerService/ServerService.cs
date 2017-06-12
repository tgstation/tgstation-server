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
			ChatCommand = 1,
			ChatConnectFail = 2,
			ChatProviderStartFail = 3,
			InvalidChatProvider = 4,
			UpdateRequest = 5,
			BYONDUpdateFail = 6,
			BYONDUpdateStaged = 7,
			BYONDUpdateComplete = 8,
			ServerMoveFailed = 9,
			ServerMovePartial = 10,
			ServerMoveComplete = 11,
			DMCompileCrash = 12,
			DMInitializeCrash = 13,
			DMCompileError = 14,
			DMCompileSuccess = 15,
			DMCompileCancel = 16,
			DDReattachFail = 17,
			DDReattachSuccess = 18,
			DDWatchdogCrash = 19,
			DDWatchdogExit = 20,
			DDWatchdogRebootedServer = 21,
			DDWatchdogRebootingServer = 22,
			DDWatchdogRestart = 23,
			DDWatchdogRestarted = 24,
			DDWatchdogStarted = 25,
			ChatSend = 26,
			ChatBroadcast = 27,
			ChatAdminBroadcast = 28,
			ChatDisconnectFail = 29,
			TopicSent = 30,
			TopicFailed = 31,
			CommsKeySet = 32,
			NudgeStartFail = 33,
			NudgeCrash = 34,
			RepoClone = 35,
			RepoCloneFail = 36,
			RepoCheckout = 37,
			RepoCheckoutFail = 38,
			RepoHardUpdate = 39,
			RepoHardUpdateFail = 40,
			RepoMergeUpdate = 41,
			RepoMergeUpdateFail = 42,
			RepoBackupTag = 43,
			RepoBackupTagFail = 44,
			RepoResetTracked = 45,
			RepoResetTrackedFail = 46,
			RepoReset = 47,
			RepoResetFail = 48,
			RepoPRListError = 49,
			RepoPRMerge = 50,
			RepoPRMergeFail = 51,
			RepoCommit = 52,
			RepoCommitFail = 53,
			RepoPush = 54,
			RepoPushFail = 55,
			RepoChangelog = 56,
			RepoChangelogFail = 57,
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
			catch { }
		}
	}
}
