using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;
using TGS.Server.Configuration;
using TGS.Server.IO;
using TGS.Server.Logging;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	sealed class DreamDaemonManager : IDreamDaemonManager, IDisposable
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
		static readonly string ResourceDiagnosticsDir = IOManager.ConcatPath(DiagnosticsDir, "Resources");

		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
		readonly IIOManager IO;
		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
		readonly IChatManager Chat;
		/// <summary>
		/// The <see cref="IInstanceConfig"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
		readonly IInstanceConfig Config;
		/// <summary>
		/// The <see cref="IInteropManager"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
		readonly IInteropManager Interop;
		/// <summary>
		/// The <see cref="IByondManager"/> for the <see cref="DreamDaemonManager"/>
		/// </summary>
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
		/// <param name="byond">The value of <see cref="Byond"/></param>
		public DreamDaemonManager(IInstanceLogger logger, IIOManager io, IChatManager chat, IInstanceConfig config, IInteropManager interop, IByondManager byond)
		{
			Logger = logger;
			IO = io;
			Chat = chat;
			Config = config;
			Interop = interop;
			Byond = byond;

			Interop.OnKillRequest += (a, b) => HandleKillRequest();
			Interop.OnWorldReboot += (a, b) => WriteCurrentDDLog("World rebooted.");

			IO.CreateDirectory(DiagnosticsDir).Wait();
			IO.CreateDirectory(ResourceDiagnosticsDir).Wait();

			if (!Config.ReattachRequired || !HandleReattach())
				process = new Process();
			
			process.StartInfo.UseShellExecute = false;

			if (Config.Autostart)
				Start();
		}

		/// <summary>
		/// Attempts to reattach to a running DreamDaemon executable. Not thread safe
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
				Interop.SetCommunicationsKey(Config.ReattachCommsKey);
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
					Interop.WorldAnnounce("Server service stopped");
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
					IO.MoveFile(IOManager.ConcatPath(ResourceDiagnosticsDir, CurrentDDLog), IOManager.ConcatPath(ResourceDiagnosticsDir, String.Format("SU-{0}", CurrentDDLog)), false).Wait();
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
			lock (this)
				if (Config.Port != new_port)
				{
					Config.Port = new_port;
					RequestRestart();
				}
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
				if (currentStatus == DreamDaemonStatus.Offline)
					return Start();

				if (RestartInProgress)
					return "Restart already in progress";
				RestartInProgress = true;

				Chat.SendMessage("DD: Hard restart triggered", MessageType.WatchdogInfo);

				Stop();

				var res = Start();
				if (res != null)
					RestartInProgress = false;
				return res;
			}
		}

		/// <summary>
		/// Write a <paramref name="message"/> to the <see cref="CurrentDDLog"/>. A timestamp will be prepended to it
		/// </summary>
		/// <param name="message">The message to log</param>
		void WriteCurrentDDLog(string message)
		{
			lock (this)
			{
				if (currentStatus != DreamDaemonStatus.Online || CurrentDDLog == null)
					return;
				IO.AppendAllText(IOManager.ConcatPath(ResourceDiagnosticsDir, CurrentDDLog), String.Format("[{0}]: {1}\n", DateTime.Now.ToLongTimeString(), message)).Wait();
			}
		}

		/// <summary>
		/// Loop that keeps DreamDaemon from unintentionally stopping
		/// </summary>
		void Watchdog(CancellationToken cancellationToken)
		{
			try
			{
				lock (this)
				{
					if (!RestartInProgress)
					{
						Chat.SendMessage("DD: Server started, watchdog active...", MessageType.WatchdogInfo);
						Logger.WriteInfo("Watchdog started", EventID.DDWatchdogStarted);
					}
					else
					{
						RestartInProgress = false;
						if (!ReattachInsteadOfRestart)
							Logger.WriteInfo("Watchdog restarted", EventID.DDWatchdogRestarted);
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
				MemTrackTimer.Elapsed += (a, b) => LogCurrentResoruceUsage();
				while (true)
				{
					var starttime = DateTime.Now;

					lock (this)
					{
						if (AwaitingShutdown == ShutdownRequestPhase.Requested)
							Interop.SendCommand(InteropCommand.ShutdownOnWorldReboot);

						CurrentDDLog = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
						WriteCurrentDDLog("Starting monitoring...");
					}

					pcpu = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
					MemTrackTimer.Start();

					try
					{
						try
						{
							process.WaitForExitAsync(cancellationToken).Wait();
						}
						catch { }
						cancellationToken.ThrowIfCancellationRequested();
					}
					finally
					{
						lock (this) //synchronize
						{
							MemTrackTimer.Stop();
							pcpu.Dispose();
						}
					}

					lock (this)
					{
						WriteCurrentDDLog("Crash detected!");
						currentStatus = DreamDaemonStatus.HardRebooting;
						Interop.ResetDMAPIVersion();
						process.Close();

						Byond.UnlockDDExecutable();

						if (AwaitingShutdown == ShutdownRequestPhase.Pinged)
							return;
						var BadStart = (DateTime.Now - starttime).TotalSeconds < DDBadStartTime;
						if (BadStart)
						{
							++retries;
							var sleep_time = (int)Math.Min(Math.Pow(2, retries), 3600); //max of one hour
							Chat.SendMessage(String.Format("DD: Watchdog server startup failed! Retrying in {0} seconds...", sleep_time), MessageType.WatchdogInfo);
							Thread.Sleep(sleep_time * 1000);
						}
						else
						{
							retries = 0;
							var msg = "DD: DreamDaemon crashed! Watchdog rebooting DD...";
							Chat.SendMessage(msg, MessageType.WatchdogInfo);
							Logger.WriteWarning(msg, EventID.DDWatchdogRebootingServer);
						}
					}

					var res = StartImpl(true);
					if (res != null)
						throw new Exception("Hard restart failed: " + res);
				}
			}
			catch (OperationCanceledException)
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
						lock (this)
							RestartInProgress = true;
					}
					process.Close();
				}
				catch
				{ }
			}
			catch (Exception e)
			{
				Chat.SendMessage("DD: Watchdog thread crashed!", MessageType.WatchdogInfo);
				Logger.WriteError("Watch dog thread crashed: " + e.ToString(), EventID.DDWatchdogCrash);
			}
			finally
			{
				lock (this)
				{
					currentStatus = DreamDaemonStatus.Offline;
					Interop.ResetDMAPIVersion();
					AwaitingShutdown = ShutdownRequestPhase.None;

					if (!RestartInProgress)
					{
						if (!Config.ReattachRequired)
							Chat.SendMessage("DD: Server stopped, watchdog exiting...", MessageType.WatchdogInfo);
						Logger.WriteInfo("Watch dog exited", EventID.DDWatchdogExit);
					}
					else
						Logger.WriteInfo("Watch dog restarting...", EventID.DDWatchdogRestart);
				}
			}
		}

		/// <summary>
		/// Called every five seconds while DreamDaemon is running to log it's current state to the <see cref="DiagnosticsDir"/>
		/// </summary>
		void LogCurrentResoruceUsage()
		{
			ulong megamem;
			float cputime;
			lock (this)
			{
				cputime = pcpu.NextValue();
				using (var pcm = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName, true))
					megamem = Convert.ToUInt64(pcm.NextValue()) / 1024;
				var PercentCpuTime = (int)Math.Round((Decimal)cputime);
				WriteCurrentDDLog(String.Format("CPU: {1}% Memory: {0}KB", megamem, PercentCpuTime.ToString("D3")));
			}
		}

		/// <inheritdoc />
		public string Start()
		{
			if (Byond.CurrentStatus() == ByondStatus.Staged)
			{
				//IMPORTANT: SLEEP FOR A MOMENT OR WONDOWS WON'T RELEASE THE FUCKING BYOND DLL HANDLES!!!! REEEEEEE
				Thread.Sleep(3000);
				Byond.ApplyStagedUpdate();
			}
			lock (this)
			{
				if(currentStatus != DreamDaemonStatus.Offline)
					return "Server already running";
				currentStatus = DreamDaemonStatus.HardRebooting;
				return StartImpl(false);
			}
		}

		/// <summary>
		/// Translate the <see cref="StartingSecurity"/> level into a BYOND command line param
		/// </summary>
		/// <returns>"safe", "trusted", or "ultrasafe" depending on <see cref="StartingSecurity"/></returns>
		string SecurityWord()
		{
			var level = StartingSecurity;
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
		/// Calls <see cref="IInteropManager.ResetDMAPIVersion"/>, <see cref="IInteropManager.SetCommunicationsKey(string)"/> with a <see langword="null"/> parameter, and <see cref="IInteropManager.UpdateBridgeDll(bool)"/> with a <see langword="true"/> parameter. Attempts to start the DreamDaemon <see cref="process"/>
		/// </summary>
		/// <param name="watchdog">If <see langword="false"/>, calls <see cref="StartWatchdogTask"/></param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string StartImpl(bool watchdog)
		{
			try
			{
				lock (this)
				{
					Interop.UpdateBridgeDll(true);
					Interop.SetCommunicationsKey();
					Interop.ResetDMAPIVersion();
					Interop.UpdateBridgeDll(true);

					var DMB = IOManager.ConcatPath(CompilerManager.GameDirLive, String.Format("{0}.dmb", Config.ProjectName));

					StartingSecurity = Config.Security;

					process.StartInfo.FileName = Byond.LockDDExecutable(out string error);
					if (error != null)
						return error;
					process.StartInfo.Arguments = String.Format("{0} -port {1} {4}-close -verbose -params \"{3}\" -{2} -public", IO.ResolvePath(DMB), Config.Port, SecurityWord(), Interop.StartParameters(), Config.Webclient ? "-webclient " : "");

					
					process.Start();
					process.PriorityClass = ProcessPriorityClass.AboveNormal;

					if (!process.WaitForInputIdle(DDHangStartTime * 1000))
					{
						process.Kill();
						process.WaitForExit();
						process.Close();
						currentStatus = DreamDaemonStatus.Offline;
						return String.Format("Server start is taking more than {0}s! Aborting!", DDHangStartTime);
					}

					Interop.TopicPort = Config.Port;

					currentStatus = DreamDaemonStatus.Online;
					if (!watchdog)
						StartWatchdogTask();
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
			return Config.Security;
		}

		/// <inheritdoc />
		public bool SetSecurityLevel(DreamDaemonSecurity level)
		{
			lock (this)
			{
				if (Config.Security != level)
				{
					Config.Security = level;
					RequestRestart();
				}
				return currentStatus != DreamDaemonStatus.Online;
			}
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
					break;
				default:
					res = "NULL AND ERRORS";
					break;
			}
			if (includeMetaInfo)
			{
				string sec;
				lock (this)
					sec = SecurityWord();
				res = String.Format("{0} (Sec: {1})", res, sec);
			}
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
			lock (this)
				return AwaitingShutdown != ShutdownRequestPhase.None;
		}

		/// <inheritdoc />
		public bool Webclient()
		{
			return Config.Webclient;
		}

		/// <inheritdoc />
		public void SetWebclient(bool on)
		{
			lock (this)
				if (on != Config.Webclient)
				{
					Config.Webclient = on;
					RequestRestart();
				}
		}

		/// <inheritdoc />
		public void RunSuspended(Action action)
		{
			lock (this)
			{
				//gotta go fast
				var online = currentStatus == DreamDaemonStatus.Online;
				if (online)
					process.Suspend();
				try
				{
					action.Invoke();
				}
				finally
				{
					if (online && !process.HasExited)
						process.Resume();
				}
			}
		}
	}
}
