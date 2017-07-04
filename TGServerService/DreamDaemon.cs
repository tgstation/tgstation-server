using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using TGServiceInterface;

namespace TGServerService
{
	//manages the dd window.
	//It's not possible to actually click it while starting it in CL mode, so in order to change visibility etc. It restarts the process when the round ends
	partial class TGStationServer : ITGDreamDaemon
	{
		enum ShutdownRequestPhase
		{
			None,
			Requested,
			Pinged,
		}

		const int DDHangStartTime = 60;
		const int DDBadStartTime = 10;

		Process Proc;

		object watchdogLock = new object();
		Thread DDWatchdog;
		TGDreamDaemonStatus currentStatus;
		ushort currentPort = 0;

		object restartLock = new object();
		bool RestartInProgress = false;

		TGDreamDaemonSecurity StartingSecurity;
		TGDreamDaemonVisibility StartingVisiblity;

		ShutdownRequestPhase AwaitingShutdown;

		//Only need 1 proc instance
		void InitDreamDaemon()
		{
			var Reattach = Config.ReattachToDD;
			if (Reattach)
				try
				{
					Proc = Process.GetProcessById(Config.ReattachPID);
					if (Proc == null)
						throw new Exception("GetProcessById returned null!");
<<<<<<< HEAD
					TGServerService.WriteInfo("Reattached to running DD process!", TGServerService.EventID.DDReattachSuccess);
					SendMessage("DD: Update complete. Watch dog reactivated...", ChatMessageType.WatchdogInfo);
=======
					TGServerService.WriteInfo("Reattached to running DD process!", TGServerService.EventID.DDReattachSuccess, this);
					SendMessage("DD: Update complete. Watchdog reactivated...");
>>>>>>> Instances
					
					//start wd 
					InitInterop();

					RestartInProgress = true;
<<<<<<< HEAD
					currentPort = Properties.Settings.Default.ReattachPort;
					serviceCommsKey = Properties.Settings.Default.ReattachCommsKey;
=======
					currentPort = Config.ReattachPort;
>>>>>>> Instances
					currentStatus = TGDreamDaemonStatus.Online;
					DDWatchdog = new Thread(new ThreadStart(Watchdog));
					DDWatchdog.Start();
				}
				catch (Exception e)
				{
<<<<<<< HEAD
					TGServerService.WriteError(String.Format("Failed to reattach to DreamDaemon! PID: {0}. Exception: {1}", Properties.Settings.Default.ReattachPID, e.ToString()), TGServerService.EventID.DDReattachFail);
=======
					TGServerService.WriteError(String.Format("Failed to reattach to DreamDaemon! PID: {0}. Exception: {1}", Config.ReattachPID, e.ToString()), TGServerService.EventID.DDReattachFail, this);
>>>>>>> Instances
				}
				finally
				{
					Config.ReattachToDD = false;
				}

			if (Proc == null)
				Proc = new Process();

			Proc.StartInfo.UseShellExecute = false;

			if (Reattach)
				return;

			//autostart the server
			if (Config.DDAutoStart)
				//break this off so we don't hold up starting the service
				ThreadPool.QueueUserWorkItem( _ => { Start(); });
		}

		//die now k thx
		void DisposeDreamDaemon()
		{
			var Detach = Config.ReattachToDD;
			if (DaemonStatus() == TGDreamDaemonStatus.Online)
			{
				if (!Detach)
				{
					WorldAnnounce("Server service stopped");
					Thread.Sleep(1000);
				}
				else
					SendMessage("DD: Detaching watch dog for update!", ChatMessageType.WatchdogInfo);
			}
			else if (Detach)
			{
				Config.ReattachToDD = false;
			}
			Stop();
		}

		//public api
		public TGDreamDaemonStatus DaemonStatus()
		{
			lock (watchdogLock)
			{
				return currentStatus;
			}
		}

		//public api
		public void RequestRestart()
		{
			SendCommand(SCHardReboot);
		}

		//public api
		public void RequestStop()
		{
			lock (watchdogLock)
			{
				if (currentStatus != TGDreamDaemonStatus.Online || AwaitingShutdown != ShutdownRequestPhase.None)
					return;
				AwaitingShutdown = ShutdownRequestPhase.Pinged;
			}
			SendCommand(SCGracefulShutdown);
		}

