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

		ShutdownRequestPhase AwaitingShutdown;

		//Only need 1 proc instance
		void InitDreamDaemon()
		{
			var Reattach = Properties.Settings.Default.ReattachToDD;
			if (Reattach)
				try
				{
					Proc = Process.GetProcessById(Properties.Settings.Default.ReattachPID);
					if (Proc == null)
						throw new Exception("GetProcessById returned null!");
					TGServerService.WriteInfo("Reattached to running DD process!", TGServerService.EventID.DDReattachSuccess);
					SendMessage("DD: Update complete. Watch dog reactivated...", ChatMessageType.WatchdogInfo);
					
					//start wd 
					InitInterop();

					RestartInProgress = true;
					currentPort = Properties.Settings.Default.ReattachPort;
					serviceCommsKey = Properties.Settings.Default.ReattachCommsKey;
					currentStatus = TGDreamDaemonStatus.Online;
					DDWatchdog = new Thread(new ThreadStart(Watchdog));
					DDWatchdog.Start();
				}
				catch (Exception e)
				{
					TGServerService.WriteError(String.Format("Failed to reattach to DreamDaemon! PID: {0}. Exception: {1}", Properties.Settings.Default.ReattachPID, e.ToString()), TGServerService.EventID.DDReattachFail);
				}
				finally
				{
					Properties.Settings.Default.ReattachToDD = false;
				}

			if (Proc == null)
				Proc = new Process();

			Proc.StartInfo.FileName = ByondDirectory + "/bin/dreamdaemon.exe";
			Proc.StartInfo.UseShellExecute = false;

			if (Reattach)
				return;

			//autostart the server
			if (Properties.Settings.Default.DDAutoStart)
				//break this off so we don't hold up starting the service
				ThreadPool.QueueUserWorkItem( _ => { Start(); });
		}

		//die now k thx
		void DisposeDreamDaemon()
		{
			var Detach = Properties.Settings.Default.ReattachToDD;
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
				Properties.Settings.Default.ReattachToDD = false;
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
				Properties.Settings.Default.ServerPort = new_port;
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
						SendMessage("DD: Server started, watchdog active...", ChatMessageType.WatchdogInfo);
						TGServerService.WriteInfo("Watchdog started", TGServerService.EventID.DDWatchdogStarted);
					}
					else
					{
						RestartInProgress = false;
						TGServerService.WriteInfo("Watchdog started", TGServerService.EventID.DDWatchdogRestarted);
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

						if ((DateTime.Now - starttime).TotalSeconds < DDBadStartTime)
						{
							++retries;
							var sleep_time = (int)Math.Min(Math.Pow(2, retries), 3600); //max of one hour
							SendMessage(String.Format("DD: Watchdog server startup failed! Retrying in {0} seconds...", sleep_time), ChatMessageType.WatchdogInfo);
							Thread.Sleep(sleep_time * 1000);
						}
						else
						{
							retries = 0;
							var msg = "DD: DreamDaemon crashed! Watchdog rebooting DD...";
							SendMessage(msg, ChatMessageType.WatchdogInfo);
							TGServerService.WriteWarning(msg, TGServerService.EventID.DDWatchdogRebootingServer);
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
					if (!Properties.Settings.Default.ReattachToDD)
					{
						Proc.Kill();
						Proc.WaitForExit();
					}
					else
					{
						Properties.Settings.Default.ReattachPID = Proc.Id;
						Properties.Settings.Default.ReattachPort = currentPort;
						Properties.Settings.Default.ReattachCommsKey = serviceCommsKey;
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
				SendMessage("DD: Watchdog thread crashed!", ChatMessageType.WatchdogInfo);
				TGServerService.WriteError("Watch dog thread crashed: " + e.ToString(), TGServerService.EventID.DDWatchdogCrash);
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
						SendMessage("DD: Server stopped, watchdog exiting...", ChatMessageType.WatchdogInfo);
						TGServerService.WriteInfo("Watch dog exited", TGServerService.EventID.DDWatchdogExit);
					}
					else
						TGServerService.WriteInfo("Watch dog restarting...", TGServerService.EventID.DDWatchdogRestart);
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
			var DMB = GameDirLive + "/" + Properties.Settings.Default.ProjectName + ".dmb";
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
			var level = starting ? StartingSecurity : (TGDreamDaemonSecurity)Properties.Settings.Default.ServerSecurity;
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

					var Config = Properties.Settings.Default;
					var DMB = GameDirLive + "/" + Config.ProjectName + ".dmb";

					GenCommsKey();
					StartingSecurity = (TGDreamDaemonSecurity)Config.ServerSecurity;
					Proc.StartInfo.Arguments = String.Format("{0} -port {1} -close -verbose -params server_service={3} -{2} -public", DMB, Config.ServerPort, SecurityWord(), serviceCommsKey);
					InitInterop();
					Proc.Start();

					if (!Proc.WaitForInputIdle(DDHangStartTime * 1000))
					{
						Proc.Kill();
						Proc.WaitForExit();
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
		public TGDreamDaemonSecurity SecurityLevel()
		{
			lock (watchdogLock)
			{
				return (TGDreamDaemonSecurity)Properties.Settings.Default.ServerSecurity;
			}
		}

		//public api
		public bool SetSecurityLevel(TGDreamDaemonSecurity level)
		{
			var Config = Properties.Settings.Default;
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
			return Properties.Settings.Default.DDAutoStart;
		}

		//public api
		public void SetAutostart(bool on)
		{
			Properties.Settings.Default.DDAutoStart = on;
		}

		//public api
		public string StatusString(bool includeMetaInfo)
		{
			const string visSecStr = " (Sec: {0})";
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
							secandvis = String.Format(visSecStr, SecurityWord(true));
						}
						res += secandvis;
					}
					break;
				default:
					res = "NULL AND ERRORS";
					break;
			}
			if (includeMetaInfo && ds != TGDreamDaemonStatus.Online)
				res += String.Format(visSecStr, SecurityWord());
			return res;
		}

		//public api
		public ushort Port()
		{
			return Properties.Settings.Default.ServerPort;
		}

		//public api
		public bool ShutdownInProgress()
		{
			lock (watchdogLock)
			{
				return AwaitingShutdown != ShutdownRequestPhase.None;
			}
		}

		/// <inheritdoc />
		public string WorldAnnounce(string message)
		{
			var res = SendCommand(SCWorldAnnounce + ";message=" + message);
			if (res == "SUCCESS")
				return null;
			return res;
		}
	}
}
