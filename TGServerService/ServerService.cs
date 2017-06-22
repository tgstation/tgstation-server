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
			ChatAdminBroadcast = 2800,
			ChatDisconnectFail = 2900,
			TopicSent = 3000,
			TopicFailed = 3100,
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
		}

		static TGServerService ActiveService;   //So everyone else can write to our eventlog

		public static readonly string Version = "/tg/station 13 Server Service " + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

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
