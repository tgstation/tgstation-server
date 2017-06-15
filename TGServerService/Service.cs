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
	//this line basically says make one instance of the service, use it multithreaded for requests, and never delete it
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
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
			InstanceCreate = 63000,
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
				Environment.CurrentDirectory = Directory.CreateDirectory(Path.GetTempPath() + "/TGStationServerService").FullName;  //MOVE THIS POINTER BECAUSE ONE TIME I ALMOST ACCIDENTALLY NUKED MYSELF BY REFACTORING! http://imgur.com/zvGEpJD.png
				var Config = Properties.Service.Default;
				if (Config.UpgradeRequired)
				{
					Config.Upgrade();
					Config.UpgradeRequired = false;
					Config.Save();
				}
				InitializeComponent();

				if (Config.InstanceConfigs == null)
					Config.InstanceConfigs = new System.Collections.Specialized.StringCollection();
				Hosts = new Dictionary<int, ServiceHost>(Config.InstanceConfigs.Count);

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
			};
			AddEndpoint(MainHost, typeof(ITGSService), false);
			MainHost.Open();

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
				var instancePath = instance.InstanceDirectory();
				var instanceName = instance.InstanceName();
				ShutdownInstance(instanceID);
				Hosts.Remove(instanceID);
				Properties.Service.Default.InstanceConfigs.Remove(instanceName);
				Program.DeleteDirectory(instancePath);
				EventLog.WriteEntry("Instance deleted", EventLogEntryType.Information, (int)EventID.InstanceDelete + instanceID);
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

			if (instancePath != null)   //new instance
			{
				if (!Path.IsPathRooted(instancePath))
					throw new Exception("Cannot use a relative path");

				var info = new DirectoryInfo(instancePath);

				if (info.Exists || File.Exists(instancePath))
					throw new Exception("Path specified already exists!");

				foreach(var I in Hosts)
					if (Program.IsDirectoryParentOf(((TGStationServer)I.Value.SingletonInstance).InstanceDirectory(), instancePath))
						throw new Exception("Cannot create instance in existing instance directory");

				Directory.CreateDirectory(info.Parent.FullName);

				Config.Reset();
				Config.ServerDirectory = instancePath;
				Config.UpgradeRequired = false;
				for(int I = 1;; ++I)
					if (!Hosts.ContainsKey(I))
					{
						Config.InstanceID = I;
						break;
					}
			}

			var inst = new TGStationServer(instanceName, Config);

			if (instancePath != null)
			{
				Properties.Service.Default.InstanceConfigs.Add(instanceName);
				EventLog.WriteEntry("Instance created", EventLogEntryType.Information, (int)EventID.InstanceCreate + inst.InstanceID());
			}

			var host = new ServiceHost(inst, new Uri[] { new Uri("net.pipe://localhost") })
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
			string Append = instanced ? "/" + String.Format(Service.InstanceFormat, ((TGStationServer)host.SingletonInstance).InstanceID()) : "";
			host.AddServiceEndpoint(typetype, new NetNamedPipeBinding(), Service.MasterPipeName + Append + "/" + typetype.Name);
		}

		//when we is kill
		protected override void OnStop()
		{
			foreach (var I in Hosts)
				ShutdownInstance(I.Key);
			Hosts.Clear();
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
