using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TGServiceInterface;
using System.Threading;

namespace TGServerService
{
	//knobs and such
	partial class TGStationServer : ITGConfig
	{
		object configLock = new object();	//for atomic reads/writes

		//public api
		public string MoveServer(string new_location)
		{
			var Config = Properties.Settings.Default;
			try
			{
				var di1 = new DirectoryInfo(Config.ServerDirectory);
				var di2 = new DirectoryInfo(new_location);

				var copy = di1.Root.FullName != di2.Root.FullName;

				if (copy && File.Exists(PrivateKeyPath))
					return String.Format("Unable to perform a cross drive server move with the {0}. Copy aborted!", PrivateKeyPath);

				new_location = di2.FullName;

				while (di2.Parent != null)
					if (di2.Parent.FullName == di1.FullName)
						return "Cannot move to child of current directory!";
					else
						di2 = di2.Parent;

				if (!Monitor.TryEnter(RepoLock))
					return "Repo locked!";
				try
				{
					if (RepoBusy)
						return "Repo busy!";
					DisposeRepo();
					if (!Monitor.TryEnter(ByondLock))
						return "BYOND locked";
					try
					{
						if (updateStat != TGByondStatus.Idle)
							return "BYOND busy!";
						if (!Monitor.TryEnter(CompilerLock))
							return "Compiler locked!";

						try
						{
							if (compilerCurrentStatus != TGCompilerStatus.Uninitialized && compilerCurrentStatus != TGCompilerStatus.Initialized)
								return "Compiler busy!";
							if (!Monitor.TryEnter(watchdogLock))
								return "Watchdog locked!";
							try
							{
								if (currentStatus != TGDreamDaemonStatus.Offline)
									return "Watchdog running!";
								lock (configLock)
								{
									CleanGameFolder();
									Program.DeleteDirectory(GameDir);
									string error = null;
									if (copy)
									{
										Program.CopyDirectory(Config.ServerDirectory, new_location);
										Directory.CreateDirectory(new_location);
										Environment.CurrentDirectory = new_location;
										try
										{
											Program.DeleteDirectory(Config.ServerDirectory);
										}
										catch (Exception e)
										{
											error = "The move was successful, but the path " + Config.ServerDirectory + " was unable to be deleted fully!";
											TGServerService.WriteWarning(String.Format("Server move from {0} to {1} partial success: {2}", Config.ServerDirectory, new_location, e.ToString()), TGServerService.EventID.ServerMovePartial);
										}
									}
									else
									{
										try
										{
											Environment.CurrentDirectory = di2.Root.FullName;
											Directory.Move(Config.ServerDirectory, new_location);
											Environment.CurrentDirectory = new_location;
										}
										catch
										{
											Environment.CurrentDirectory = Config.ServerDirectory;
											throw;
										}
									}
									TGServerService.WriteInfo(String.Format("Server moved from {0} to {1}", Config.ServerDirectory, new_location), TGServerService.EventID.ServerMoveComplete);
									Config.ServerDirectory = new_location;
									return null;
								}
							}
							finally
							{
								Monitor.Exit(watchdogLock);
							}
						}
						finally
						{
							Monitor.Exit(CompilerLock);
						}
					}
					finally
					{
						Monitor.Exit(ByondLock);
					}
				}
				finally
				{
					Monitor.Exit(RepoLock);
				}
			}
			catch (Exception e)
			{
				TGServerService.WriteError(String.Format("Server move from {0} to {1} failed: {2}", Config.ServerDirectory, new_location, e.ToString()), TGServerService.EventID.ServerMoveFailed);
				return e.ToString();
			}
		}
		
		//public api
		public string ServerDirectory()
		{
			return Environment.CurrentDirectory;
		}

		//public api
		public string ReadText(string staticRelativePath, bool repo, out string error)
		{
			try
			{
				var configDir = repo ? RepoPath : StaticDirs;

				var path = Path.Combine(configDir, staticRelativePath);
				lock (configLock)
				{
					var di1 = new DirectoryInfo(configDir);
					if (repo)
					{
						//ensure we aren't trying to read anything outside the static dirs
						var Config = LoadRepoConfig();
						if (Config == null)
						{
							error = "Unable to load static directory configuration";
							return null;
						}
						var Found = false;
						foreach (var I in Config.StaticDirectoryPaths)
						{
							if (di1.FullName == new DirectoryInfo(Path.Combine(RepoPath, I)).FullName)
							{
								Found = true;
								break;
							}
						}
						if (!Found)
						{
							error = "File is not in a configured static directory!";
							return null;
						}
					}

					var di2 = new DirectoryInfo(new FileInfo(path).Directory.FullName);

					var good = false;
					while (di2 != null)
					{
						if (di2.FullName == di1.FullName)
						{
							good = true;
							break;
						}
						else di2 = di2.Parent;
					}

					if (!good)
					{
						error = "Cannot read above static directories!";
						return null;
					}

					error = null;
					return File.ReadAllText(path);
				}
			}
			catch (Exception e)
			{
				error = e.ToString();
				return null;
			}
		}

		public string WriteText(string staticRelativePath, string data)
		{
			try
			{
				var path = Path.Combine(StaticDirs, staticRelativePath);
				lock (configLock)
				{
					var di1 = new DirectoryInfo(StaticDirs);
					var destdir = new FileInfo(path).Directory.FullName;
					var di2 = new DirectoryInfo(destdir);

					var good = false;
					while (di2 != null)
					{
						if (di2.FullName == di1.FullName)
						{
							good = true;
							break;
						}
						else di2 = di2.Parent;
					}

					if (!good)
						return "Cannot write above static directories!";
					Directory.CreateDirectory(destdir);
					File.WriteAllText(path, data);
					return null;
				}
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}
	}
}
