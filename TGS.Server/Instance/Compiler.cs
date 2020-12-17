using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Server
{
	sealed partial class Instance : ITGCompiler
	{
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
				InitCompiler(); //also cleanup
			}
		}

		//translates the win32 api call into an exception if it fails
		static void CreateSymlink(string link, string target)
		{
			if (!NativeMethods.CreateSymbolicLink(new DirectoryInfo(link).FullName, new DirectoryInfo(target).FullName, File.Exists(target) ? NativeMethods.SymbolicLink.File : NativeMethods.SymbolicLink.Directory))
				throw new Win32Exception(Marshal.GetLastWin32Error());
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
			if (File.Exists(RelativePath(Path.Combine(GameDirLive, BridgeDLLName))) || File.Exists(RelativePath(Path.Combine(GameDirLive, Assembly.GetAssembly(typeof(IClient)).GetName().Name + ".dll"))))	//its a good tell, jim
				return CompilerStatus.Initialized;
			return CompilerStatus.Uninitialized;
		}
		
		//we need to remove symlinks before we can recursively delete
		void CleanGameFolder()
		{
			var GameDirABridge = RelativePath(Path.Combine(GameDirA, BridgeDLLName));
			if (Directory.Exists(GameDirABridge))
				Directory.Delete(GameDirABridge);

			var GameDirBBridge = RelativePath(Path.Combine(GameDirB, BridgeDLLName));
			if (Directory.Exists(GameDirBBridge))
				Directory.Delete(GameDirBBridge);

			var rgdl = RelativePath(GameDirLive);
			if (Directory.Exists(rgdl))
				Directory.Delete(rgdl);
		}

		/// <summary>
		/// Copies .dm files from <see cref="StaticDirs"/> to <paramref name="stagingDir"/> and returns #include lines for them
		/// </summary>
		/// <param name="stagingDir">The directory to copy .dm files to</param>
		/// <returns>DM #include lines for .dm files in <see cref="StaticDirs"/></returns>
		IEnumerable<string> GetAndCopyIncludeLines(string stagingDir)
		{
			if (stagingDir == null)
				throw new ArgumentNullException(nameof(stagingDir));
			var baseURI = new Uri(stagingDir);
			foreach (var I in Helpers.GetFilesWithExtensionInDirectory(RelativePath(StaticDirs), "dm"))
			{
				var fileURI = new Uri(I);
				var fileName = Path.GetFileName(I);
				File.Copy(I, Path.Combine(stagingDir, fileName));
				yield return String.Format(CultureInfo.InvariantCulture, "#include \"{0}\"", fileName);
			}
		}

		/// <summary>
		/// Adds includes for .dm files in <see cref="StaticDirs"/>
		/// </summary>
		/// <param name="stagingDir">The directory to operate on</param>
		/// <param name="dmePath">The full path the the .dme in <paramref name="stagingDir"/></param>
		void HandleDMEModifications(string stagingDir, string dmePath)
		{
			if (stagingDir == null)
				throw new ArgumentNullException(nameof(stagingDir));
			if (dmePath == null)
				throw new ArgumentNullException(nameof(dmePath));
			if (!File.Exists(dmePath))
				return;	//someone else will deal with this
			var newInclusions = new List<string>();
			var newLines = new List<string>();

			var lines = File.ReadAllLines(dmePath).ToList();
			var initalCount = lines.Count;
			var enumerator = GetAndCopyIncludeLines(stagingDir);
			for (var I = 0; I < lines.Count; ++I)
			{
				var line = lines[I];
				if (line.Contains("BEGIN_INCLUDE"))
				{
					lines.InsertRange(I + 1, enumerator);
					break;
				}
			}
			if (lines.Count == initalCount)
				lines.InsertRange(0, enumerator);

			if (lines.Count > initalCount)
				File.WriteAllLines(dmePath, lines);
		}

		//Initializing thread
		public void InitializeImpl()
		{
			Thread.CurrentThread.Name = "Initialization Thread";
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
					Helpers.DeleteDirectory(RelativePath(GameDir));

					Directory.CreateDirectory(RelativePath(GameDirA));
					Directory.CreateDirectory(RelativePath(GameDirB));

					var rep_config = GetCachedRepoConfig();

					if (rep_config != null) {
						foreach (var I in rep_config.StaticDirectoryPaths)
							CreateSymlink(RelativePath(Path.Combine(GameDirA, I)), RelativePath(Path.Combine(StaticDirs, I)));
						foreach (var I in rep_config.DLLPaths)
							CreateSymlink(RelativePath(Path.Combine(GameDirA, I)), RelativePath(Path.Combine(StaticDirs, I)));
					}

					var rbdlln = RelativePath(BridgeDLLName);
					CreateSymlink(RelativePath(Path.Combine(GameDirA, BridgeDLLName)), rbdlln);
					CreateSymlink(RelativePath(Path.Combine(GameDirB, BridgeDLLName)), rbdlln);

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
				catch   //held open by byond
				{
					//This means there is a staged update waiting to be applied, we have to unstage it before we can work
					Directory.Delete(RelativePath(GameDirLive));
					CreateSymlink(RelativePath(GameDirLive), RelativePath(TheDir));
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
			if(Thread.CurrentThread.Name == null)
				Thread.CurrentThread.Name = "Compiler Thread";
			try
			{
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

				lock (RepoLock)
				{
					if (RepoBusy)
						repobusy_check = true;
					else
						RepoBusy = true;
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
				string dmeName, dmePath;
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

					resurrectee = GetStagingDir();  //non-relative

					var Config = GetCachedRepoConfig();
					var deleteExcludeList = new List<string> { BridgeDLLName };
					deleteExcludeList.AddRange(Config.StaticDirectoryPaths);
					deleteExcludeList.AddRange(Config.DLLPaths);
					Helpers.DeleteDirectory(resurrectee, true, deleteExcludeList);


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

					if (!File.Exists(Path.Combine(resurrectee, BridgeDLLName)))
						CreateSymlink(Path.Combine(resurrectee, BridgeDLLName), RelativePath(BridgeDLLName));

					deleteExcludeList.Add(".git");
					Helpers.CopyDirectory(RelativePath(RepoPath), resurrectee, deleteExcludeList);
					CurrentSha = GetShaOrBranchNoLock(out string error, false, false);
					//just the tip
					const string GitLogsDir = "/.git/logs";
					Helpers.CopyDirectory(RelativePath(RepoPath + GitLogsDir), resurrectee + GitLogsDir);
					dmeName = String.Format(CultureInfo.InvariantCulture, "{0}.dme", ProjectName());
					dmePath = Path.Combine(resurrectee, dmeName);
					HandleDMEModifications(resurrectee, dmePath);
					try
					{
						File.Copy(RelativePath(PRJobFile), Path.Combine(resurrectee, PRJobFile));
					}
					catch { }
				}
				finally
				{
					lock (RepoLock)
						RepoBusy = false;
				}

				var res = CreateBackup();
				if (res != null)
					lock (CompilerLock)
					{
						lastCompilerError = res;
						compilerCurrentStatus = CompilerStatus.Initialized;
						return;
					}

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

				if (!PrecompileHook($"\"{resurrectee.Replace('\\', '/')}\""))
				{
					lastCompilerError = "The precompile hook failed";
					compilerCurrentStatus = CompilerStatus.Initialized;   //still fairly valid
					WriteWarning("Precompile hook failed!", EventID.DMCompileError);
					return;
				}

				bool stagedBuild;
				lock (ByondLock)
				{
					stagedBuild = updateStat == ByondStatus.Staged;
					if (stagedBuild)
						updateStat = ByondStatus.CompilingStaged;
					else if (GetVersion(ByondVersion.Installed) == null)
						lock (CompilerLock)
						{
							lastCompilerError = "BYOND not installed!";
							compilerCurrentStatus = CompilerStatus.Initialized;
							return;
						}
				}

				using (var DM = new Process())  //will kill the process if the thread is terminated
				{
					DM.StartInfo.FileName = RelativePath(Path.Combine(stagedBuild ? StagingDirectoryInner : ByondDirectory, "bin/dm.exe"));
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
						if (stagedBuild)
						{
							lock (ByondLock)
								updateStat = ByondStatus.Staged;
							RequestRestart();
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
						var msg = String.Format("Compile complete!{0}", !staged ? "" : " Server will update on reboot.");
						WorldAnnounce("Server updated, changes will be applied on reboot...");
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
				lock (ByondLock)
					if (updateStat == ByondStatus.Staged)
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
				Config.Save();
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
