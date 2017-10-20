using System;
using System.ServiceModel;
using TGServiceInterface.Components;

namespace TGServerService
{
	//I know the fact that this is one massive partial class is gonna trigger everyone
	//There really was no other succinct way to do it

	//this line basically says make one instance of the service, use it multithreaded for requests, and never delete it

	/// <summary>
	/// The class which holds all interface components
	/// </summary>
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	partial class ServerInstance : IDisposable, ITGSService, ITGConnectivity
	{
		/// <summary>
		/// Constructs and a <see cref="ServerInstance"/>
		/// </summary>
		public ServerInstance()
		{
			FindTheDroidsWereLookingFor();
			InitChat();
			InitRepo();
			InitByond();
			InitCompiler();
			InitDreamDaemon();
		}

		/// <summary>
		/// Cleans up the <see cref="ServerInstance"/>
		/// </summary>
		void RunDisposals()
		{
			DisposeDreamDaemon();
			DisposeCompiler();
			DisposeByond();
			DisposeRepo();
			DisposeChat();
		}

		/// <inheritdoc />
		public string Version()
		{
			return Service.Version;
		}

		/// <inheritdoc />
		public void VerifyConnection() { }

		/// <inheritdoc />
		public void PrepareForUpdate()
		{
			Properties.Settings.Default.ReattachToDD = true;
			SendMessage("SERVICE: Update started...", MessageType.DeveloperInfo);
		}

		//mostly generated code with a call to RunDisposals()
		//you don't need to open this
		#region IDisposable Support
		/// <summary>
		/// To detect redundant <see cref="Dispose()"/> calls
		/// </summary>
		private bool disposedValue = false;

		/// <summary>
		/// Implements the <see cref="IDisposable"/> pattern. Calls <see cref="RunDisposals"/>
		/// </summary>
		/// <param name="disposing"><see langword="true"/> if <see cref="Dispose()"/> was called manually, <see langword="false"/> if it was from the finalizer</param>
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
		/// <summary>
		/// Implements the <see cref="IDisposable"/> pattern
		/// </summary>
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
