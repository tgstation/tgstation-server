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

		const string InterfaceDLLName = "TGServiceInterface.dll";

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
			if(File.Exists(LiveDirTest))
				File.Delete(LiveDirTest);
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

		//translates the win32 api call into an exception if it fails
		void CreateSymlink(string link, string target)
		{
			if (!CreateSymbolicLink(new DirectoryInfo(link).FullName, new DirectoryInfo(target).FullName, File.Exists(target) ? SymbolicLink.File : SymbolicLink.Directory))
				throw new Exception(String.Format("Failed to create symlink from {0} to {1}! Error: {2}", target, link, Marshal.GetLastWin32Error()));
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
			if (File.Exists(GameDirLive + LibMySQLFile))	//its a good tell, jim
				return TGCompilerStatus.Initialized;
			return TGCompilerStatus.Uninitialized;
		}

		//we need to remove symlinks before we can recursively delete
		void CleanGameFolder()
		{
			if (Directory.Exists(GameDirA + InterfaceDLLName))
				Directory.Delete(GameDirA + InterfaceDLLName);

			if (Directory.Exists(GameDirA + LibMySQLFile))
				Directory.Delete(GameDirA + LibMySQLFile);

			if (Directory.Exists(GameDirA + "/data"))
				Directory.Delete(GameDirA + "/data");

			if (Directory.Exists(GameDirA + "/config"))
				Directory.Delete(GameDirA + "/config");

			if (Directory.Exists(GameDirB + InterfaceDLLName))
				Directory.Delete(GameDirB + InterfaceDLLName);

			if (Directory.Exists(GameDirB + LibMySQLFile))
				Directory.Delete(GameDirB + LibMySQLFile);

			if (Directory.Exists(GameDirB + "/data"))
				Directory.Delete(GameDirB + "/data");

			if (Directory.Exists(GameDirB + "/config"))
				Directory.Delete(GameDirB + "/config");

			if (Directory.Exists(GameDirLive))
				Directory.Delete(GameDirLive);
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
					Program.DeleteDirectory(GameDir);

					Directory.CreateDirectory(GameDirA);
					Directory.CreateDirectory(GameDirB);

					CreateSymlink(GameDirA + "/data", StaticDataDir);
					CreateSymlink(GameDirB + "/data", StaticDataDir);

					CreateSymlink(GameDirA + "/config", StaticConfigDir);
					CreateSymlink(GameDirB + "/config", StaticConfigDir);

					CreateSymlink(GameDirA + LibMySQLFile, StaticDirs + LibMySQLFile);
					CreateSymlink(GameDirB + LibMySQLFile, StaticDirs + LibMySQLFile);

					CreateSymlink(GameDirA + InterfaceDLLName, StaticDirs + InterfaceDLLName);
					CreateSymlink(GameDirB + InterfaceDLLName, StaticDirs + InterfaceDLLName);

					CreateSymlink(GameDirLive, GameDirA);
					
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
			if (!Directory.Exists(GameDirLive))
				TheDir = GameDirA;
			else
			{
				File.Create(LiveDirTest).Close();
				try
				{
					if (File.Exists(ADirTest))
						TheDir = GameDirA;
					else if (File.Exists(BDirTest))
						TheDir = GameDirB;
					else
						throw new Exception("Unable to determine current live directory!");
				}
				finally
				{
					File.Delete(LiveDirTest);
				}


				TheDir = InvertDirectory(TheDir);

			}
			//So TheDir is what the Live folder is NOT pointing to
			//Now we need to check if DD is running that folder and swap it if necessary

			var rsclock = TheDir + "/" + Properties.Settings.Default.ProjectName + ".rsc.lk";
			if (File.Exists(rsclock))
			{
				try
				{
					File.Delete(rsclock);
				}
				catch	//held open by byond
				{
					//This means there is a staged update waiting to be applied, we have to unstage it before we can work
					Directory.Delete(GameDirLive);
					CreateSymlink(GameDirLive, TheDir);
					return InvertDirectory(TheDir);
				}
			}
			return TheDir;
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
				if (!File.Exists(resurrectee + InterfaceDLLName))
					CreateSymlink(resurrectee + InterfaceDLLName, StaticDirs + InterfaceDLLName);

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
					Program.CopyDirectory(RepoPath, resurrectee, copyExcludeList);
					//just the tip
					const string GitLogsDir = "/.git/logs";
					Program.CopyDirectory(RepoPath + GitLogsDir, resurrectee + GitLogsDir);
					try
					{
						File.Copy(PRJobFile, resurrectee + Path.DirectorySeparatorChar + PRJobFile);
					}
					catch { }
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
					SendMessage("DM: " + errorMsg, ChatMessageType.DeveloperInfo);
					TGServerService.WriteError(errorMsg, TGServerService.EventID.DMCompileCrash);
					lock (CompilerLock)
					{
						lastCompilerError = errorMsg;
						compilerCurrentStatus = TGCompilerStatus.Initialized;
						return;
					}
				}

				if (!PrecompileHook())
				{
					lastCompilerError = "The precompile hook failed";
					compilerCurrentStatus = TGCompilerStatus.Initialized;   //still fairly valid
					TGServerService.WriteWarning("Precompile hook failed!", TGServerService.EventID.DMCompileError);
					return;
				}

				using (var DM = new Process())  //will kill the process if the thread is terminated
				{
					DM.StartInfo.FileName = ByondDirectory + "/bin/dm.exe";
					DM.StartInfo.Arguments = String.Format("-clean {0}", dmePath);
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
						while (!DM.HasExited)
							DM.WaitForExit(100);
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
						{
							DM.Kill();
							DM.WaitForExit();
						}
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
						var staged = DaemonStatus() != TGDreamDaemonStatus.Offline;
						if (!PostcompileHook())
						{
							lastCompilerError = "The postcompile hook failed";
							compilerCurrentStatus = TGCompilerStatus.Initialized;   //still fairly valid
							TGServerService.WriteWarning("Postcompile hook failed!", TGServerService.EventID.DMCompileError);
							return;
						}
						var msg = String.Format("Compile complete!{0}", !staged ? "" : " Server will update next round.");
						SendMessage("DM: " + msg, ChatMessageType.DeveloperInfo);
						TGServerService.WriteInfo(msg, TGServerService.EventID.DMCompileSuccess);
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
						SendMessage("DM: Compile failed!", ChatMessageType.DeveloperInfo); //Also happens for warnings
						TGServerService.WriteWarning("Compile error: " + OutputList.ToString(), TGServerService.EventID.DMCompileError);
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
				SendMessage("DM: Compiler thread crashed!", ChatMessageType.DeveloperInfo);
				TGServerService.WriteError("Compile manager errror: " + e.ToString(), TGServerService.EventID.DMCompileCrash);
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
						SendMessage("DM: Compile cancelled!", ChatMessageType.DeveloperInfo);
						TGServerService.WriteInfo("Compilation cancelled", TGServerService.EventID.DMCompileCancel);
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
				return Properties.Settings.Default.ProjectName;
			}
		}

		//public api
		public void SetProjectName(string projectName)
		{
			lock (CompilerLock)
			{
				Properties.Settings.Default.ProjectName = projectName;
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
