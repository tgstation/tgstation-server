using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using TGServiceInterface;

namespace TGServerService
{
	partial class TGStationServer : ITGCompiler
	{
		#region Win32 Shit
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);
		enum SymbolicLink
		{
			File = 0,
			Directory = 1
		}


		//translates the win32 api call into an exception if it fails
		static void CreateSymlink(string link, string target)
		{
			if (!CreateSymbolicLink(new DirectoryInfo(link).FullName, new DirectoryInfo(target).FullName, File.Exists(target) ? SymbolicLink.File : SymbolicLink.Directory))
				throw new Exception(String.Format("Failed to create symlink from {0} to {1}! Error: {2}", target, link, Marshal.GetLastWin32Error()));
		}
		#endregion

		const string StaticDirs = "Static";
		const string StaticDataDir = StaticDirs + "/data";
		const string StaticConfigDir = StaticDirs + "/config";
		const string StaticBackupDir = "Static_BACKUP";

		const string LibMySQLFile = "/libmysql.dll";

		const string GameDir = "Game";
		const string GameDirA = GameDir + "/A";
		const string GameDirB = GameDir + "/B";
		const string GameDirLive = GameDir + "/Live";

		const string LiveFile = "/TestLive.lk";
		const string ADirTest = GameDirA + LiveFile;
		const string BDirTest = GameDirB + LiveFile;
		const string LiveDirTest = GameDirLive + LiveFile;

		List<string> copyExcludeList = new List<string> { ".git", "data", "config", "libmysql.dll" };   //shit we handle
		List<string> deleteExcludeList = new List<string> { "data", "config", "libmysql.dll" };   //shit we handle

		object CompilerLock = new object();
		TGCompilerStatus compilerCurrentStatus;
		string lastCompilerError;
		
		Thread CompilerThread;
		bool compilationCancellationRequestation = false;
		bool canCancelCompilation = false;
		bool silentCompile = false;

		bool UpdateStaged = false;

		//deletes leftovers and checks current status
		void InitCompiler()
		{
			if(File.Exists(PrepPath(LiveDirTest)))
				File.Delete(PrepPath(LiveDirTest));
			compilerCurrentStatus = IsInitialized();
		}

		//public api
		public TGCompilerStatus GetStatus()
		{
			lock (CompilerLock)
			{
				return compilerCurrentStatus;
			}
		}

		//public api
		public string CompileError()
		{
			lock (CompilerLock)
			{
				var err = lastCompilerError;
				lastCompilerError = null;
				return err;
			}
		}

		//kills the compiler if its running
		void DisposeCompiler()
		{
			lock (CompilerLock)
			{
				if (CompilerThread == null || !CompilerThread.IsAlive)
					return;
				CompilerThread.Abort(); //this will safely kill dm
				InitCompiler();	//also cleanup
			}
		}

		//requires CompilerLock to be locked
		bool CompilerIdleNoLock()
		{
			return compilerCurrentStatus == TGCompilerStatus.Uninitialized || compilerCurrentStatus == TGCompilerStatus.Initialized;
		}
		
		//public api
		public bool Initialize()
		{
			lock (CompilerLock)
			{
				if (!CompilerIdleNoLock())
					return false;
				lastCompilerError = null;
				compilerCurrentStatus = TGCompilerStatus.Initializing;
				CompilerThread = new Thread(new ThreadStart(InitializeImpl));
				CompilerThread.Start();
				return true;
			}
		}

		//what is says on the tin
		TGCompilerStatus IsInitialized()
		{
			if (File.Exists(PrepPath(GameDirLive + LibMySQLFile)))	//its a good tell, jim
				return TGCompilerStatus.Initialized;
			return TGCompilerStatus.Uninitialized;
		}

		//we need to remove symlinks before we can recursively delete
		public void CleanGameFolder()
		{
			if (Directory.Exists(PrepPath(GameDirB + LibMySQLFile)))
				Directory.Delete(PrepPath(GameDirB + LibMySQLFile));

			if (Directory.Exists(PrepPath(GameDirA + "/data")))
				Directory.Delete(PrepPath(GameDirA + "/data"));

			if (Directory.Exists(PrepPath(GameDirA + "/config")))
				Directory.Delete(PrepPath(GameDirA + "/config"));

			if (Directory.Exists(PrepPath(GameDirA + LibMySQLFile)))
				Directory.Delete(PrepPath(GameDirA + LibMySQLFile));

			if (Directory.Exists(PrepPath(GameDirB + "/data")))
				Directory.Delete(PrepPath(GameDirB + "/data"));

			if (Directory.Exists(PrepPath(GameDirB + "/config")))
				Directory.Delete(PrepPath(GameDirB + "/config"));

			if (Directory.Exists(PrepPath(GameDirLive)))
				Directory.Delete(PrepPath(GameDirLive));
		}

