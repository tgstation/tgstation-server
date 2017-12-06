using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Server
{
	//manages the dd window.
	//It's not possible to actually click it while starting it in CL mode, so in order to change visibility, security, etc. It restarts the process when the world reboots
	sealed partial class Instance : ITGDreamDaemon
	{
		enum ShutdownRequestPhase
		{
			None,
			Requested,
			Pinged,
		}

		/// <summary>
		/// Directory for storing DreamDaemon diagnostic files
		/// </summary>
		const string DiagnosticsDir = "Diagnostics";
		/// <summary>
		/// Directory for storing DreamDaemon ResourceUsage files
		/// </summary>
		const string ResourceDiagnosticsDir = DiagnosticsDir + "/Resources";
		/// <summary>
		/// Time until DD is considered DOA on startup
		/// </summary>
		const int DDHangStartTime = 60;
		/// <summary>
		/// If DreamDaemon crashes before this time it is considered a bad startup
		/// </summary>
		const int DDBadStartTime = 10;

		/// <summary>
		/// The DreamDaemon process
		/// </summary>
		Process Proc;
		/// <summary>
		/// CPU performance information for <see cref="Proc"/>
		/// </summary>
		PerformanceCounter pcpu;

		/// <summary>
		/// Used for multithreading safety
		/// </summary>
		object watchdogLock = new object();
		/// <summary>
		/// The thread that monitors the status of DreamDaemon
		/// </summary>
		Thread DDWatchdog;
		/// <summary>
		/// The <see cref="DreamDaemonStatus"/> of <see cref="Proc"/>
		/// </summary>
		DreamDaemonStatus currentStatus;
		/// <summary>
		/// Current logfile in use in <see cref="ResourceDiagnosticsDir"/>
		/// </summary>
		string CurrentDDLog;
		/// <summary>
		/// Current port DreamDaemon is running on
		/// </summary>
		ushort currentPort = 0;

		/// <summary>
		/// Used for multithreading safety
		/// </summary>
		object restartLock = new object();
		/// <summary>
		/// Used to indicate if an intentional restart is in progress on the watchdog. Requires <see cref="restartLock"/> to access
		/// </summary>
		bool RestartInProgress = false;
		/// <summary>
		/// Used to indicate if an service restart is in progress on the watchdog. Requires <see cref="restartLock"/> to access
		/// </summary>
		bool ReattachInsteadOfRestart = false;

		/// <summary>
		/// Current <see cref="DreamDaemonSecurity"/> level of DreamDaemon
		/// </summary>
		DreamDaemonSecurity StartingSecurity;

		/// <summary>
		/// Indicator of progress on a <see cref="RequestRestart"/> or <see cref="RequestStop"/> operation
		/// </summary>
		ShutdownRequestPhase AwaitingShutdown;

		/// <summary>
		/// Setup or reattach the watchdog, depending on <see cref="InstanceConfig.ReattachRequired"/> of <see cref="Config"/>, and create the <see cref="DiagnosticsDir"/>
		/// </summary>
		void InitDreamDaemon()
		{
			Directory.CreateDirectory(RelativePath(DiagnosticsDir));
			Directory.CreateDirectory(RelativePath(ResourceDiagnosticsDir));
			var Reattach = Config.ReattachRequired;
			if (Reattach)
				try
				{
					Proc = Process.GetProcessById(Config.ReattachProcessID);
					if (Proc == null)
						throw new Exception("GetProcessById returned null!");
					WriteInfo("Reattached to running DD process!", EventID.DDReattachSuccess);
					ThreadPool.QueueUserWorkItem(_ =>
					{
						Thread.Sleep(5000);
						SendMessage("DD: Update complete. Watch dog reactivated...", MessageType.WatchdogInfo);
					});

					//start wd 
					lock (restartLock)
					{
						RestartInProgress = true;
						ReattachInsteadOfRestart = true;
					}
					currentPort = Config.ReattachPort;
					serviceCommsKey = Config.ReattachCommsKey;
					try
					{
						GameAPIVersion = new Version(Config.ReattachAPIVersion);
					}
					catch { }
					currentStatus = DreamDaemonStatus.Online;
					DDWatchdog = new Thread(new ThreadStart(Watchdog));
					DDWatchdog.Start();
				}
				catch (Exception e)
				{
					WriteError(String.Format("Failed to reattach to DreamDaemon! PID: {0}. Exception: {1}", Config.ReattachProcessID, e.ToString()), EventID.DDReattachFail);
				}
				finally
				{
					Config.ReattachRequired = false;
					Config.Save();
				}

			if (Proc == null)
				Proc = new Process();

			Proc.StartInfo.FileName = RelativePath(ByondDirectory + "/bin/dreamdaemon.exe");
			Proc.StartInfo.UseShellExecute = false;

			if (Reattach)
				return;

			//autostart the server
			if (Config.Autostart)
				//break this off so we don't hold up starting the service
				ThreadPool.QueueUserWorkItem(_ => { Start(); });
		}

		/// <summary>
		/// Either let go of <see cref="Proc"/> for reattachment or terminate it, depending on <see cref="InstanceConfig.ReattachRequired"/> of <see cref="Config"/>
		/// </summary>
		void DisposeDreamDaemon()
		{
			var Detach = Config.ReattachRequired;
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
				Config.ReattachRequired = false;
			Stop();
			if (pcpu != null)
				pcpu.Dispose();
			if (RenameLog)
				try
				{
					var rrdd = RelativePath(ResourceDiagnosticsDir);
					File.Move(Path.Combine(rrdd, CurrentDDLog), Path.Combine(rrdd, "SU-" + CurrentDDLog));
				}
				catch { }
		}

		/// <inheritdoc />
		public DreamDaemonStatus DaemonStatus()
		{
			lock (watchdogLock)
			{
				return currentStatus;
			}
		}

		/// <inheritdoc />
		public void RequestRestart()
		{
			SendCommand(SCHardReboot);
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
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

		/// <inheritdoc />
		public void SetPort(ushort new_port)
		{
			lock (watchdogLock)
			{
				Config.Port = new_port;
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

		/// <inheritdoc />
		public string Restart()
		{
			if (DaemonStatus() == DreamDaemonStatus.Offline)
				return Start();
			lock (restartLock)
			{
				if (RestartInProgress)
					return "Restart already in progress";
				RestartInProgress = true;
			}
			SendMessage("DD: Hard restart triggered", MessageType.WatchdogInfo);
			Stop();
			var res = Start();
			if (res != null)
				lock (restartLock)
				{
					RestartInProgress = false;
				}
			return res;
		}

		/// <summary>
		/// Write a <paramref name="message"/> to the <see cref="CurrentDDLog"/>. A timestamp will be prepended to it
		/// </summary>
		/// <param name="message">The message to log</param>
		void WriteCurrentDDLog(string message)
		{
			lock (watchdogLock)
			{
				if (currentStatus != DreamDaemonStatus.Online || CurrentDDLog == null)
					return;
				File.AppendAllText(Path.Combine(RelativePath(ResourceDiagnosticsDir), CurrentDDLog), String.Format("[{0}]: {1}\n", DateTime.Now.ToLongTimeString(), message));
			}
		}

		/// <summary>
		/// Threaded loop that keeps DreamDaemon from unintentionally stopping
		/// </summary>
		void Watchdog()
		{
			try
			{
				lock (restartLock)
				{
					if (!RestartInProgress)
					{
						SendMessage("DD: Server started, watchdog active...", MessageType.WatchdogInfo);
						WriteInfo("Watchdog started", EventID.DDWatchdogStarted);
					}
					else
					{
						RestartInProgress = false;
						if (!ReattachInsteadOfRestart)
							WriteInfo("Watchdog restarted", EventID.DDWatchdogRestarted);
						else
							ReattachInsteadOfRestart = false;
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
						CurrentDDLog = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
						WriteCurrentDDLog("Starting monitoring...");
					}
					try
					{
						pcpu = new PerformanceCounter("Process", "% Processor Time", Proc.ProcessName, true);
						MemTrackTimer.Start();
					}
					catch (InvalidOperationException) { /*process already exited*/ }
					try
					{
						Proc.WaitForExit();
					}
					finally
					{
						lock (watchdogLock) //synchronize
						{
							MemTrackTimer.Stop();
							pcpu?.Dispose();
						}
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
							WriteWarning(msg, EventID.DDWatchdogRebootingServer);
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
					if (!Config.ReattachRequired)
					{
						Proc.Kill();
						Proc.WaitForExit();
					}
					else
					{
						Config.ReattachProcessID = Proc.Id;
						Config.ReattachPort = currentPort;
						Config.ReattachAPIVersion = GameAPIVersion != null ? GameAPIVersion.ToString() : null;
						Config.ReattachCommsKey = serviceCommsKey;
						lock (restartLock)
						{
							RestartInProgress = true;
						}
					}
					Proc.Close();
				}
				catch
				{ }
			}
			catch (Exception e)
			{
				SendMessage("DD: Watchdog thread crashed!", MessageType.WatchdogInfo);
				WriteError("Watch dog thread crashed: " + e.ToString(), EventID.DDWatchdogCrash);
			}
			finally
			{
				lock (watchdogLock)
				{
					currentStatus = DreamDaemonStatus.Offline;
					currentPort = 0;
					AwaitingShutdown = ShutdownRequestPhase.None;
					lock (restartLock)
					{
						if (!RestartInProgress)
						{
							if (!Config.ReattachRequired)
								SendMessage("DD: Server stopped, watchdog exiting...", MessageType.WatchdogInfo);
							WriteInfo("Watch dog exited", EventID.DDWatchdogExit);
						}
						else
							WriteInfo("Watch dog restarting...", EventID.DDWatchdogRestart);
					}
				}
			}
		}

		/// <summary>
		/// Called every five seconds while DreamDaemon is running to log it's current state to the <see cref="DiagnosticsDir"/>
		/// </summary>
		/// <param name="sender">The event sender, an instance of <see cref="System.Timers.Timer"/></param>
		/// <param name="e">The <see cref="ElapsedEventArgs"/></param>
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
			WriteCurrentDDLog(String.Format("CPU: {1}% Memory: {0}KB", megamem, PercentCpuTime.ToString("D3")));
		}

		/// <inheritdoc />
		public string CanStart()
		{
			if (GetVersion(ByondVersion.Installed) == null)
				return "Byond is not installed!";
			var DMB = RelativePath(GameDirLive + "/" + Config.ProjectName + ".dmb");
			if (!File.Exists(DMB))
				return String.Format("Unable to find {0}!", DMB);
			return null;
		}

		/// <inheritdoc />
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
				var res = CanStart();
				if (res != null)
					return res;
				currentPort = 0;
				currentStatus = DreamDaemonStatus.HardRebooting;
			}
			return StartImpl(false);
		}

		/// <summary>
		/// Translate the configured <see cref="DreamDaemonSecurity"/> level into a byond command line param
		/// </summary>
		/// <param name="starting">If <see langword="true"/> bases it's result on <see cref="StartingSecurity"/>, uses <see cref="InstanceConfig.Security"/> of <see cref="Config"/> otherwise</param>
		/// <returns>"safe", "trusted", or "ultrasafe" depending on the <see cref="DreamDaemonSecurity"/> it checks</returns>
		string SecurityWord(bool starting = false)
		{
			var level = starting ? StartingSecurity : Config.Security;
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

		/// <summary>
		/// Copies <see cref="BridgeDLLName"/> from the program directory to the the <see cref="Instance"/> directory
		/// </summary>
		/// <param name="overwrite">If <see langword="true"/>, overwrites the <see cref="Instance"/>'s current interface .dll if it exists</param>
		void UpdateBridgeDll(bool overwrite)
		{
			var rbdlln = RelativePath(BridgeDLLName);
			var FileExists = File.Exists(rbdlln);
			if (FileExists && !overwrite)
				return;
			//Copy the interface dll to the static dir

			var InterfacePath = Assembly.GetAssembly(typeof(IServerInterface)).Location;
			//bridge is installed next to the interface
			var BridgePath = Path.Combine(Path.GetDirectoryName(InterfacePath), BridgeDLLName);
#if DEBUG
			//We could be debugging from the project directory
			if (!File.Exists(BridgePath))
				//A little hackish debug mode doctoring never hurt anyone
				BridgePath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(InterfacePath)))), "TGS.Interface.Bridge/bin/x86/Debug", BridgeDLLName);
#endif
			try
			{
				//Use reflection to ensure these are the droids we're looking for
				Assembly.ReflectionOnlyLoadFrom(BridgePath).GetType(String.Format("{0}.{1}.{2}.{3}", nameof(TGS), nameof(Interface), DreamDaemonBridgeNamespace, DreamDaemonBridgeType), true);
			}
			catch (Exception e)
			{
				WriteError(String.Format("Unable to locate {0}! Error: {1}", BridgeDLLName, e.ToString()), EventID.BridgeDLLUpdateFail);
				return;
			}

			try
			{
				if (FileExists)
				{
					var Old = File.ReadAllBytes(rbdlln);
					var New = File.ReadAllBytes(BridgePath);
					if (Old.SequenceEqual(New))
						return; //no need
				}
				File.Copy(BridgePath, rbdlln, overwrite);
			}
			catch
			{
				try
				{
					//ok the things being stupid and hasn't released the dll yet, try ONCE more
					Thread.Sleep(1000);
					File.Copy(BridgePath, rbdlln, overwrite);
				}
				catch (Exception e)
				{
					//intentionally using the fi
					WriteError("Failed to update bridge DLL! Error: " + e.ToString(), EventID.BridgeDLLUpdateFail);
					return;
				}
			}
			WriteInfo("Updated interface DLL", EventID.BridgeDLLUpdated);
		}

		/// <summary>
		/// Clears the current <see cref="GameAPIVersion"/>, calls <see cref="UpdateBridgeDll(bool)"/> with a <see langword="true"/> parameter, and attempts to start the DreamDaemon <see cref="Proc"/>
		/// </summary>
		/// <param name="watchdog">If <see langword="false"/>, sets <see cref="DDWatchdog"/> to a new <see cref="Thread"/> pointing to <see cref="Watchdog"/> and starts it</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string StartImpl(bool watchdog)
		{
			try
			{
				lock (watchdogLock)
				{
					var res = CanStart();
					if (res != null)
						return res;
					
					var DMB = RelativePath(GameDirLive + "/" + Config.ProjectName + ".dmb");

					GenCommsKey();
					StartingSecurity = Config.Security;
					Proc.StartInfo.Arguments = String.Format("{0} -port {1} {5}-close -verbose -params \"server_service={3}&server_service_version={4}&{6}={7}\" -{2} -public", DMB, Config.Port, SecurityWord(), serviceCommsKey, Version(), Config.Webclient ? "-webclient " : "", SPInstanceName, Config.Name);
					UpdateBridgeDll(true);
					lock (topicLock)
					{
						GameAPIVersion = null;  //needs updating
					}
					Proc.Start();
					Proc.PriorityClass = ProcessPriorityClass.AboveNormal;

					if (!Proc.WaitForInputIdle(DDHangStartTime * 1000))
					{
						Proc.Kill();
						Proc.WaitForExit();
						Proc.Close();
						currentStatus = DreamDaemonStatus.Offline;
						currentPort = 0;
						return String.Format("Server start is taking more than {0}s! Aborting!", DDHangStartTime);
					}
					currentPort = Config.Port;
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

		/// <inheritdoc />
		public DreamDaemonSecurity SecurityLevel()
		{
			lock (watchdogLock)
			{
				return Config.Security;
			}
		}

		/// <inheritdoc />
		public bool SetSecurityLevel(DreamDaemonSecurity level)
		{
			bool needReboot;
			lock (watchdogLock)
			{
				needReboot = Config.Security != level;
				Config.Security = level;
			}
			if (needReboot)
				RequestRestart();
			return DaemonStatus() != DreamDaemonStatus.Online;
		}

		/// <inheritdoc />
		public bool Autostart()
		{
			return Config.Autostart;
		}

		/// <inheritdoc />
		public void SetAutostart(bool on)
		{
			Config.Autostart = on;
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public ushort Port()
		{
			return Config.Port;
		}

		/// <inheritdoc />
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
			var res = SendCommand(SCWorldAnnounce + ";message=" + Helpers.SanitizeTopicString(message));
			if (res == "SUCCESS")
				return null;
			return res;
		}

		/// <inheritdoc />
		public bool Webclient()
		{
			return Config.Webclient;
		}

		/// <inheritdoc />
		public void SetWebclient(bool on)
		{
			lock (watchdogLock)
			{
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
