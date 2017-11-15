using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	sealed class DreamDaemonManager : ITGDreamDaemon, IDisposable
	{
		/// <summary>
		/// Graceful shutdown state used with <see cref="RequestRestart"/>
		/// </summary>
		enum ShutdownRequestPhase
		{
			/// <summary>
			/// There is no shutdown request
			/// </summary>
			None,
			/// <summary>
			/// <see cref="RequestRestart"/> was called but <see cref="currentStatus"/> was <see cref="DreamDaemonStatus.HardRebooting"/> and the request needs to be delayed
			/// </summary>
			Requested,
			/// <summary>
			/// An <see cref="InteropCommand.ShutdownOnWorldReboot"/> request was sent to DreamDaemon
			/// </summary>
			Pinged,
		}

		/// <summary>
		/// Directory for storing DreamDaemon diagnostic files
		/// </summary>
		const string DiagnosticsDir = "Diagnostics";
		/// <summary>
		/// Time until DD is considered DOA on startup
		/// </summary>
		const int DDHangStartTime = 60;
		/// <summary>
		/// If DreamDaemon crashes before this time it is considered a bad startup
		/// </summary>
		const int DDBadStartTime = 10;

		/// <summary>
		/// Directory for storing resource usage files
		/// </summary>
		static readonly string ResourceDiagnosticsDir = Path.Combine(DiagnosticsDir, "Resources");

		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
		readonly IIOManager IO;
		/// <summary>
		/// The <see cref="IChatBroadcaster"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
		readonly IChatBroadcaster Chat;
		/// <summary>
		/// The <see cref="IInstanceConfig"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
		readonly IInstanceConfig Config;
		/// <summary>
		/// The <see cref="IInteropManager"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
		readonly IInteropManager Interop;
		readonly IByondManager Byond;

		/// <summary>
		/// The DreamDaemon process
		/// </summary>
		Process process;
		/// <summary>
		/// CPU performance information for <see cref="process"/>
		/// </summary>
		PerformanceCounter pcpu;
		/// <summary>
		/// The task that runs the <see cref="Watchdog"/>
		/// </summary>
		Task watchdogTask;
		/// <summary>
		/// The thread that monitors the status of DreamDaemon
		/// </summary>
		CancellationTokenSource watchdogCancellationToken;

		/// <summary>
		/// The <see cref="DreamDaemonStatus"/> of <see cref="process"/>
		/// </summary>
		DreamDaemonStatus currentStatus;
		/// <summary>
		/// Current <see cref="DreamDaemonSecurity"/> level of DreamDaemon
		/// </summary>
		DreamDaemonSecurity StartingSecurity;
		/// <summary>
		/// Indicator of progress on a <see cref="RequestRestart"/> or <see cref="RequestStop"/> operation
		/// </summary>
		ShutdownRequestPhase AwaitingShutdown;
		/// <summary>
		/// Current logfile in use in <see cref="ResourceDiagnosticsDir"/>
		/// </summary>
		string CurrentDDLog;
		/// <summary>
		/// Current port DreamDaemon is running on
		/// </summary>
		ushort currentPort;
		/// <summary>
		/// Used to indicate if an intentional restart is in progress on the watchdog
		/// </summary>
		bool RestartInProgress;
		/// <summary>
		/// Used to indicate if an service restart is in progress on the watchdog
		/// </summary>
		bool ReattachInsteadOfRestart;

		/// <summary>
		/// Construct a <see cref="DreamDaemonManager"/>, reattaches the watchdog based on <paramref name="config"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="io">The value of <see cref="IO"/></param>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		/// <param name="config">The value of <see cref="Config"/></param>
		/// <param name="interop">The value of <see cref="Interop"/></param>
		public DreamDaemonManager(IInstanceLogger logger, IIOManager io, IChatBroadcaster chat, IInstanceConfig config, IInteropManager interop)
		{
			Logger = logger;
			IO = io;
			Chat = chat;
			Config = config;
			Interop = interop;

			Interop.OnKillRequest += () => HandleKillRequest();

			IO.CreateDirectory(DiagnosticsDir);
			IO.CreateDirectory(ResourceDiagnosticsDir);

			if (!Config.ReattachRequired || !HandleReattach())
				process = new Process();
			
			process.StartInfo.UseShellExecute = false;

			if (Config.Autostart)
				Start();
		}

		/// <summary>
		/// Attempts to reattach to a running DreamDaemon executable
		/// </summary>
		/// <returns><see langword="true"/> on successful reattach, <see langword="false"/> otherwise</returns>
		bool HandleReattach()
		{
			try
			{
				process = Process.GetProcessById(Config.ReattachProcessID);
				if (process == null)
					throw new Exception("GetProcessById returned null!");

				RestartInProgress = true;
				ReattachInsteadOfRestart = true;
				currentPort = Config.ReattachPort;
				Interop.CommunicationsKey = Config.ReattachCommsKey;
				currentStatus = DreamDaemonStatus.Online;

				StartWatchdogTask();

				Logger.WriteInfo("Reattached to running DD process!", EventID.DDReattachSuccess);
				Chat.SendMessage("DD: Update complete. Watch dog reactivated...", MessageType.WatchdogInfo);
				return true;
			}
			catch (Exception e)
			{
				Logger.WriteError(String.Format("Failed to reattach to DreamDaemon! PID: {0}. Exception: {1}", Config.ReattachProcessID, e.ToString()), EventID.DDReattachFail);
				return false;
			}
			finally
			{
				Config.ReattachRequired = false;
			}
		}

		/// <summary>
		/// Sets <see cref="watchdogCancellationToken"/> and sets and starts <see cref="watchdogTask"/>
		/// </summary>
		void StartWatchdogTask()
		{
			watchdogCancellationToken = new CancellationTokenSource();
			watchdogTask = Task.Factory.StartNew(() => Watchdog(watchdogCancellationToken.Token));
		}

		/// <summary>
		/// Either let go of <see cref="process"/> for reattachment or terminate it, depending on the <see cref="IInstanceConfig.ReattachRequired"/> value of <see cref="Config"/>
		/// </summary>
		public void Dispose()
		{
			var Detach = Config.ReattachRequired;
			bool RenameLog = false;
			if (DaemonStatus() == DreamDaemonStatus.Online)
			{
				if (!Detach)
				{
					WorldAnnounce("Server service stopped");
					Task.Delay(1000);
				}
				else
				{
					RenameLog = CurrentDDLog != null;
					Chat.SendMessage("DD: Detaching watch dog for update!", MessageType.WatchdogInfo);
					WriteCurrentDDLog("Detaching DreamDaemon! Splitting diagnostics file...");
				}
			}
			else if (Detach)
				Config.ReattachRequired = false;

			Stop();

			if (pcpu != null)
			{
				pcpu.Dispose();
				pcpu = null;
			}

			if(process != null)
			{
				process.Dispose();
				process = null;
			}

			if (RenameLog)
				try
				{
					IO.MoveFile(Path.Combine(ResourceDiagnosticsDir, CurrentDDLog), Path.Combine(ResourceDiagnosticsDir, String.Format("SU-{0}", CurrentDDLog)));
				}
				catch { }
		}

		/// <inheritdoc />
		public DreamDaemonStatus DaemonStatus()
		{
			lock (this)
				return currentStatus;
		}

		/// <inheritdoc />
		public void RequestRestart()
		{
			Interop.SendCommand(InteropCommand.RestartOnWorldReboot);
		}

		/// <inheritdoc />
		public void RequestStop()
		{
			Interop.SendCommand(InteropCommand.ShutdownOnWorldReboot);
			lock (this)
			{
				if (currentStatus != DreamDaemonStatus.Online || AwaitingShutdown != ShutdownRequestPhase.None)
					return;
				AwaitingShutdown = ShutdownRequestPhase.Pinged;
			}
		}

		/// <inheritdoc />
		public string Stop()
		{
			Task t;
			CancellationTokenSource cts;
			lock (this)
			{
				cts = watchdogCancellationToken;
				watchdogCancellationToken = null;
				t = watchdogTask;
				watchdogTask = null;
			}
			if (cts != null)
			{
				cts.Cancel();
				cts.Dispose();
			}
			if (t != null)
			{
				t.Wait();
				t.Dispose();
				return null;
			}
			else
				return "Error, DreamDaemon not running";
		}

		/// <inheritdoc />
		public void SetPort(ushort new_port)
		{
			Config.Port = new_port;
			RequestRestart();
		}

		/// <summary>
		/// Event handler for <see cref="IInteropManager.OnKillRequest"/>
		/// </summary>
		void HandleKillRequest()
		{
			bool DoRestart;
			lock (this)
			{
				DoRestart = AwaitingShutdown == ShutdownRequestPhase.None;
				if (!DoRestart)
					AwaitingShutdown = ShutdownRequestPhase.Pinged;
			}
			//Do this is a seperate thread or we'll kill this thread in the middle of rebooting
			Task.Factory.StartNew(() => DoRestart ? Restart() : Stop());
		}

		/// <inheritdoc />
		public string Restart()
		{
			lock (this)
			{
				if (DaemonStatus() == DreamDaemonStatus.Offline)
					return Start();
				if (RestartInProgress)
					return "Restart already in progress";
				RestartInProgress = true;
			}
			Chat.SendMessage("DD: Hard restart triggered", MessageType.WatchdogInfo);
			Stop();
			var res = Start();
			if (res != null)
				RestartInProgress = false;
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
					pcpu = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
					MemTrackTimer.Start();
					try
					{
						process.WaitForExit();
					}
					finally
					{
						lock (watchdogLock) //synchronize
						{
							MemTrackTimer.Stop();
							pcpu.Dispose();
						}
					}

					WriteCurrentDDLog("Crash detected!");

					lock (watchdogLock)
					{
						currentStatus = DreamDaemonStatus.HardRebooting;
						currentPort = 0;
						process.Close();

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
						process.Kill();
						process.WaitForExit();
					}
					else
					{
						Config.ReattachProcessID = process.Id;
						Config.ReattachPort = currentPort;
						Config.ReattachCommsKey = serviceCommsKey;
						lock (restartLock)
						{
							RestartInProgress = true;
						}
					}
					process.Close();
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
				using (var pcm = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName, true))
					megamem = Convert.ToUInt64(pcm.NextValue()) / 1024;
			}
			var PercentCpuTime = (int)Math.Round((Decimal)cputime);
			WriteCurrentDDLog(String.Format("CPU: {1}% Memory: {0}MB", megamem, PercentCpuTime.ToString("D3")));
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
		/// Clears the current <see cref="GameAPIVersion"/>, calls <see cref="UpdateBridgeDll(bool)"/> with a <see langword="true"/> parameter, and attempts to start the DreamDaemon <see cref="process"/>
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
					process.StartInfo.Arguments = String.Format("{0} -port {1} {5}-close -verbose -params \"server_service={3}&server_service_version={4}&{6}={7}\" -{2} -public", DMB, Config.Port, SecurityWord(), serviceCommsKey, Version(), Config.Webclient ? "-webclient " : "", SPInstanceName, Config.Name);
					UpdateBridgeDll(true);
					lock (topicLock)
					{
						GameAPIVersion = null;  //needs updating
					}
					process.Start();
					process.PriorityClass = ProcessPriorityClass.AboveNormal;

					if (!process.WaitForInputIdle(DDHangStartTime * 1000))
					{
						process.Kill();
						process.WaitForExit();
						process.Close();
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