		//Initializing thread
		public void InitializeImpl()
		{
			try
			{
				if (DaemonStatus() != TGDreamDaemonStatus.Offline)
				{
					lock (CompilerLock)
					{
						lastCompilerError = "Dream daemon must not be running";
						compilerCurrentStatus = IsInitialized();
						return;
					}
				}

				if (!Exists()) //repo
				{
					lock (CompilerLock)
					{
						lastCompilerError = "Repository is not setup!";
						compilerCurrentStatus = IsInitialized();
						return;
					}
				}
				try
				{
					SendMessage("DM: Setting up symlinks...", ChatMessageType.DeveloperInfo);
					CleanGameFolder();
					Program.DeleteDirectory(PrepPath(GameDir));

					Directory.CreateDirectory(PrepPath(GameDirA));
					Directory.CreateDirectory(PrepPath(GameDirB));

					CreateSymlink(PrepPath(GameDirA + "/data"), PrepPath(StaticDataDir));
					CreateSymlink(PrepPath(GameDirB + "/data"), PrepPath(StaticDataDir));

					CreateSymlink(PrepPath(GameDirA + "/config"), PrepPath(StaticConfigDir));
					CreateSymlink(PrepPath(GameDirB + "/config"), PrepPath(StaticConfigDir));

					CreateSymlink(PrepPath(GameDirA + LibMySQLFile), PrepPath(StaticDirs + LibMySQLFile));
					CreateSymlink(PrepPath(GameDirB + LibMySQLFile), PrepPath(StaticDirs + LibMySQLFile));

					CreateSymlink(PrepPath(GameDirLive), PrepPath(GameDirA));
					
					lock (CompilerLock)
					{
						compilerCurrentStatus = TGCompilerStatus.Compiling;
						silentCompile = true;
					}
				}
				catch (ThreadAbortException)
				{
					return;
				}
				catch (Exception e)
				{
					lock (CompilerLock)
					{
						SendMessage("DM: Setup failed!", ChatMessageType.DeveloperInfo);
						lastCompilerError = e.ToString();
						compilerCurrentStatus = TGCompilerStatus.Uninitialized;
						return;
					}
				}
			}
			catch (ThreadAbortException)
			{
				return;
			}
			CompileImpl();
		}		

		//Returns the A or B dir in which the game is NOT running
		string GetStagingDir()
		{
			string TheDir;
			if (!Directory.Exists(PrepPath(GameDirLive)))
				TheDir = GameDirA;
			else
			{
				File.Create(PrepPath(LiveDirTest)).Close();
				try
				{
					if (File.Exists(PrepPath(ADirTest)))
						TheDir = GameDirA;
					else if (File.Exists(PrepPath(BDirTest)))
						TheDir = GameDirB;
					else
						throw new Exception("Unable to determine current live directory!");
				}
				finally
				{
					File.Delete(PrepPath(LiveDirTest));
				}


				TheDir = InvertDirectory(TheDir);

			}
			//So TheDir is what the Live folder is NOT pointing to
			//Now we need to check if DD is running that folder and swap it if necessary

			var rsclock = TheDir + "/" + Config.ProjectName + ".rsc.lk";
			if (File.Exists(rsclock))
			{
				try
				{
					File.Delete(PrepPath(rsclock));
				}
				catch	//held open by byond
				{
					//This means there is a staged update waiting to be applied, we have to unstage it before we can work
					Directory.Delete(GameDirLive);
					CreateSymlink(GameDirLive, TheDir);
					return InvertDirectory(TheDir);
				}
			}
			return PrepPath(TheDir);
		}

		//I hope you can read this
		string InvertDirectory(string gameDirectory)
		{
			if (gameDirectory == GameDirA)
				return GameDirB;
			else
				return GameDirA;
		}