		//public api
		public string Stop()
		{
			Thread t;
			lock (watchdogLock)
			{
				t = DDWatchdog;
				DDWatchdog = null;
			}
			if (t != null && t.IsAlive)
			{
				t.Abort();
				t.Join();
				return null;
			}
			else
				return "Server not running";
		}

		//public api
		public void SetPort(ushort new_port)
		{
			lock (watchdogLock)
			{
				Config.ServerPort = new_port;
				RequestRestart();
			}
		}

		//handle a kill request from the server
		public void KillMe()
		{
			bool DoRestart;
			lock (watchdogLock)
			{
				DoRestart = AwaitingShutdown == ShutdownRequestPhase.None;
				if (!DoRestart)
					AwaitingShutdown = ShutdownRequestPhase.Pinged;
			}
			//Do this is a seperate thread or we'll kill this thread in the middle of rebooting
			if (DoRestart)
				ThreadPool.QueueUserWorkItem(_ => { Restart(); });
			else
				ThreadPool.QueueUserWorkItem(_ => { Stop(); });
		}

		//public api
		public string Restart()
		{
			if (DaemonStatus() == TGDreamDaemonStatus.Offline)
				return Start();
			if (!Monitor.TryEnter(restartLock))
				return "Restart already in progress";
			try
			{
				SendMessage("DD: Hard restart triggered", ChatMessageType.WatchdogInfo);
				RestartInProgress = true;
				Stop();
				var res = Start();
				return res;
			}
			finally
			{
				RestartInProgress = false;
				Monitor.Exit(restartLock);
			}
		}

		//loop that keeps the server running
		void Watchdog()
		{
			try
			{
				lock (restartLock)
				{
					if (!RestartInProgress)
					{
<<<<<<< HEAD
						SendMessage("DD: Server started, watchdog active...", ChatMessageType.WatchdogInfo);
						TGServerService.WriteInfo("Watchdog started", TGServerService.EventID.DDWatchdogStarted);
=======
						SendMessage("DD: Server started, watchdog active...");
						TGServerService.WriteInfo("Watchdog started", TGServerService.EventID.DDWatchdogStarted, this);
>>>>>>> Instances
					}
					else
					{
						RestartInProgress = false;
<<<<<<< HEAD
						TGServerService.WriteInfo("Watchdog started", TGServerService.EventID.DDWatchdogRestarted);
=======
						TGServerService.WriteInfo("Watchdog started", TGServerService.EventID.DDWatchdogRestarted, this);
>>>>>>> Instances
					}
				}
				var retries = 0;
				while (true)
				{
					var starttime = DateTime.Now;

					lock (watchdogLock)
					{
						if (AwaitingShutdown == ShutdownRequestPhase.Requested)
							SendCommand(SCGracefulShutdown);
					}

					Proc.WaitForExit();

					lock (watchdogLock)
					{
						currentStatus = TGDreamDaemonStatus.HardRebooting;
						currentPort = 0;
						Proc.Close();
						ShutdownInterop();

						if (AwaitingShutdown == ShutdownRequestPhase.Pinged)
							return;

						if ((DateTime.Now - starttime).Seconds < DDBadStartTime)
						{
							++retries;
							var sleep_time = (int)Math.Min(Math.Pow(2, retries), 3600); //max of one hour
							SendMessage(String.Format("DD: Watchdog server startup failed! Retrying in {0} seconds...", sleep_time), ChatMessageType.WatchdogInfo);
							Thread.Sleep(sleep_time * 1000);
						}
						else
						{
							retries = 0;
<<<<<<< HEAD
							var msg = "DD: DreamDaemon crashed! Watchdog rebooting DD...";
							SendMessage(msg, ChatMessageType.WatchdogInfo);
							TGServerService.WriteWarning(msg, TGServerService.EventID.DDWatchdogRebootingServer);
=======
							var msg = "DD: DreamDaemon crashed! Rebooting...";
							SendMessage(msg);
							TGServerService.WriteWarning(msg, TGServerService.EventID.DDWatchdogRebootingServer, this);
>>>>>>> Instances
						}
					}

					var res = StartImpl(true);
					if (res != null)
						throw new Exception("Hard restart failed: " + res);
				}
			}
			catch (ThreadAbortException)
			{
				//No Mr bond, I expect you to die
				try
				{
					if (!Config.ReattachToDD)
						Proc.Kill();
					else
					{
<<<<<<< HEAD
						Properties.Settings.Default.ReattachPID = Proc.Id;
						Properties.Settings.Default.ReattachPort = currentPort;
						Properties.Settings.Default.ReattachCommsKey = serviceCommsKey;
=======
						Config.ReattachPID = Proc.Id;
						Config.ReattachPort = currentPort;
>>>>>>> Instances
						RestartInProgress = true;
					}
					Proc.Close();
					ShutdownInterop();
				}
				catch
				{ }
			}
			catch (Exception e)
			{
<<<<<<< HEAD
				SendMessage("DD: Watchdog thread crashed!", ChatMessageType.WatchdogInfo);
				TGServerService.WriteError("Watch dog thread crashed: " + e.ToString(), TGServerService.EventID.DDWatchdogCrash);
=======
				SendMessage("DD: Watchdog thread crashed!");
				TGServerService.WriteError("Watch dog thread crashed: " + e.ToString(), TGServerService.EventID.DDWatchdogCrash, this);
>>>>>>> Instances
			}
			finally
			{
				lock (watchdogLock)
				{
					currentStatus = TGDreamDaemonStatus.Offline;
					currentPort = 0;
					AwaitingShutdown = ShutdownRequestPhase.None;
					if (!RestartInProgress)
					{
<<<<<<< HEAD
						SendMessage("DD: Server stopped, watchdog exiting...", ChatMessageType.WatchdogInfo);
						TGServerService.WriteInfo("Watch dog exited", TGServerService.EventID.DDWatchdogExit);
					}
					else
						TGServerService.WriteInfo("Watch dog restarting...", TGServerService.EventID.DDWatchdogRestart);
=======
						SendMessage("DD: Server stopped, watchdog exiting...");
						TGServerService.WriteInfo("Watch dog exited", TGServerService.EventID.DDWatchdogExit, this);
					}
					else
						TGServerService.WriteInfo("Watch dog restarting...", TGServerService.EventID.DDWatchdogRestart, this);
>>>>>>> Instances
				}
			}
		}

