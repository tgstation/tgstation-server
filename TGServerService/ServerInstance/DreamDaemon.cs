using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Timers;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGServerService
{
	//manages the dd window.
	//It's not possible to actually click it while starting it in CL mode, so in order to change visibility etc. It restarts the process when the round ends
	partial class ServerInstance : ITGDreamDaemon
	{
		enum ShutdownRequestPhase
		{
			None,
			Requested,
			Pinged,
		}

		const string DiagnosticsDir = "Diagnostics";
		const string ResourceDiagnosticsDir = DiagnosticsDir + "/Resources";
		const int DDHangStartTime = 60;
		const int DDBadStartTime = 10;

		Process Proc;
		PerformanceCounter pcpu;

		object watchdogLock = new object();
		Thread DDWatchdog;
		DreamDaemonStatus currentStatus;
		string CurrentDDLog;
		ushort currentPort = 0;

		object restartLock = new object();
		bool RestartInProgress = false;

		DreamDaemonSecurity StartingSecurity;

		ShutdownRequestPhase AwaitingShutdown;

		//Only need 1 proc instance
		void InitDreamDaemon()
		{
			Directory.CreateDirectory(DiagnosticsDir);
			Directory.CreateDirectory(ResourceDiagnosticsDir);
			var Reattach = Properties.Settings.Default.ReattachToDD;
			if (Reattach)
				try
				{
					Proc = Process.GetProcessById(Properties.Settings.Default.ReattachPID);
					if (Proc == null)
						throw new Exception("GetProcessById returned null!");
					Service.WriteInfo("Reattached to running DD process!", EventID.DDReattachSuccess);
					ThreadPool.QueueUserWorkItem(_ =>
					{
						Thread.Sleep(5000);
						SendMessage("DD: Update complete. Watch dog reactivated...", MessageType.WatchdogInfo);
					});

					//start wd 
					RestartInProgress = true;
					currentPort = Properties.Settings.Default.ReattachPort;
					serviceCommsKey = Properties.Settings.Default.ReattachCommsKey;
					try
					{
						GameAPIVersion = new Version(Properties.Settings.Default.ReattachAPIVersion);
					}
					catch { }
					currentStatus = DreamDaemonStatus.Online;
					DDWatchdog = new Thread(new ThreadStart(Watchdog));
					DDWatchdog.Start();
				}
				catch (Exception e)
				{
					Service.WriteError(String.Format("Failed to reattach to DreamDaemon! PID: {0}. Exception: {1}", Properties.Settings.Default.ReattachPID, e.ToString()), EventID.DDReattachFail);
				}
				finally
				{
					Properties.Settings.Default.ReattachToDD = false;
					Properties.Settings.Default.Save();
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
			bool RenameLog = false;
			if (DaemonStatus() == DreamDaemonStatus.Online)
			{
				if (!Detach)
				{
					WorldAnnounce("Server service stopped");
					Thread.Sleep(1000);
				}
				else
				{
					RenameLog = CurrentDDLog != null;
					SendMessage("DD: Detaching watch dog for update!", MessageType.WatchdogInfo);
					WriteCurrentDDLog("Service updating! Splitting diagnostics...");
				}
			}
			else if (Detach)
				Properties.Settings.Default.ReattachToDD = false;
			Stop();
			if(pcpu != null)
				pcpu.Dispose();
			if (RenameLog)
				try
				{
					File.Move(Path.Combine(ResourceDiagnosticsDir, CurrentDDLog), Path.Combine(ResourceDiagnosticsDir, "SU-" + CurrentDDLog));
				}
				catch { }
		}

		//public api
		public DreamDaemonStatus DaemonStatus()
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
				if (currentStatus != DreamDaemonStatus.Online || AwaitingShutdown != ShutdownRequestPhase.None)
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
			if (DaemonStatus() == DreamDaemonStatus.Offline)
				return Start();
			lock(restartLock)
			{
				if (RestartInProgress)
					return "Restart already in progress";
				RestartInProgress = true;
			}
			SendMessage("DD: Hard restart triggered", MessageType.WatchdogInfo);
			Stop();
			var res = Start();
			if(res != null)
				lock(restartLock)
				{
					RestartInProgress = false;
				}
			return res;
		}

		void WriteCurrentDDLog(string message)
		{
			lock (watchdogLock)
			{
				if (currentStatus != DreamDaemonStatus.Online || CurrentDDLog == null)
					return;
				File.AppendAllText(Path.Combine(ResourceDiagnosticsDir, CurrentDDLog), String.Format("[{0}]: {1}\n", DateTime.Now.ToLongTimeString(), message));
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
						SendMessage("DD: Server started, watchdog active...", MessageType.WatchdogInfo);
						Service.WriteInfo("Watchdog started", EventID.DDWatchdogStarted);
					}
					else
					{
						RestartInProgress = false;
						Service.WriteInfo("Watchdog started", EventID.DDWatchdogRestarted);
					}
				}
				var retries = 0;

				var MemTrackTimer = new System.Timers.Timer
				{
					AutoReset = true,
					Interval = 5000 //every 5 seconds
				};
				MemTrackTimer.Elapsed += MemTrackTimer_Elapsed;
				while (true)
				{
					var starttime = DateTime.Now;

					lock (watchdogLock)
					{
						if (AwaitingShutdown == ShutdownRequestPhase.Requested)
							SendCommand(SCGracefulShutdown);
					}

					//all good to go, let's start monitoring
					var Now = DateTime.Now;
					lock (watchdogLock)
					{
						CurrentDDLog = String.Format("{0} {1} Diagnostics.txt", Now.ToLongDateString(), Now.ToLongTimeString()).Replace(':', '-');
						WriteCurrentDDLog("Starting monitoring...");
						pcpu = new PerformanceCounter("Process", "% Processor Time", Proc.ProcessName, true);
					}
					MemTrackTimer.Start();
					Proc.WaitForExit();
					lock (watchdogLock)	//synchronize
					{
						MemTrackTimer.Stop();
						pcpu.Dispose();
					}

					WriteCurrentDDLog("Crash detected!");

					lock (watchdogLock)
					{
						currentStatus = DreamDaemonStatus.HardRebooting;
						currentPort = 0;
						Proc.Close();

						if (AwaitingShutdown == ShutdownRequestPhase.Pinged)
							return;
						var BadStart = (DateTime.Now - starttime).TotalSeconds < DDBadStartTime;
						if (BadStart)
						{
							++retries;
							var sleep_time = (int)Math.Min(Math.Pow(2, retries), 3600); //max of one hour
							SendMessage(String.Format("DD: Watchdog server startup failed! Retrying in {0} seconds...", sleep_time), MessageType.WatchdogInfo);
							Thread.Sleep(sleep_time * 1000);
						}
						else
						{
							retries = 0;
							var msg = "DD: DreamDaemon crashed! Watchdog rebooting DD...";
							SendMessage(msg, MessageType.WatchdogInfo);
							Service.WriteWarning(msg, EventID.DDWatchdogRebootingServer);
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
				}
				catch
				{ }
			}
			catch (Exception e)
			{
				SendMessage("DD: Watchdog thread crashed!", MessageType.WatchdogInfo);
				Service.WriteError("Watch dog thread crashed: " + e.ToString(), EventID.DDWatchdogCrash);
			}
			finally
			{
				lock (watchdogLock)
				{
					currentStatus = DreamDaemonStatus.Offline;
					currentPort = 0;
					AwaitingShutdown = ShutdownRequestPhase.None;
					if (!RestartInProgress)
					{
						if(!Properties.Settings.Default.ReattachToDD)
							SendMessage("DD: Server stopped, watchdog exiting...", MessageType.WatchdogInfo);
						Service.WriteInfo("Watch dog exited", EventID.DDWatchdogExit);
					}
					else
						Service.WriteInfo("Watch dog restarting...", EventID.DDWatchdogRestart);
				}
			}
		}

		private void MemTrackTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			ulong megamem;
			float cputime;
			lock (watchdogLock)
			{
				cputime = pcpu.NextValue();
				using (var pcm = new PerformanceCounter("Process", "Working Set - Private", Proc.ProcessName, true))
					megamem = Convert.ToUInt64(pcm.NextValue()) / 1024;
			}
			var PercentCpuTime = (int)Math.Round((Decimal)cputime);
			WriteCurrentDDLog(String.Format("CPU: {1}% Memory: {0}MB", megamem, PercentCpuTime.ToString("D3")));
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
			if (GetVersion(ByondVersion.Installed) == null)
				return "Byond is not installed!";
			var DMB = GameDirLive + "/" + Properties.Settings.Default.ProjectName + ".dmb";
			if (!File.Exists(DMB))
				return String.Format("Unable to find {0}!", DMB);
			return null;
		}

		//public api
		public string Start()
		{
			if (CurrentStatus() == ByondStatus.Staged)
			{
				//IMPORTANT: SLEEP FOR A MOMENT OR WONDOWS WON'T RELEASE THE FUCKING BYOND DLL HANDLES!!!! REEEEEEE
				Thread.Sleep(3000);
				ApplyStagedUpdate();
			}
			lock (watchdogLock)
			{
				if (currentStatus != DreamDaemonStatus.Offline)
					return "Server already running";
				var res = CanStartImpl();
				if (res != null)
					return res;
				currentPort = 0;
				currentStatus = DreamDaemonStatus.HardRebooting;
			}
			return StartImpl(false);
		}

		//translate the configured security level into a byond param
		string SecurityWord(bool starting = false)
		{
			var level = starting ? StartingSecurity : (DreamDaemonSecurity)Properties.Settings.Default.ServerSecurity;
			switch (level)
			{
				case DreamDaemonSecurity.Safe:
					return "safe";
				case DreamDaemonSecurity.Trusted:
					return "trusted";
				case DreamDaemonSecurity.Ultrasafe:
					return "ultrasafe";
				default:
					throw new Exception(String.Format("Bad DreamDaemon security level: {0}", level));
			}
		}

		void UpdateInterfaceDll(bool overwrite)
		{
			if (File.Exists(InterfaceDLLName) && !overwrite)
				return;
			//Copy the interface dll to the static dir
			var InterfacePath = Assembly.GetAssembly(typeof(DreamDaemonBridge)).Location;
			File.Copy(InterfacePath, InterfaceDLLName, overwrite);
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
					StartingSecurity = (DreamDaemonSecurity)Config.ServerSecurity;
					Proc.StartInfo.Arguments = String.Format("{0} -port {1} {5}-close -verbose -params \"server_service={3}&server_service_version={4}\" -{2} -public", DMB, Config.ServerPort, SecurityWord(), serviceCommsKey, Version(), Config.Webclient ? "-webclient " : "");
					UpdateInterfaceDll(true);
					lock (topicLock)
					{
						GameAPIVersion = null;  //needs updating
					}
					Proc.Start();

					if (!Proc.WaitForInputIdle(DDHangStartTime * 1000))
					{
						Proc.Kill();
						Proc.WaitForExit();
						Proc.Close();
						currentStatus = DreamDaemonStatus.Offline;
						currentPort = 0;
						return String.Format("Server start is taking more than {0}s! Aborting!", DDHangStartTime);
					}
					currentPort = Config.ServerPort;
					currentStatus = DreamDaemonStatus.Online;
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
				currentStatus = DreamDaemonStatus.Offline;
				return e.ToString();
			}
		}

		//public api
		public DreamDaemonSecurity SecurityLevel()
		{
			lock (watchdogLock)
			{
				return (DreamDaemonSecurity)Properties.Settings.Default.ServerSecurity;
			}
		}

		//public api
		public bool SetSecurityLevel(DreamDaemonSecurity level)
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
			return DaemonStatus() != DreamDaemonStatus.Online;
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
				case DreamDaemonStatus.Offline:
					res = "OFFLINE";
					break;
				case DreamDaemonStatus.HardRebooting:
					res = "REBOOTING";
					break;
				case DreamDaemonStatus.Online:
					res = "ONLINE";
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
			if (includeMetaInfo && ds != DreamDaemonStatus.Online)
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
			var res = SendCommand(SCWorldAnnounce + ";message=" + Program.SanitizeTopicString(message));
			if (res == "SUCCESS")
				return null;
			return res;
		}

		/// <inheritdoc />
		public bool Webclient()
		{
			return Properties.Settings.Default.Webclient;
		}

		/// <inheritdoc />
		public void SetWebclient(bool on)
		{
			var Config = Properties.Settings.Default;
			lock (watchdogLock) {
				var diff = on != Config.Webclient;
				if (diff)
				{
					Config.Webclient = on;
					RequestRestart();
				}
			}
		}
	}
}