		//Compiler thread
		void CompileImpl()
		{
			try
			{
				if (GetVersion(TGByondVersion.Installed) == null)
				{
					lastCompilerError = "BYOND not installed!";
					compilerCurrentStatus = TGCompilerStatus.Initialized;
					return;
				}
				bool silent;
				lock (CompilerLock)
				{
					silent = silentCompile;
					silentCompile = false;
				}

				if(!silent)
					SendMessage("DM: Compiling...", ChatMessageType.DeveloperInfo);

				var resurrectee = GetStagingDir();

				Program.DeleteDirectory(resurrectee, true, deleteExcludeList);

				Directory.CreateDirectory(resurrectee + "/.git/logs");

				if (!Directory.Exists(resurrectee + "/config"))
					CreateSymlink(resurrectee + "/config", StaticConfigDir);
				if (!Directory.Exists(resurrectee + "/data"))
					CreateSymlink(resurrectee + "/data", StaticDataDir);
				if (!File.Exists(resurrectee + LibMySQLFile))
					CreateSymlink(resurrectee + LibMySQLFile, StaticDirs + LibMySQLFile);

				bool repobusy_check = false;
				if (!Monitor.TryEnter(RepoLock))
					repobusy_check = true;

				if (!repobusy_check)
				{
					if (RepoBusy)
						repobusy_check = true;
					else
						RepoBusy = true;
					Monitor.Exit(RepoLock);
				}

				if (repobusy_check)
				{
					SendMessage("DM: Copy aborted, repo locked!", ChatMessageType.DeveloperInfo);
					lock (CompilerLock)
					{
						lastCompilerError = "The repo could not be locked for copying";
						compilerCurrentStatus = TGCompilerStatus.Initialized;	//still fairly valid
						return;
					}
				}
				try
				{
					Program.CopyDirectory(PrepPath(RepoPath), resurrectee, copyExcludeList);
					//just the tip
					const string GitLogsDir = "/.git/logs";
					Program.CopyDirectory(PrepPath(RepoPath + GitLogsDir), resurrectee + GitLogsDir);
				}
				finally
				{
					lock (RepoLock)
					{
						RepoBusy = false;
					}
				}
				
				var res = CreateBackup();
				if(res != null)
					lock (CompilerLock)
					{
						lastCompilerError = res;
						compilerCurrentStatus = TGCompilerStatus.Initialized;
						return;
					}

				var dmeName = ProjectName() + ".dme";
				var dmePath = resurrectee + "/" + dmeName; 
				if (!File.Exists(dmePath))
				{
					var errorMsg = String.Format("Could not find {0}!", dmeName);
<<<<<<< HEAD
					SendMessage("DM: " + errorMsg, ChatMessageType.DeveloperInfo);
					TGServerService.WriteError(errorMsg, TGServerService.EventID.DMCompileCrash);
=======
					SendMessage("DM: " + errorMsg);
					TGServerService.WriteError(errorMsg, TGServerService.EventID.DMCompileCrash, this);
>>>>>>> Instances
					lock (CompilerLock)
					{
						lastCompilerError = errorMsg;
						compilerCurrentStatus = TGCompilerStatus.Initialized;
						return;
					}
				}

				using (var DM = new Process())  //will kill the process if the thread is terminated
				{
<<<<<<< HEAD
					DM.StartInfo.FileName = ByondDirectory + "/bin/dm.exe";
					DM.StartInfo.Arguments = String.Format("-clean {0}", dmePath);
=======
					DM.StartInfo.FileName = PrepPath(ByondDirectory + "/bin/dm.exe");
					DM.StartInfo.Arguments = '"' + dmePath + '"';
>>>>>>> Instances
					DM.StartInfo.RedirectStandardOutput = true;
					DM.StartInfo.UseShellExecute = false;
					var OutputList = new StringBuilder();
					DM.OutputDataReceived += new DataReceivedEventHandler(
						delegate(object sender, DataReceivedEventArgs e)
						{
							OutputList.Append(Environment.NewLine);
							OutputList.Append(e.Data);
						}
					);
					try
					{
						lock (CompilerLock)
						{
							if (compilationCancellationRequestation)
								return;
							canCancelCompilation = true;
						}
						
						DM.Start();
						DM.BeginOutputReadLine();
						DM.WaitForExit();
						DM.CancelOutputRead();

						lock (CompilerLock)
						{
							canCancelCompilation = false;
							compilationCancellationRequestation = false;
						}
					}
					catch
					{
						if (!DM.HasExited)
							DM.Kill();
						throw;
					}
					finally
					{
						lock (CompilerLock)
						{
							canCancelCompilation = false;
						}
					}

					if (DM.ExitCode == 0)
					{
						lock (watchdogLock)
						{
							try
							{
								//gotta go fast
								var online = currentStatus == TGDreamDaemonStatus.Online;
								if(online)
									Proc.Suspend();
								try
								{
									if (Directory.Exists(GameDirLive))
										//these two lines should be atomic but this is the best we can do
										Directory.Delete(GameDirLive);
									CreateSymlink(GameDirLive, resurrectee);
								}
								finally
								{
									if(online && !Proc.HasExited)
										Proc.Resume();
								}
<<<<<<< HEAD
=======
								if (Directory.Exists(PrepPath(GameDirLive)))
									//these two lines should be atomic but this is the best we can do
									Directory.Delete(PrepPath(GameDirLive));
								CreateSymlink(PrepPath(GameDirLive), resurrectee);
>>>>>>> Instances
							}
							finally
							{
								if (currentStatus == TGDreamDaemonStatus.Online)
								{
									try
									{
										Proc.PriorityClass = ProcessPriorityClass.Normal;
									}
									catch { }
									Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
									Thread.CurrentThread.Priority = ThreadPriority.Normal;
								}
							}
						}
<<<<<<< HEAD
						var staged = DaemonStatus() != TGDreamDaemonStatus.Offline;
						var msg = String.Format("Compile complete!{0}", !staged ? "" : " Server will update next round.");
						SendMessage("DM: " + msg, ChatMessageType.DeveloperInfo);
						TGServerService.WriteInfo(msg, TGServerService.EventID.DMCompileSuccess);
=======
						var msg = String.Format("DM: Compile complete!{0}", DaemonStatus() == TGDreamDaemonStatus.Offline ? "" : " Server will update next round.");
						SendMessage(msg);
						TGServerService.WriteInfo(msg, TGServerService.EventID.DMCompileSuccess, this);
>>>>>>> Instances
						lock (CompilerLock)
						{
							if (staged)
								UpdateStaged = true;
							lastCompilerError = null;
							compilerCurrentStatus = TGCompilerStatus.Initialized;   //still fairly valid
						}
					}
					else
					{
<<<<<<< HEAD
						SendMessage("DM: Compile failed!", ChatMessageType.DeveloperInfo); //Also happens for warnings
						TGServerService.WriteWarning("Compile error: " + OutputList.ToString(), TGServerService.EventID.DMCompileError);
=======
						SendMessage("DM: Compile failed!"); //Also happens for warnings
						TGServerService.WriteWarning(String.Format("Compile error ({1} {0}: {2}", DM.StartInfo.Arguments, DM.StartInfo.FileName, OutputList.ToString()), TGServerService.EventID.DMCompileError, this);
>>>>>>> Instances
						lock (CompilerLock)
						{
							lastCompilerError = "DM compile failure";
							compilerCurrentStatus = TGCompilerStatus.Initialized;
						}
					}
				}

			}
			catch (ThreadAbortException)
			{
				return;
			}
			catch (Exception e)
			{
<<<<<<< HEAD
				SendMessage("DM: Compiler thread crashed!", ChatMessageType.DeveloperInfo);
				TGServerService.WriteError("Compile manager errror: " + e.ToString(), TGServerService.EventID.DMCompileCrash);
=======
				SendMessage("DM: Compiler thread crashed!");
				TGServerService.WriteError("Compile manager errror: " + e.ToString(), TGServerService.EventID.DMCompileCrash, this);
>>>>>>> Instances
				lock (CompilerLock)
				{
					lastCompilerError = e.ToString();
					compilerCurrentStatus = TGCompilerStatus.Initialized;   //still fairly valid
				}
			}
			finally
			{
				lock (CompilerLock)
				{
					canCancelCompilation = false;
					if (compilationCancellationRequestation)
					{
						compilerCurrentStatus = TGCompilerStatus.Initialized;
						compilationCancellationRequestation = false;
<<<<<<< HEAD
						SendMessage("DM: Compile cancelled!", ChatMessageType.DeveloperInfo);
						TGServerService.WriteInfo("Compilation cancelled", TGServerService.EventID.DMCompileCancel);
=======
						SendMessage("Compile cancelled!");
						TGServerService.WriteInfo("Compilation cancelled", TGServerService.EventID.DMCompileCancel, this);
>>>>>>> Instances
					}
				}
			}
		}
		//kicks off the compiler thread
		//public api
		public bool Compile(bool silent = false)
		{
			lock (CompilerLock)
			{
				if (compilerCurrentStatus != TGCompilerStatus.Initialized)
					return false;
				silentCompile = silent;
				lastCompilerError = null;
				compilerCurrentStatus = TGCompilerStatus.Compiling;
				CompilerThread = new Thread(new ThreadStart(CompileImpl));
				CompilerThread.Start();
			}
			return true;
		}

		//public api
		public string ProjectName()
		{
			lock (CompilerLock)
			{
				return Config.ProjectName;
			}
		}

		//public api
		public void SetProjectName(string projectName)
		{
			lock (CompilerLock)
			{
				Config.ProjectName = projectName;
			}
		}

		public string Cancel()
		{
			lock (CompilerLock)
			{
				if (compilerCurrentStatus != TGCompilerStatus.Compiling)
					return "Invalid state for cancellation!";
				compilationCancellationRequestation = true;
				if (canCancelCompilation)
					CompilerThread.Abort();
				else
					return "Compilation will be cancelled when the repo copy is complete";
				return null;
			}
		}
	}
}
