using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGServerService
{
	sealed partial class ServerInstance : ITGCompiler
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
		const string StaticBackupDir = "Static_BACKUP";

		const string GameDir = "Game";
		const string GameDirA = GameDir + "/A";
		const string GameDirB = GameDir + "/B";
		const string GameDirLive = GameDir + "/Live";

		const string LiveFile = "/TestLive.lk";
		const string ADirTest = GameDirA + LiveFile;
		const string BDirTest = GameDirB + LiveFile;
		const string LiveDirTest = GameDirLive + LiveFile;

		const string InterfaceDLLName = "TGServiceInterface.dll";

		object CompilerLock = new object();
		CompilerStatus compilerCurrentStatus;
		string lastCompilerError;
		
		Thread CompilerThread;
		bool compilationCancellationRequestation = false;
		bool canCancelCompilation = false;
		bool silentCompile = false;

		bool UpdateStaged = false;

		//deletes leftovers and checks current status
		void InitCompiler()
		{
			var rldt = RelativePath(LiveDirTest);
			if (File.Exists(rldt))
				File.Delete(rldt);
			compilerCurrentStatus = IsInitialized();
		}

		/// <inheritdoc />
		public CompilerStatus GetStatus()
		{
			lock (CompilerLock)
			{
				return compilerCurrentStatus;
			}
		}

		/// <inheritdoc />
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
		static void CreateSymlink(string link, string target)
		{
			if (!CreateSymbolicLink(new DirectoryInfo(link).FullName, new DirectoryInfo(target).FullName, File.Exists(target) ? SymbolicLink.File : SymbolicLink.Directory))
				throw new Exception(String.Format("Failed to create symlink from {0} to {1}! Error: {2}", target, link, Marshal.GetLastWin32Error()));
		}

		//requires CompilerLock to be locked
		bool CompilerIdleNoLock()
		{
			return compilerCurrentStatus == CompilerStatus.Uninitialized || compilerCurrentStatus == CompilerStatus.Initialized;
		}
		
		/// <inheritdoc />
		public bool Initialize()
		{
			lock (CompilerLock)
			{
				if (!CompilerIdleNoLock())
					return false;
				lastCompilerError = null;
				compilerCurrentStatus = CompilerStatus.Initializing;
				CompilerThread = new Thread(new ThreadStart(InitializeImpl));
				CompilerThread.Start();
				return true;
			}
		}

		//what is says on the tin
		CompilerStatus IsInitialized()
		{
			if (File.Exists(RelativePath(Path.Combine(GameDirLive, InterfaceDLLName))))	//its a good tell, jim
				return CompilerStatus.Initialized;
			return CompilerStatus.Uninitialized;
		}
		
		//we need to remove symlinks before we can recursively delete
		void CleanGameFolder()
		{
			var GameDirAInterface = RelativePath(Path.Combine(GameDirA, InterfaceDLLName));
			if (Directory.Exists(GameDirAInterface))
				Directory.Delete(GameDirAInterface);

			var GameDirBInterface = RelativePath(Path.Combine(GameDirB, InterfaceDLLName));
			if (Directory.Exists(GameDirBInterface))
				Directory.Delete(GameDirBInterface);

			var rgdl = RelativePath(GameDirLive);
			if (Directory.Exists(rgdl))
				Directory.Delete(rgdl);
		}

		//Initializing thread
		public void InitializeImpl()
		{
			try
			{
				if (DaemonStatus() != DreamDaemonStatus.Offline)
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

				if (!RepoConfigsMatch())
				{
					lock (CompilerLock)
					{
						lastCompilerError = "Repository TGS3.json does not match cached version! Please update the config appropriately!";
						compilerCurrentStatus = IsInitialized();
						return;
					}
				}
				try
				{
					SendMessage("DM: Setting up symlinks...", MessageType.DeveloperInfo);
					CleanGameFolder();
					Program.DeleteDirectory(RelativePath(GameDir));

					Directory.CreateDirectory(RelativePath(GameDirA));
					Directory.CreateDirectory(RelativePath(GameDirB));

					var rep_config = GetCachedRepoConfig();

					if (rep_config != null) {
						foreach (var I in rep_config.StaticDirectoryPaths)
							CreateSymlink(RelativePath(Path.Combine(GameDirA, I)), RelativePath(Path.Combine(StaticDirs, I)));
						foreach (var I in rep_config.DLLPaths)
							CreateSymlink(RelativePath(Path.Combine(GameDirA, I)), RelativePath(Path.Combine(StaticDirs, I)));
					}

					var ridlln = RelativePath(InterfaceDLLName);
					CreateSymlink(RelativePath(Path.Combine(GameDirA, InterfaceDLLName)), ridlln);
					CreateSymlink(RelativePath(Path.Combine(GameDirB, InterfaceDLLName)), ridlln);

					CreateSymlink(RelativePath(GameDirLive), RelativePath(GameDirA));
					
					lock (CompilerLock)
					{
						compilerCurrentStatus = CompilerStatus.Compiling;
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
						SendMessage("DM: Setup failed!", MessageType.DeveloperInfo);
						WriteError("Compiler Initialization Error: " + e.ToString(), EventID.DMInitializeCrash);
						lastCompilerError = e.ToString();
						compilerCurrentStatus = CompilerStatus.Uninitialized;
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
			if (!Directory.Exists(RelativePath(GameDirLive)))
				TheDir = GameDirA;
			else
			{
				File.Create(RelativePath(LiveDirTest)).Close();
				try
				{
					if (File.Exists(RelativePath(ADirTest)))
						TheDir = GameDirA;
					else if (File.Exists(RelativePath(BDirTest)))
						TheDir = GameDirB;
					else
						throw new Exception("Unable to determine current live directory!");
				}
				finally
				{
					File.Delete(RelativePath(LiveDirTest));
				}


				TheDir = InvertDirectory(TheDir);
			}

			//So TheDir is what the Live folder is NOT pointing to
			//Now we need to check if DD is running that folder and swap it if necessary

			var rsclock = RelativePath(TheDir + "/" + Config.ProjectName + ".rsc.lk");
			if (File.Exists(rsclock))
			{
				try
				{
					File.Delete(rsclock);
				}
				catch	//held open by byond
				{
					//This means there is a staged update waiting to be applied, we have to unstage it before we can work
					Directory.Delete(RelativePath(GameDirLive));
					CreateSymlink(RelativePath(GameDirLive), TheDir);
					return RelativePath(InvertDirectory(TheDir));
				}
			}
			return RelativePath(TheDir);
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
				if (GetVersion(ByondVersion.Installed) == null)
				{
					lock (CompilerLock)
					{
						lastCompilerError = "BYOND not installed!";
						compilerCurrentStatus = CompilerStatus.Initialized;
						return;
					}
				}
				if (!RepoConfigsMatch())
				{
					lock (CompilerLock)
					{
						lastCompilerError = "Repository TGS3.json does not match cached version! Please update the config appropriately!";
						compilerCurrentStatus = IsInitialized();
						return;
					}
				}
				string resurrectee;
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
					SendMessage("DM: Copy aborted, repo locked!", MessageType.DeveloperInfo);
					lock (CompilerLock)
					{
						lastCompilerError = "The repo could not be locked for copying";
						compilerCurrentStatus = CompilerStatus.Initialized;   //still fairly valid
						return;
					}
				}
				string CurrentSha;
				try
				{
					bool silent;
					lock (CompilerLock)
					{
						silent = silentCompile;
						silentCompile = false;
					}

					if (!silent)
						SendMessage("DM: Compiling...", MessageType.DeveloperInfo);

					resurrectee = GetStagingDir();	//non-relative

					var Config = GetCachedRepoConfig();
					var deleteExcludeList = new List<string> { InterfaceDLLName };
					deleteExcludeList.AddRange(Config.StaticDirectoryPaths);
					deleteExcludeList.AddRange(Config.DLLPaths);
					Program.DeleteDirectory(resurrectee, true, deleteExcludeList);


					Directory.CreateDirectory(resurrectee + "/.git/logs");

					foreach (var I in Config.StaticDirectoryPaths)
					{
						var the_path = Path.Combine(resurrectee, I);
						if (!Directory.Exists(the_path))
							CreateSymlink(Path.Combine(resurrectee, I), RelativePath(Path.Combine(StaticDirs, I)));
					}
					foreach (var I in Config.DLLPaths)
					{
						var the_path = Path.Combine(resurrectee, I);
						if (!File.Exists(the_path))
							CreateSymlink(the_path, RelativePath(Path.Combine(StaticDirs, I)));
					}

					if (!File.Exists(Path.Combine(resurrectee, InterfaceDLLName)))
						CreateSymlink(Path.Combine(resurrectee, InterfaceDLLName), RelativePath(InterfaceDLLName));

					deleteExcludeList.Add(".git");
					Program.CopyDirectory(RelativePath(RepoPath), resurrectee, deleteExcludeList);
					CurrentSha = GetHead(false, out string error);
					//just the tip
					const string GitLogsDir = "/.git/logs";
					Program.CopyDirectory(RelativePath(RepoPath + GitLogsDir), resurrectee + GitLogsDir);
					try
					{
						File.Copy(RelativePath(PRJobFile), Path.Combine(resurrectee, PRJobFile));
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
				if (res != null)
					lock (CompilerLock)
					{
						lastCompilerError = res;
						compilerCurrentStatus = CompilerStatus.Initialized;
						return;
					}

				var dmeName = ProjectName() + ".dme";
				var dmePath = resurrectee + "/" + dmeName;
				if (!File.Exists(dmePath))
				{
					var errorMsg = String.Format("Could not find {0}!", dmeName);
					SendMessage("DM: " + errorMsg, MessageType.DeveloperInfo);
					WriteError(errorMsg, EventID.DMCompileCrash);
					lock (CompilerLock)
					{
						lastCompilerError = errorMsg;
						compilerCurrentStatus = CompilerStatus.Initialized;
						return;
					}
				}

				if (!PrecompileHook())
				{
					lastCompilerError = "The precompile hook failed";
					compilerCurrentStatus = CompilerStatus.Initialized;   //still fairly valid
					WriteWarning("Precompile hook failed!", EventID.DMCompileError);
					return;
				}

				using (var DM = new Process())  //will kill the process if the thread is terminated
				{
					DM.StartInfo.FileName = RelativePath(ByondDirectory + "/bin/dm.exe");
					DM.StartInfo.Arguments = String.Format("-clean {0}", dmePath);
					DM.StartInfo.RedirectStandardOutput = true;
					DM.StartInfo.UseShellExecute = false;
					var OutputList = new StringBuilder();
					DM.OutputDataReceived += new DataReceivedEventHandler(
						delegate (object sender, DataReceivedEventArgs e)
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
							//gotta go fast
							var online = currentStatus == DreamDaemonStatus.Online;
							if (online)
								Proc.Suspend();
							try
							{
								var rgdl = RelativePath(GameDirLive);
								if (Directory.Exists(rgdl))
									//these next two lines should be atomic but this is the best we can do
									Directory.Delete(rgdl);
								CreateSymlink(rgdl, resurrectee);
							}
							finally
							{
								if (online && !Proc.HasExited)
									Proc.Resume();
							}
						}
						var staged = DaemonStatus() != DreamDaemonStatus.Offline;
						if (!PostcompileHook())
						{
							lastCompilerError = "The postcompile hook failed";
							compilerCurrentStatus = CompilerStatus.Initialized;   //still fairly valid
							WriteWarning("Postcompile hook failed!", EventID.DMCompileError);
							return;
						}
						UpdateLiveSha(CurrentSha);
						var msg = String.Format("Compile complete!{0}", !staged ? "" : " Server will update next round.");
						SendMessage("DM: " + msg, MessageType.DeveloperInfo);
						WriteInfo(msg, EventID.DMCompileSuccess);
						lock (CompilerLock)
						{
							if (staged)
								UpdateStaged = true;
							lastCompilerError = null;
							compilerCurrentStatus = CompilerStatus.Initialized;   //still fairly valid
						}
					}
					else
					{
						SendMessage("DM: Compile failed!", MessageType.DeveloperInfo); //Also happens for warnings
						WriteWarning("Compile error: " + OutputList.ToString(), EventID.DMCompileError);
						lock (CompilerLock)
						{
							lastCompilerError = "DM compile failure";
							compilerCurrentStatus = CompilerStatus.Initialized;
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
				SendMessage("DM: Compiler thread crashed!", MessageType.DeveloperInfo);
				WriteError("Compile manager errror: " + e.ToString(), EventID.DMCompileCrash);
				lock (CompilerLock)
				{
					lastCompilerError = e.ToString();
					compilerCurrentStatus = CompilerStatus.Initialized;   //still fairly valid
				}
			}
			finally
			{
				lock (CompilerLock)
				{
					canCancelCompilation = false;
					if (compilationCancellationRequestation)
					{
						compilerCurrentStatus = CompilerStatus.Initialized;
						compilationCancellationRequestation = false;
						SendMessage("DM: Compile cancelled!", MessageType.DeveloperInfo);
						WriteInfo("Compilation cancelled", EventID.DMCompileCancel);
					}
				}
			}
		}
		//kicks off the compiler thread
		/// <inheritdoc />
		public bool Compile(bool silent = false)
		{
			lock (CompilerLock)
			{
				if (compilerCurrentStatus != CompilerStatus.Initialized)
					return false;
				silentCompile = silent;
				lastCompilerError = null;
				compilerCurrentStatus = CompilerStatus.Compiling;
				CompilerThread = new Thread(new ThreadStart(CompileImpl));
				CompilerThread.Start();
			}
			return true;
		}

		/// <inheritdoc />
		public string ProjectName()
		{
			lock (CompilerLock)
			{
				return Config.ProjectName;
			}
		}

		/// <inheritdoc />
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
				if (compilerCurrentStatus != CompilerStatus.Compiling)
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
