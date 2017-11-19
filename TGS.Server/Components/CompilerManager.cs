using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	sealed class CompilerManager : ICompilerManager, IDisposable
	{
		const string StaticBackupDir = "Static_BACKUP";

		/// <summary>
		/// Path containing production game code
		/// </summary>
		const string GameDir = "Game";
		/// <summary>
		/// Filename used for testing the current live directory
		/// </summary>
		const string LiveFile = "TestLive.lk";

		/// <summary>
		/// Staging directory A
		/// </summary>
		static readonly string GameDirA = Path.Combine(GameDir, "A");
		/// <summary>
		/// Staging directory B
		/// </summary>
		static readonly string GameDirB = Path.Combine(GameDir, "B");
		/// <summary>
		/// Live directory
		/// </summary>
		static readonly string GameDirLive = Path.Combine(GameDir + "Live");
		/// <summary>
		/// Test file path for checking if if <see cref="GameDirA"/> is <see cref="GameDirLive"/>
		/// </summary>
		static readonly string ADirTest = Path.Combine(GameDirA, LiveFile);
		/// <summary>
		/// Test file path for checking if if <see cref="GameDirB"/> is <see cref="GameDirLive"/>
		/// </summary>
		static readonly string BDirTest = Path.Combine(GameDirB, LiveFile);
		/// <summary>
		/// Test file path for determining which of <see cref="GameDirA"/> and <see cref="GameDirB"/> is <see cref="GameDirLive"/>
		/// </summary>
		static readonly string LiveDirTest = Path.Combine(GameDirLive, LiveFile);

		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IRepoConfigProvider"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IRepoConfigProvider RepoConfigProvider;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IIOManager IO;
		/// <summary>
		/// The <see cref="IInstanceConfig"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IInstanceConfig Config;
		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IChatManager Chat;
		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IRepositoryManager Repo;
		/// <summary>
		/// The <see cref="IInteropManager"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IInteropManager Interop;
		/// <summary>
		/// The <see cref="IDreamDaemonManager"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IDreamDaemonManager DreamDaemon;
		/// <summary>
		/// The <see cref="IStaticManager"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IStaticManager Static;
		/// <summary>
		/// The <see cref="IByondManager"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IByondManager Byond;

		/// <summary>
		/// The current status of the <see cref="CompilerManager"/>
		/// </summary>
		CompilerStatus compilerCurrentStatus;

		/// <summary>
		/// The current async operation the <see cref="CompilerManager"/> is running
		/// </summary>
		Task currentTask;
		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="currentTask"/>
		/// </summary>
		CancellationTokenSource currentTaskCanceller;

		/// <summary>
		/// The error put out by the last <see cref="currentTask"/>
		/// </summary>
		string lastCompilerError;
		/// <summary>
		/// <see langword="true"/> if there is a code update waiting to be applied, <see langword="false"/> otherwise
		/// </summary>
		bool updateStaged;
		/// <summary>
		/// If <see langword="true"/>, <see cref="CompileImpl"/> should not use <see cref="Chat"/>
		/// </summary>
		bool silentCompile;

		/// <summary>
		/// Construct a <see cref="CompilerManager"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="repoConfigProvider">The value of <see cref="RepoConfigProvider"/></param>
		/// <param name="io">The value of <see cref="IO"/></param>
		/// <param name="config">The value of <see cref="Config"/></param>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		/// <param name="repo">The value of <see cref="Repo"/></param>
		/// <param name="interop">The value of <see cref="Interop"/></param>
		/// <param name="dreamDaemon">The value of <see cref="DreamDaemon"/></param>
		/// <param name="_static">The value of <see cref="Static"/></param>
		/// <param name="byond">The value of <see cref="Byond"/></param>
		public CompilerManager(IInstanceLogger logger, IRepoConfigProvider repoConfigProvider, IIOManager io, IInstanceConfig config, IChatManager chat, IRepositoryManager repo, IInteropManager interop, IDreamDaemonManager dreamDaemon, IStaticManager _static, IByondManager byond)
		{
			Logger = logger;
			RepoConfigProvider = repoConfigProvider;
			IO = io;
			Config = config;
			Chat = chat;
			Repo = repo;
			Interop = interop;
			DreamDaemon = dreamDaemon;
			Static = _static;
			Byond = byond;

			Interop.OnWorldReboot += (a, b) =>
			{
				lock (this)
					if (updateStaged)
					{
						updateStaged = false;
						Logger.WriteInfo("Staged update applied", EventID.ServerUpdateApplied);
					}
			};

			CleanLiveDirTest();
			compilerCurrentStatus = IsInitialized();
		}

		/// <summary>
		/// Deletes the <see cref="LiveDirTest"/> file
		/// </summary>
		void CleanLiveDirTest()
		{
			IO.DeleteFile(LiveDirTest).Wait();
		}

		/// <inheritdoc />
		public CompilerStatus GetStatus()
		{
			return compilerCurrentStatus;
		}

		/// <inheritdoc />
		public string CompileError()
		{
			string err;
			lock (this)
			{
				err = lastCompilerError;
				lastCompilerError = null;
			}
			return err;
		}

		//kills the compiler if its running
		public void Dispose()
		{
			lock (CompilerLock)
			{
				if (CompilerThread == null || !CompilerThread.IsAlive)
					return;
				CompilerThread.Abort(); //this will safely kill dm
			}
			CleanLiveDirTest();
		}

		/// <summary>
		/// Check if the <see cref="CompilerManager"/> is idle
		/// </summary>
		/// <returns><see langword="true"/> if the <see cref="CompilerManager"/> is idle, <see langword="false"/> otherwise</returns>
		bool Idle()
		{
			switch (compilerCurrentStatus)
			{
				case CompilerStatus.Uninitialized:
				case CompilerStatus.Initialized:
					return true;
				default:
					return false;
			}
		}

		/// <inheritdoc />
		public bool Initialize()
		{
			lock (this)
			{
				if (!Idle())
					return false;
				lastCompilerError = null;
				compilerCurrentStatus = CompilerStatus.Initializing;
				CompilerThread = new Thread(new ThreadStart(InitializeImpl));
				CompilerThread.Start();
				return true;
			}
		}

		/// <summary>
		/// Gets the current idle <see cref="CompilerStatus"/> of the <see cref="CompilerManager"/>
		/// </summary>
		/// <returns><see cref="CompilerStatus.Initialized"/> if the <see cref="CompilerManager"/> is initialized, <see cref="CompilerStatus.Uninitialized"/> otherwise</returns>
		CompilerStatus IsInitialized()
		{
			var InterfaceAssemblyName = Assembly.GetAssembly(typeof(IServerInterface)).GetName().Name;
			if (IO.FileExists(Path.Combine(GameDirLive, InteropManager.BridgeDLLName)) || IO.FileExists(Path.Combine(GameDirLive, InterfaceAssemblyName, ".dll")))	//its a good tell, jim
				return CompilerStatus.Initialized;
			return CompilerStatus.Uninitialized;
		}
		
		/// <summary>
		/// Removes bridge symlinks from <see cref="GameDir"/>s
		/// </summary>
		void CleanGameFolder()
		{
			var GameDirABridge = Path.Combine(GameDirA, InteropManager.BridgeDLLName);
			if (IO.DirectoryExists(GameDirABridge))
				Directory.Delete(IO.ResolvePath(GameDirABridge));

			var GameDirBBridge = Path.Combine(GameDirB, InteropManager.BridgeDLLName);
			if (IO.DirectoryExists(GameDirBBridge))
				Directory.Delete(IO.ResolvePath(GameDirBBridge));
			
			if (IO.DirectoryExists(GameDirLive))
				Directory.Delete(IO.ResolvePath(GameDirLive));
		}

		/// <summary>
		/// Check that the <see cref="IRepoConfig"/> provided by the <see cref="RepoConfigProvider"/> and <see cref="Repo"/> match
		/// </summary>
		/// <returns><see langword="true"/> if the two <see cref="IRepoConfig"/>s match, <see langword="false"/> otherwise</returns>
		bool CheckRepoConfigsMatch() {
			if (RepoConfigProvider.GetRepoConfig() != Repo.GetRepoConfig())
			{
				lock (this)
				{
					lastCompilerError = "Repository TGS3.json does not match cached version! Please update the config appropriately!";
					compilerCurrentStatus = IsInitialized();
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Sets up initial <see cref="GameDir"/> symlinks
		/// </summary>
		public void InitializeImpl()
		{
			try
			{
				if (DreamDaemon.DaemonStatus() != DreamDaemonStatus.Offline)
				{
					lock (this)
					{
						lastCompilerError = "Dream daemon must not be running";
						compilerCurrentStatus = IsInitialized();
						return;
					}
				}

				if (!Repo.Exists()) //repo
				{
					lock (this)
					{
						lastCompilerError = "Repository is not setup!";
						compilerCurrentStatus = IsInitialized();
						return;
					}
				}

				if (!CheckRepoConfigsMatch())
					return;

				try
				{
					Chat.SendMessage("DM: Setting up symlinks...", MessageType.DeveloperInfo);
					CleanGameFolder();
					IO.DeleteDirectory(GameDir).Wait();

					IO.CreateDirectory(GameDirA);
					IO.CreateDirectory(GameDirB);

					Static.SymlinkTo(GameDirA);
					Static.SymlinkTo(GameDirB);
					
					IO.CreateSymlink(Path.Combine(GameDirA, InteropManager.BridgeDLLName), InteropManager.BridgeDLLName);
					IO.CreateSymlink(Path.Combine(GameDirB, InteropManager.BridgeDLLName), InteropManager.BridgeDLLName);

					IO.CreateSymlink(GameDirLive, GameDirA);
					
					lock (this)
					{
						compilerCurrentStatus = CompilerStatus.Compiling;
						silentCompile = true;
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					Chat.SendMessage("DM: Setup failed!", MessageType.DeveloperInfo);
					Logger.WriteError("Compiler Initialization Error: " + e.ToString(), EventID.DMInitializeCrash);
					lock (this)
					{
						lastCompilerError = e.ToString();
						compilerCurrentStatus = CompilerStatus.Uninitialized;
						return;
					}
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}
			CompileImpl();
		}

		//Returns the A or B dir in which the game is NOT running
		/// <summary>
		/// Returns the A or B dir in which <see cref="DreamDaemon"/> is not running
		/// </summary>
		/// <returns>The A or B dir in which <see cref="DreamDaemon"/> is not running</returns>
		string GetStagingDir()
		{
			string TheDir;
			if (!IO.DirectoryExists(GameDirLive))
				TheDir = GameDirA;
			else
			{
				File.Create(IO.ResolvePath(LiveDirTest)).Close();
				try
				{
					if (IO.FileExists(ADirTest))
						TheDir = GameDirA;
					else if (IO.FileExists(BDirTest))
						TheDir = GameDirB;
					else
						throw new Exception("Unable to determine current live directory!");
				}
				finally
				{
					IO.DeleteFile(LiveDirTest).Wait();
				}

				TheDir = InvertDirectory(TheDir);
			}

			//So TheDir is what the Live folder is NOT pointing to
			//Now we need to check if DD is running that folder and swap it if necessary

			var rsclock = Path.Combine(TheDir, String.Format("{0}.rsc.lk", Config.ProjectName));
			if (IO.FileExists(rsclock))
			{
				try
				{
					IO.DeleteFile(rsclock);
				}
				catch   //held open by byond
				{
					//This means there is a staged update waiting to be applied, we have to unstage it before we can work
					Directory.Delete(IO.ResolvePath(GameDirLive));
					IO.CreateSymlink(GameDirLive, TheDir);
					return InvertDirectory(TheDir);
				}
			}
			return TheDir;
		}

		/// <summary>
		/// Returns <see cref="GameDirA"/> if <paramref name="gameDirectory"/> is <see cref="GameDirB"/> and vice versa
		/// </summary>
		/// <param name="gameDirectory">One of <see cref="GameDirA"/> or <see cref="GameDirB"/></param>
		/// <returns>The opposite of <paramref name="gameDirectory"/></returns>
		string InvertDirectory(string gameDirectory)
		{
			if (gameDirectory == GameDirA)
				return GameDirB;
			else
				return GameDirA;
		}

		/// <summary>
		/// Copies code from the <see cref="Repo"/>, compiles it, and stages it to go live on the next <see cref="IInteropManager.OnWorldReboot"/>
		/// </summary>
		void CompileImpl()
		{
			try
			{
				if (Byond.GetVersion(ByondVersion.Installed) == null)
				{
					lock (this)
					{
						lastCompilerError = "BYOND not installed!";
						compilerCurrentStatus = CompilerStatus.Initialized;
						return;
					}
				}
				if (!CheckRepoConfigsMatch())
					return;


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

					var Config = RepoConfigProvider.GetRepoConfig();
					var deleteExcludeList = new List<string> { InteropManager.BridgeDLLName };
					deleteExcludeList.AddRange(Config.StaticDirectoryPaths);
					deleteExcludeList.AddRange(Config.DLLPaths);

					IO.DeleteDirectory(resurrectee, true, deleteExcludeList).Wait();
					IO.CreateDirectory(resurrectee + "/.git/logs");

					Static.SymlinkTo(resurrectee);

					if (!File.Exists(Path.Combine(resurrectee, BridgeDLLName)))
						CreateSymlink(Path.Combine(resurrectee, BridgeDLLName), RelativePath(BridgeDLLName));

					deleteExcludeList.Add(".git");
					Helpers.CopyDirectory(RelativePath(RepoPath), resurrectee, deleteExcludeList);
					CurrentSha = GetHead(false, out string error);
					//just the tip
					const string GitLogsDir = "/.git/logs";
					Helpers.CopyDirectory(RelativePath(RepoPath + GitLogsDir), resurrectee + GitLogsDir);
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