		//public api
		public string CanStart()
		{
			lock (watchdogLock)
			{
				return CanStartImpl();
			}
		}

		string CanStartImpl()
		{
			if (GetVersion(TGByondVersion.Installed) == null)
				return "Byond is not installed!";
			var DMB = PrepPath(GameDirLive + "/" + Config.ProjectName + ".dmb");
			if (!File.Exists(DMB))
				return String.Format("Unable to find {0}!", DMB);
			return null;
		}

		//public api
		public string Start()
		{
			if (CurrentStatus() == TGByondStatus.Staged)
			{
				//IMPORTANT: SLEEP FOR A MOMENT OR WONDOWS WON'T RELEASE THE FUCKING BYOND DLL HANDLES!!!! REEEEEEE
				Thread.Sleep(3000);
				ApplyStagedUpdate();
			}
			lock (watchdogLock)
			{
				if (currentStatus != TGDreamDaemonStatus.Offline)
					return "Server already running";
				var res = CanStartImpl();
				if (res != null)
					return res;
				currentPort = 0;
				currentStatus = TGDreamDaemonStatus.HardRebooting;
			}
			return StartImpl(false);
		}

		//translate the configured security level into a byond param
		string SecurityWord(bool starting = false)
		{
			var level = starting ? StartingSecurity : (TGDreamDaemonSecurity)Config.ServerSecurity;
			switch (level)
			{
				case TGDreamDaemonSecurity.Safe:
					return "safe";
				case TGDreamDaemonSecurity.Trusted:
					return "trusted";
				case TGDreamDaemonSecurity.Ultrasafe:
					return "ultrasafe";
				default:
					throw new Exception(String.Format("Bad DreamDaemon security level: {0}", level));
			}
		}

		//same thing with visibility
		string VisibilityWord(bool starting = false)
		{
			var level = starting ? StartingVisiblity : (TGDreamDaemonVisibility)Config.ServerVisiblity;
			switch (level)
			{
				case TGDreamDaemonVisibility.Invisible:
					return "invisible";
				case TGDreamDaemonVisibility.Private:
					return "private";
				case TGDreamDaemonVisibility.Public:
					return "public";
				default:
					throw new Exception(String.Format("Bad DreamDaemon visibility level: {0}", level));
			}
		}

