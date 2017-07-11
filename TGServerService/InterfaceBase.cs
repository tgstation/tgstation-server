using System;
using System.ServiceModel;
using TGServiceInterface;

namespace TGServerService
{
	//I know the fact that this is one massive partial class is gonna trigger everyone
	//There really was no other succinct way to do it

	//this line basically says make one instance of the service, use it multithreaded for requests, and never delete it
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	partial class TGStationServer : IDisposable, ITGSService, ITGServerUpdater
	{

		//call partial constructors/destructors from here
		//called when the service is started
		public TGStationServer()
		{
			InitChat();
			InitByond();
			InitCompiler();
			InitDreamDaemon();
		}

		//called when the service is stopped
		void RunDisposals()
		{
			DisposeDreamDaemon();
			DisposeCompiler();
			DisposeByond();
			DisposeRepo();
			DisposeChat();
		}

		public string Version()
		{
			return TGServerService.Version;
		}

		//one stop update
		public string UpdateServer(TGRepoUpdateMethod updateType, ushort testmerge_pr)
		{
			try
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

				GenerateChangelog(out res);
				if (res != null)
					return res;

				if (SSHAuth())
				{
					if (LocalIsOrigin())
						res = "Skipping changelog push, local branch does not match remote";
					else
					{
						res = Commit();
						if (res == null)
							res = Push();
					}
				}

				if (!Compile(true))
					return "Compilation could not be started!";
				return res;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		//public api
		public void VerifyConnection() { }

		//public api
		public void StopForUpdate()
		{
			PrepareForUpdate();
		}

		//public api
		public void PrepareForUpdate()
		{
			Properties.Settings.Default.ReattachToDD = true;
			SendMessage("SERVICE: Update started...", ChatMessageType.DeveloperInfo);
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
