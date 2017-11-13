using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using TGS.Interface.Components;

namespace TGS.Server
{
	//I know the fact that this is one massive partial class is gonna trigger everyone
	//There really was no other succinct way to do it (<= He's lying through his teeth, don't listen to him)

	//this line basically says take one instance of the service, use it multithreaded for requests, and never delete it

	/// <summary>
	/// The class which holds all interface components. There are no safeguards for call race conditions so these must be guarded against internally
	/// </summary>
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	sealed partial class Instance : IDisposable, ITGConnectivity, ITGInstance
	{
		/// <summary>
		/// Used to assign the instance to event IDs
		/// </summary>
		public readonly byte LoggingID;

		/// <summary>
		/// The configuration settings for the <see cref="Instance"/>
		/// </summary>
		readonly IInstanceConfig Config;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Instance"/>
		/// </summary>
		readonly ILogger Logger;

		/// <summary>
		/// The <see cref="ILoggingIDProvider"/> for the <see cref="Instance"/>
		/// </summary>
		readonly ILoggingIDProvider LoggingIDProvider;

		/// <summary>
		/// The <see cref="IServerConfig"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IServerConfig ServerConfig;

		/// <summary>
		/// Constructs and a <see cref="Instance"/>
		/// </summary>
		/// <param name="config">The value for <see cref="Config"/></param>
		/// <param name="logger">The value for <see cref="Logger"/></param>
		/// <param name="loggingIDProvider">The value for <see cref="LoggingIDProvider"/></param>
		/// <param name="serverConfig">The value for <see cref="ServerConfig"/></param>
		public Instance(IInstanceConfig config, ILogger logger, ILoggingIDProvider loggingIDProvider, IServerConfig serverConfig)
		{
			LoggingIDProvider = loggingIDProvider;
			LoggingID = loggingIDProvider.Get();
			Logger = logger;
			Config = config;
			ServerConfig = serverConfig;

			WriteInfo(String.Format("Instance {0} ({1}) assigned logging ID", Config.Name, Config.Directory), EventID.InstanceIDAssigned);

			FindTheDroidsWereLookingFor();
			InitEventHandlers();
			InitChat();
			InitRepo();
			InitByond();
			InitDreamDaemon();
			InitCompiler();
		}

		/// <summary>
		/// Cleans up the <see cref="Instance"/>
		/// </summary>
		void RunDisposals()
		{
			DisposeDreamDaemon();
			DisposeCompiler();
			DisposeByond();
			DisposeRepo();
			DisposeChat();
			DisposeAdministration();
			Config.Save();
			LoggingIDProvider.Release(LoggingID);
		}

		/// <summary>
		/// Writes information to the Windows event log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		void WriteInfo(string message, EventID id)
		{
			Logger.WriteInfo(message, id, LoggingID);
		}

		/// <summary>
		/// Writes an error to the Windows event log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		void WriteError(string message, EventID id)
		{
			Logger.WriteError(message, id, LoggingID);
		}

		/// <summary>
		/// Writes a warning to the Windows event log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		void WriteWarning(string message, EventID id)
		{
			Logger.WriteWarning(message, id, LoggingID);
		}

		/// <summary>
		/// Writes an access event to the Windows event log
		/// </summary>
		/// <param name="username">The (un)authenticated Windows user's name</param>
		/// <param name="authSuccess"><see langword="true"/> if <paramref name="username"/> authenticated sucessfully, <see langword="false"/> otherwise</param>
		void WriteAccess(string username, bool authSuccess)
		{
			Logger.WriteAccess(String.Format("Access from: {0}", username), authSuccess, LoggingID);
		}

		/// <summary>
		/// Converts relative paths to full <see cref="Instance"/> directory paths
		/// </summary>
		/// <returns></returns>
		string RelativePath(string path)
		{
			return Path.Combine(Config.Directory, path);
		}

		/// <inheritdoc />
		public string Version()
		{
			return Server.VersionString;
		}

		/// <inheritdoc />
		public void VerifyConnection() { }

		/// <inheritdoc />
		public void Reattach(bool silent)
		{
			Config.ReattachRequired = true;
			if(!silent)
				SendMessage("SERVICE: Update started...", MessageType.DeveloperInfo);
		}

		/// <inheritdoc />
		public string ServerDirectory()
		{
			return Config.Directory;
		}

		/// <summary>
		/// Sets <see cref="InstanceConfig.Enabled"/> of <see cref="Config"/> to <see langword="false"/>
		/// </summary>
		public void Offline()
		{
			Config.Enabled = false;
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
		void Dispose(bool disposing)
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