		//used by Start and Watchdog to start a DD instance
		string StartImpl(bool watchdog)
		{
			try
			{
				lock (watchdogLock)
				{
					var res = CanStartImpl();
					if (res != null)
						return res;
					
					var DMB = '"' + PrepPath(GameDirLive + "/" + Config.ProjectName + ".dmb") + '"';

					GenCommsKey();
					StartingVisiblity = (TGDreamDaemonVisibility)Config.ServerVisiblity;
					StartingSecurity = (TGDreamDaemonSecurity)Config.ServerSecurity;
					Proc.StartInfo.FileName = PrepPath(ByondDirectory + "/bin/dreamdaemon.exe");
					Proc.StartInfo.Arguments = String.Format("{0} -port {1} -close -verbose -params server_service={4} -{2} -{3}", DMB, Config.ServerPort, SecurityWord(), VisibilityWord(), serviceCommsKey);
					InitInterop();
					Proc.Start();

					if (!Proc.WaitForInputIdle(DDHangStartTime * 1000))
					{
						Proc.Kill();
						Proc.Close();
						ShutdownInterop();
						currentStatus = TGDreamDaemonStatus.Offline;
						currentPort = 0;
						return String.Format("Server start is taking more than {0}s! Aborting!", DDHangStartTime);
					}
					currentPort = Config.ServerPort;
					currentStatus = TGDreamDaemonStatus.Online;
					if (!watchdog)
					{
						DDWatchdog = new Thread(new ThreadStart(Watchdog));
						DDWatchdog.Start();
					}
					return null;
				}
			}
			catch (Exception e)
			{
				currentStatus = TGDreamDaemonStatus.Offline;
				return e.ToString();
			}
		}

		//public api
		public TGDreamDaemonVisibility VisibilityLevel()
		{
			lock (watchdogLock)
			{
				return (TGDreamDaemonVisibility)Config.ServerVisiblity;
			}
		}

		//public api
		public bool SetVisibility(TGDreamDaemonVisibility NewVis)
		{
			var visInt = (int)NewVis;
			bool needReboot;
			lock (watchdogLock)
			{
				needReboot = Config.ServerVisiblity != visInt;
				Config.ServerVisiblity = visInt;
			}
			if (needReboot)
				RequestRestart();
			return DaemonStatus() != TGDreamDaemonStatus.Online;
		}

		//public api
		public TGDreamDaemonSecurity SecurityLevel()
		{
			lock (watchdogLock)
			{
				return (TGDreamDaemonSecurity)Config.ServerSecurity;
			}
		}

		//public api
		public bool SetSecurityLevel(TGDreamDaemonSecurity level)
		{
			var secInt = (int)level;
			bool needReboot;
			lock (watchdogLock)
			{
				needReboot = Config.ServerSecurity != secInt;
				Config.ServerSecurity = secInt;
			}
			if (needReboot)
				RequestRestart();
			return DaemonStatus() != TGDreamDaemonStatus.Online;
		}

		//public api
		public bool Autostart()
		{
			return Config.DDAutoStart;
		}

		//public api
		public void SetAutostart(bool on)
		{
			Config.DDAutoStart = on;
		}

		//public api
		public string StatusString(bool includeMetaInfo)
		{
			const string visSecStr = " (Vis: {0}, Sec: {1})";
			string res;
			var ds = DaemonStatus();
			switch (ds)
			{
				case TGDreamDaemonStatus.Offline:
					res = "OFFLINE";
					break;
				case TGDreamDaemonStatus.HardRebooting:
					res = "REBOOTING";
					break;
				case TGDreamDaemonStatus.Online:
					res = SendCommand(SCIRCCheck);
					if (includeMetaInfo)
					{
						string secandvis;
						lock (watchdogLock)
						{
							secandvis = String.Format(visSecStr, VisibilityWord(true), SecurityWord(true));
						}
						res += secandvis;
					}
					break;
				default:
					res = "NULL AND ERRORS";
					break;
			}
			if (includeMetaInfo && ds != TGDreamDaemonStatus.Online)
				res += String.Format(visSecStr, VisibilityWord(), SecurityWord());
			return res;
		}

		//public api
		public ushort Port()
		{
			return Config.ServerPort;
		}

		//public api
		public bool ShutdownInProgress()
		{
			lock (watchdogLock)
			{
				return AwaitingShutdown != ShutdownRequestPhase.None;
			}
		}
	}
}
