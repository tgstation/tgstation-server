using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using TGServiceInterface;

namespace TGServerService
{
	partial class TGServerService : ServiceBase, ITGSService
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
			InstanceDelete = 58000,
			InstanceDeleteFail = 59000,
			InstanceShutdownFail = 60000,
			ServiceShutdownFail = 61000,
			WorldReboot = 62000,
		}

		static TGServerService ActiveService;   //So everyone else can write to our eventlog

		public static void WriteInfo(string message, EventID id, TGStationServer server)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Information, (int)id + server.InstanceID());
		}
		public static void WriteError(string message, EventID id, TGStationServer server)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Error, (int)id + server.InstanceID());
		}
		public static void WriteWarning(string message, EventID id, TGStationServer server)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Warning, (int)id + server.InstanceID());
		}

		ServiceHost MainHost;
		IDictionary<int, ServiceHost> Hosts;	//the WCF host
		
		//you should seriously not add anything here
		//Use OnStart instead
		public TGServerService()
		{
			try
			{
				if (Properties.Service.Default.UpgradeRequired)
				{
					Properties.Service.Default.Upgrade();
					Properties.Service.Default.UpgradeRequired = false;
					Properties.Service.Default.Save();
				}
				InitializeComponent();
				Hosts = new Dictionary<int, ServiceHost>(Properties.Service.Default.InstanceConfigs.Count);

				ActiveService = this;
				Run(this);
			}
			finally
			{
				Properties.Service.Default.Save();
			}
		}

		//when babby is formed
		protected override void OnStart(string[] args)
		{
			MainHost = new ServiceHost(this, new Uri[] { new Uri("net.pipe://localhost") })
			{
				CloseTimeout = new TimeSpan(0, 0, 5)
			}; //construction runs here

			AddEndpoint(MainHost, typeof(ITGSService), false);
			foreach (var I in Properties.Service.Default.InstanceConfigs)
				CreateInstanceImpl(I, null);
		}

		public string CreateInstance(string instanceName, string instancePath)
		{
			try
			{
				CreateInstanceImpl(instanceName, instancePath);
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		void ShutdownInstance(int instanceID)
		{
			try
			{
				var host = Hosts[instanceID];
				host.Close();
				((IDisposable)host.SingletonInstance).Dispose();
				Hosts.Remove(instanceID);
			}
			catch (Exception e)
			{
				EventLog.WriteEntry("Failed to shutdown instance: " + e.ToString(), EventLogEntryType.Error, (int)EventID.InstanceShutdownFail + instanceID);
			}
		}

		public static void DeleteInstance(int instanceID)
		{
			ActiveService.DeleteInstanceImpl(instanceID);
		}

		void DeleteInstanceImpl(int instanceID)
		{
			try
			{
				var instance = ((TGStationServer)Hosts[instanceID].SingletonInstance);
				var instancePath = instance.ServerDirectory();
				var instanceName = instance.InstanceName();
				ShutdownInstance(instanceID);
				Properties.Service.Default.InstanceConfigs.Remove(instanceName);
				Program.DeleteDirectory(instancePath);
				EventLog.WriteEntry("Instance deleted.", EventLogEntryType.Information, (int)EventID.InstanceDelete + instanceID);
			}
			catch (Exception e)
			{
				EventLog.WriteEntry("Error deleting instance: " + e.ToString(), EventLogEntryType.Error, (int)EventID.InstanceDeleteFail + instanceID);
			}
		}
		void CreateInstanceImpl(string instanceName, string instancePath)
		{
			var Config = new Properties.Instance() { SettingsKey = instanceName };
			Config.Reload();

			if (instancePath != null)
				Config.ServerDirectory = instancePath;

			if (!Directory.Exists(Config.ServerDirectory))
				Directory.CreateDirectory(Config.ServerDirectory);

			var host = new ServiceHost(new TGStationServer(instanceName, Config), new Uri[] { new Uri("net.pipe://localhost") })
			{
				CloseTimeout = new TimeSpan(0, 0, 5)
			};

			foreach (var I in Service.ValidInterfaces)
				AddEndpoint(host, I, true);

			host.Open();
			Hosts.Add(((TGStationServer)host.SingletonInstance).InstanceID(), host);
		}

		//shorthand for adding the WCF endpoint
		void AddEndpoint(ServiceHost host, Type typetype, bool instanced)
		{
			string Append = instanced ? "/Instance-" + ((TGStationServer)host.SingletonInstance).InstanceID() : "";
			host.AddServiceEndpoint(typetype, new NetNamedPipeBinding(), Service.MasterPipeName + Append + "/" + typetype.Name);
		}

		//when we is kill
		protected override void OnStop()
		{
			foreach (var I in Hosts)
				ShutdownInstance(I.Key);
		}

		public IDictionary<int, string> ListInstances()
		{
			var res = new Dictionary<int, string>(Hosts.Count);
			foreach (var I in Hosts)
				res.Add(I.Key, ((TGStationServer)I.Value.SingletonInstance).InstanceName());
			return res;
		}

		//public api
		public void VerifyConnection()
		{ }

		//public api
		public void StopForUpdate()
		{
			EventLog.WriteEntry("Stopping for update", EventLogEntryType.Information, (int)EventID.UpdateRequest);
			foreach (var I in Hosts)
				((TGStationServer)I.Value.SingletonInstance).Config.ReattachToDD = true;
			ThreadPool.QueueUserWorkItem(_ => { ActiveService.Stop(); });
		}
	}
}
