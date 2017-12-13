using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;
using TGS.Server.Configuration;
using TGS.Server.IO;
using TGS.Server.Logging;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	sealed class CompilerManager : ICompilerManager, IDisposable
	{
		/// <summary>
		/// Live directory
		/// </summary>
		public static readonly string GameDirLive = IOManager.ConcatPath(GameDir, "Live");

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
		static readonly string GameDirA = IOManager.ConcatPath(GameDir, "A");
		/// <summary>
		/// Staging directory B
		/// </summary>
		static readonly string GameDirB = IOManager.ConcatPath(GameDir, "B");
		/// <summary>
		/// Test file path for checking if if <see cref="GameDirA"/> is <see cref="GameDirLive"/>
		/// </summary>
		static readonly string ADirTest = IOManager.ConcatPath(GameDirA, LiveFile);
		/// <summary>
		/// Test file path for checking if if <see cref="GameDirB"/> is <see cref="GameDirLive"/>
		/// </summary>
		static readonly string BDirTest = IOManager.ConcatPath(GameDirB, LiveFile);
		/// <summary>
		/// Test file path for determining which of <see cref="GameDirA"/> and <see cref="GameDirB"/> is <see cref="GameDirLive"/>
		/// </summary>
		static readonly string LiveDirTest = IOManager.ConcatPath(GameDirLive, LiveFile);
		/// <summary>
		/// Directory to find git logfiles in
		/// </summary>
		static readonly string GitLogsDir = IOManager.ConcatPath(".git", "logs");

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
		/// The <see cref="IActionEventManager"/> for the <see cref="CompilerManager"/>
		/// </summary>
		readonly IActionEventManager Events;

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
		/// If <see langword="true"/>, <see cref="currentTaskCanceller"/> will respond promptly, otherwise it may have to wait for file IO
		/// </summary>
		bool canCancelCompilation;

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
		/// <param name="events">The value of <see cref="Events"/></param>
		public CompilerManager(IInstanceLogger logger, IRepoConfigProvider repoConfigProvider, IIOManager io, IInstanceConfig config, IChatManager chat, IRepositoryManager repo, IInteropManager interop, IDreamDaemonManager dreamDaemon, IStaticManager _static, IByondManager byond, IActionEventManager events)
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
			Events = events;

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

		/// <summary>
		/// Attempts to cancel <see cref="currentTask"/>
		/// </summary>
		/// <returns><see langword="true"/> if <see cref="currentTask"/> was waited upon, <see langword="false"/> otherwise</returns>
		bool CancelImpl()
		{
			Task t;
			CancellationTokenSource cts;
			lock (this)
			{
				t = currentTask;
				currentTask = null;
				cts = currentTaskCanceller;
				currentTaskCanceller = null;
			}
			var res = false;
			if (cts != null)
				cts.Cancel();
			if (t != null)
			{
				t.Wait();
				t.Dispose();
				res = true;
			}
			if (cts != null)
				cts.Dispose();
			return res;
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
			Cancel();
			CleanLiveDirTest();
		}

		/// <summary>
		/// Check if the <see cref="CompilerManager"/> is idle
		/// </summary>
		/// <returns><see langword="true"/> if the <see cref="CompilerManager"/> is idle, <see langword="false"/> otherwise</returns>
		bool Idle()
		{
			lock(this)
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
				currentTaskCanceller = new CancellationTokenSource();
				var token = currentTaskCanceller.Token;
				currentTask = Task.Factory.StartNew(() => InitializeImpl(token), TaskCreationOptions.LongRunning);
				return true;
			}
		}

		/// <summary>
		/// Gets the current idle <see cref="CompilerStatus"/> of the <see cref="CompilerManager"/>
		/// </summary>
		/// <returns><see cref="CompilerStatus.Initialized"/> if the <see cref="CompilerManager"/> is initialized, <see cref="CompilerStatus.Uninitialized"/> otherwise</returns>
		CompilerStatus IsInitialized()
		{
			var InterfaceAssemblyName = Assembly.GetAssembly(typeof(IClient)).GetName().Name;
			if (IO.FileExists(IOManager.ConcatPath(GameDirLive, InteropManager.BridgeDLLName)).Result || IO.FileExists(IOManager.ConcatPath(GameDirLive, InterfaceAssemblyName, ".dll")).Result)    //its a good tell, jim
				return CompilerStatus.Initialized;
			return CompilerStatus.Uninitialized;
		}

		/// <summary>
		/// Removes bridge symlinks from <see cref="GameDir"/>s
		/// </summary>
		void CleanGameFolder()
		{
			var GameDirABridge = IOManager.ConcatPath(GameDirA, InteropManager.BridgeDLLName);
			var tasks = new List<Task>();
			if (IO.DirectoryExists(GameDirABridge).Result)
				tasks.Add(IO.Unlink(IO.ResolvePath(GameDirABridge)));

			var GameDirBBridge = IOManager.ConcatPath(GameDirB, InteropManager.BridgeDLLName);
			if (IO.DirectoryExists(GameDirBBridge).Result)
				tasks.Add(IO.Unlink(IO.ResolvePath(GameDirBBridge)));

			if (IO.DirectoryExists(GameDirLive).Result)
				tasks.Add(IO.Unlink(IO.ResolvePath(GameDirLive)));

			Task.WhenAll(tasks).Wait();
		}

		/// <summary>
		/// Check that the <see cref="IRepoConfig"/> provided by the <see cref="RepoConfigProvider"/> and <see cref="Repo"/> match
		/// </summary>
		/// <returns><see langword="true"/> if the two <see cref="IRepoConfig"/>s match, <see langword="false"/> otherwise</returns>
		bool CheckRepoConfigsMatch()
		{
			if (!RepoConfigProvider.GetRepoConfig().Equals(Repo.GetRepoConfig()))
				lock (this)
				{
					lastCompilerError = "Repository TGS3.json does not match cached version! Please update the config appropriately!";
					compilerCurrentStatus = IsInitialized();
					return false;
				}
			return true;
		}

		/// <summary>
		/// Sets up initial <see cref="GameDir"/> symlinks
		/// </summary>
		public void InitializeImpl(CancellationToken cancellationToken)
		{
			try
			{
				if (DreamDaemon.DaemonStatus() != DreamDaemonStatus.Offline)
					lock (this)
					{
						lastCompilerError = "Dream daemon must not be running";
						compilerCurrentStatus = IsInitialized();
						return;
					}

				Chat.SendMessage("DM: Setting up symlinks...", MessageType.DeveloperInfo);
				try
				{
					cancellationToken.ThrowIfCancellationRequested();
					CleanGameFolder();
					cancellationToken.ThrowIfCancellationRequested();
					IO.DeleteDirectory(GameDir).Wait();
					cancellationToken.ThrowIfCancellationRequested();

					IO.CreateDirectory(GameDirA).Wait();
					cancellationToken.ThrowIfCancellationRequested();
					IO.CreateDirectory(GameDirB).Wait();
					cancellationToken.ThrowIfCancellationRequested();

					Static.SymlinkTo(GameDirA);
					cancellationToken.ThrowIfCancellationRequested();
					Static.SymlinkTo(GameDirB);
					cancellationToken.ThrowIfCancellationRequested();

					IO.CreateSymlink(IOManager.ConcatPath(GameDirA, InteropManager.BridgeDLLName), InteropManager.BridgeDLLName).Wait();
					cancellationToken.ThrowIfCancellationRequested();
					IO.CreateSymlink(IOManager.ConcatPath(GameDirB, InteropManager.BridgeDLLName), InteropManager.BridgeDLLName).Wait();
					cancellationToken.ThrowIfCancellationRequested();

					IO.CreateSymlink(GameDirLive, GameDirA).Wait();

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
				cancellationToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException)
			{
				lock (this)
				{
					currentTask = null;
					currentTaskCanceller = null;
				}
			}
			CompileImpl(cancellationToken);
		}

		//Returns the A or B dir in which the game is NOT running
		/// <summary>
		/// Returns the A or B dir in which <see cref="DreamDaemon"/> is not running
		/// </summary>
		/// <returns>The A or B dir in which <see cref="DreamDaemon"/> is not running</returns>
		string GetStagingDir()
		{
			string TheDir;
			if (!IO.DirectoryExists(GameDirLive).Result)
				TheDir = GameDirA;
			else
			{
				IO.Touch(LiveDirTest).Wait();
				try
				{
					if (IO.FileExists(ADirTest).Result)
						TheDir = GameDirA;
					else if (IO.FileExists(BDirTest).Result)
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

			var rsclock = IOManager.ConcatPath(TheDir, String.Format("{0}.rsc.lk", Config.ProjectName));
			if (IO.FileExists(rsclock).Result)
			{
				try
				{
					IO.DeleteFile(rsclock);
				}
				catch   //held open by byond
				{
					//This means there is a staged update waiting to be applied, we have to unstage it before we can work
					IO.Unlink(GameDirLive).Wait();
					IO.CreateSymlink(GameDirLive, TheDir).Wait();
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
		void CompileImpl(CancellationToken cancellationToken)
		{
			try
			{
				if (Byond.GetVersion(ByondVersion.Installed) == null)
					lock (this)
					{
						lastCompilerError = "BYOND not installed!";
						compilerCurrentStatus = CompilerStatus.Initialized;
						return;
					}

				cancellationToken.ThrowIfCancellationRequested();

				if (!CheckRepoConfigsMatch())
					return;


				bool silent;
				lock (this)
				{
					silent = silentCompile;
					silentCompile = false;
				}

				if (!silent)
					Chat.SendMessage("DM: Compiling...", MessageType.DeveloperInfo);

				var resurrectee = GetStagingDir();  //non-relative
				cancellationToken.ThrowIfCancellationRequested();

				var Config = RepoConfigProvider.GetRepoConfig();
				var deleteExcludeList = new List<string> { InteropManager.BridgeDLLName };
				deleteExcludeList.AddRange(Config.StaticDirectoryPaths);
				deleteExcludeList.AddRange(Config.DLLPaths);

				IO.DeleteDirectory(resurrectee, true, deleteExcludeList).Wait();
				cancellationToken.ThrowIfCancellationRequested();
				IO.CreateDirectory(resurrectee + "/.git/logs").Wait();
				cancellationToken.ThrowIfCancellationRequested();

				Static.SymlinkTo(resurrectee);
				cancellationToken.ThrowIfCancellationRequested();

				if (!IO.FileExists(IOManager.ConcatPath(resurrectee, InteropManager.BridgeDLLName)).Result)
					IO.CreateSymlink(IOManager.ConcatPath(resurrectee, InteropManager.BridgeDLLName), InteropManager.BridgeDLLName).Wait();
				cancellationToken.ThrowIfCancellationRequested();

				deleteExcludeList.Add(".git");
				var CurrentSha = Repo.GetHead(false, out string error);
				Task.WaitAll(new List<Task>
				{
					Repo.CopyTo(resurrectee, deleteExcludeList),
					Repo.CopyToRestricted(resurrectee, new List<string> { GitLogsDir })
				}.ToArray());

				var res = Repo.CreateBackup();
				if (res != null)
					lock (this)
					{
						lastCompilerError = res;
						compilerCurrentStatus = CompilerStatus.Initialized;
						return;
					}
				cancellationToken.ThrowIfCancellationRequested();

				var dmeName = ProjectName() + ".dme";
				var dmePath = resurrectee + "/" + dmeName;
				if (!IO.FileExists(dmePath).Result)
				{
					var errorMsg = String.Format("Could not find {0}!", dmeName);
					Chat.SendMessage("DM: " + errorMsg, MessageType.DeveloperInfo);
					Logger.WriteError(errorMsg, EventID.DMCompileCrash);
					lock (this)
					{
						lastCompilerError = errorMsg;
						compilerCurrentStatus = CompilerStatus.Initialized;
						return;
					}
				}

				if (!Events.HandleEvent(ActionEvent.Precompile))
				{
					lastCompilerError = "The precompile hook failed";
					compilerCurrentStatus = CompilerStatus.Initialized;   //still fairly valid
					Logger.WriteWarning("Precompile hook failed!", EventID.DMCompileError);
					return;
				}

				canCancelCompilation = true;

				using (var DM = new Process())  //will kill the process if the thread is terminated
				{
					DM.StartInfo.FileName = Byond.LockDMExecutable(false, out error);
					if (error != null)
						lock (this)
						{
							lastCompilerError = error;
							compilerCurrentStatus = CompilerStatus.Initialized;
							return;
						}
					DM.StartInfo.Arguments = String.Format("-clean {0}", IO.ResolvePath(dmePath));
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
						DM.Start();
						DM.BeginOutputReadLine();
						DM.WaitForExitAsync(cancellationToken).Wait();
						cancellationToken.ThrowIfCancellationRequested();
						DM.CancelOutputRead();
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

					if (DM.ExitCode == 0)
					{
						DreamDaemon.RunSuspended(() =>
						{
							if (IO.DirectoryExists(GameDirLive).Result)
								//these next two lines should be atomic but this is the best we can do
								IO.Unlink(GameDirLive).Wait();
							IO.CreateSymlink(GameDirLive, resurrectee).Wait();
						});
						var staged = DreamDaemon.DaemonStatus() != DreamDaemonStatus.Offline;
						if (!Events.HandleEvent(ActionEvent.Postcompile))
						{
							lock (this)
								lastCompilerError = "The postcompile hook failed";
							Logger.WriteWarning("Postcompile hook failed!", EventID.DMCompileError);
							return;
						}
						Repo.UpdateLiveSha(CurrentSha);
						var msg = String.Format("Compile complete!{0}", !staged ? "" : " Server will update next round.");
						Interop.WorldAnnounce("Server updated, changes will be applied on reboot...");
						Chat.SendMessage("DM: " + msg, MessageType.DeveloperInfo);
						Logger.WriteInfo(msg, EventID.DMCompileSuccess);
						lock (this)
						{
							if (staged)
								updateStaged = true;
							lastCompilerError = null;
						}
					}
					else
					{
						Chat.SendMessage("DM: Compile failed!", MessageType.DeveloperInfo); //Also happens for warnings
						Logger.WriteWarning("Compile error: " + OutputList.ToString(), EventID.DMCompileError);
						lock (this)
							lastCompilerError = "DM compile failure";
					}
				}

			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (Exception e)
			{
				Chat.SendMessage("DM: Compiler thread crashed!", MessageType.DeveloperInfo);
				Logger.WriteError("Compile manager errror: " + e.ToString(), EventID.DMCompileCrash);
				lock (this)
					lastCompilerError = e.ToString();
			}
			finally
			{
				canCancelCompilation = false;
				lock (this)
				{
					currentTask = null;
					currentTaskCanceller = null;
					compilerCurrentStatus = CompilerStatus.Initialized;
					if (cancellationToken.IsCancellationRequested)
					{
						Chat.SendMessage("DM: Compile cancelled!", MessageType.DeveloperInfo);
						Logger.WriteInfo("Compilation cancelled", EventID.DMCompileCancel);
					}
				}
			}
		}
		//kicks off the compiler thread
		/// <inheritdoc />
		public bool Compile(bool silent = false)
		{
			lock (this)
			{
				if (compilerCurrentStatus != CompilerStatus.Initialized)
					return false;
				silentCompile = silent;
				lastCompilerError = null;
				compilerCurrentStatus = CompilerStatus.Compiling;
				currentTaskCanceller = new CancellationTokenSource();
				var token = currentTaskCanceller.Token;
				currentTask = Task.Factory.StartNew(() => CompileImpl(token), TaskCreationOptions.LongRunning);
			}
			return true;
		}

		/// <inheritdoc />
		public string ProjectName()
		{
			return Config.ProjectName;
		}

		/// <inheritdoc />
		public void SetProjectName(string projectName)
		{
			lock (this)
				Config.ProjectName = projectName;
		}

		/// <inheritdoc />
		public string Cancel()
		{
			lock (this)
			{
				if (compilerCurrentStatus != CompilerStatus.Compiling)
					return "Invalid state for cancellation!";
				if (CancelImpl() && !canCancelCompilation)
					return "Compilation will be cancelled when the repo copy is complete";
				return null;
			}
		}
	}
}
