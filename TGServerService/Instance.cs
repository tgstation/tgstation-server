using System;
using System.IO;
using System.ServiceModel;
using System.Threading;
using TGServiceInterface;

namespace TGServerService
{
	//I know the fact that this is one massive partial class is gonna trigger everyone
	//There really was no other succinct way to do it

	//this line basically says make one instance of the service, use it multithreaded for requests, and never delete it
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	partial class TGStationServer : IDisposable, ITGInstance
	{
		readonly string instanceName;
		public Properties.Instance Config;

		//call partial constructors/destructors from here
		//called when the service is started
		public TGStationServer(string name, Properties.Instance cfg)
		{
			instanceName = name;
			Config = cfg;

			if (Config.UpgradeRequired)
			{
				Config.Upgrade();
				Config.UpgradeRequired = false;
			}

			if (!Directory.Exists(Config.ServerDirectory))
				Directory.CreateDirectory(Config.ServerDirectory);

			InitChat();
			InitByond();
			InitCompiler();
			InitDreamDaemon();
		}

		//called when the service is stopped
		void RunDisposals()
		{
			try
			{
				DisposeDreamDaemon();
				DisposeCompiler();
				DisposeByond();
				DisposeRepo();
				DisposeChat();
			}
			finally
			{
				Config.Save();
			}
		}

		public string InstanceName()
		{
			return instanceName;
		}

		public int InstanceID()
		{
			return Config.InstanceID;
		}

		public void Delete()
		{
			ThreadPool.QueueUserWorkItem( _ => { TGServerService.DeleteInstance(Config.InstanceID); });
		}

		//one stop update
		public string UpdateServer(TGRepoUpdateMethod updateType, bool push_changelog_if_enabled, ushort testmerge_pr)
		{
			string res;
			switch (updateType)
			{
				case TGRepoUpdateMethod.Hard:
				case TGRepoUpdateMethod.Merge:
					res = Update(updateType == TGRepoUpdateMethod.Hard);
					if (res != null && res != RepoErrorUpToDate)
						return res;
					break;
				case TGRepoUpdateMethod.Reset:
					res = Reset(true);
					if (res != null)
						return res;
					break;
				case TGRepoUpdateMethod.None:
					break;
			}

			if (testmerge_pr != 0)
			{
				res = MergePullRequest(testmerge_pr);
				if (res != null && res != RepoErrorUpToDate)
					return res;
			}

			GenerateChangelog(out res);
			if (res != null)
				return res;

			if (push_changelog_if_enabled && SSHAuth())
			{
				res = Commit();
				if (res != null)
					return res;
				res = Push();
				if (res != null)
					return res;
			}

			if (!Compile())
				return "Compilation could not be started!";
			return null;
		}

		//mostly generated code with a call to RunDisposals()
		//you don't need to open this
		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					RunDisposals();
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		public string PrepPath(string path)
		{
			return Config.ServerDirectory + Path.DirectorySeparatorChar + path;
		}
		//public api
		public string MoveInstance(string new_location)
		{
			try
			{
				new_location = new DirectoryInfo(new_location).Root.FullName;
				var copy = new DirectoryInfo(Config.ServerDirectory).Root.FullName != new_location;

				if (copy && File.Exists(PrivateKeyPath))
					return String.Format("Unable to perform a cross drive server move with the {0}. Copy aborted!", PrivateKeyPath);

				if(Program.IsDirectoryParentOf(Config.ServerDirectory, new_location))
					return "Cannot move to child of current directory!";

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
										try
										{
											Program.DeleteDirectory(Config.ServerDirectory);
										}
										catch (Exception e)
										{
											error = "The move was successful, but the path " + Config.ServerDirectory + " was unable to be deleted fully!";
											TGServerService.WriteWarning(String.Format("Server move from {0} to {1} partial success: {2}", Config.ServerDirectory, new_location, e.ToString()), TGServerService.EventID.ServerMovePartial, this);
										}
									}
									else
										Directory.Move(Config.ServerDirectory, new_location);
									TGServerService.WriteInfo(String.Format("Server moved from {0} to {1}", Config.ServerDirectory, new_location), TGServerService.EventID.ServerMoveComplete, this);
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
				TGServerService.WriteError(String.Format("Server move from {0} to {1} failed: {2}", Config.ServerDirectory, new_location, e.ToString()), TGServerService.EventID.ServerMoveFailed, this);
				return e.ToString();
			}
		}

		//public api
		public string InstanceDirectory()
		{
			return Config.ServerDirectory;
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~TGStationServer() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
