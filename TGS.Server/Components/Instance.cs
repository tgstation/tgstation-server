using System;
using System.ServiceModel;
using System.Timers;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <summary>
	/// The root class for a <see cref="Server"/> <see cref="Instance"/>
	/// </summary>
	sealed class Instance : IInstance, IConnectivityManager, IRepoConfigProvider, IInstanceLogger, IDisposable
	{
		/// <summary>
		/// Conversion from minutes to milliseconds
		/// </summary>
		const int AutoUpdateTimerMultiplier = 60000;
		/// <summary>
		/// Used to assign the instance to event IDs
		/// </summary>
		readonly byte LoggingID;

		/// <summary>
		/// The <see cref="IInstanceConfig"/> for the <see cref="Instance"/>
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
		/// The <see cref="IChatManager"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IChatManager Chat;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IIOManager IO;
		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IRepositoryManager Repo;
		/// <summary>
		/// The <see cref="ICompilerManager"/> for the <see cref="Instance"/>
		/// </summary>
		readonly ICompilerManager Compiler;
		/// <summary>
		/// The <see cref="IAdministrationManager"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IAdministrationManager Administration;
		/// <summary>
		/// The <see cref="IDependencyInjector"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IDependencyInjector Container;

		/// <summary>
		/// <see cref="Timer"/> used for auto update operations
		/// </summary>
		Timer autoUpdateTimer;

		/// <summary>
		/// Constructs and a <see cref="Instance"/>
		/// </summary>
		/// <param name="config">The value for <see cref="Config"/></param>
		/// <param name="logger">The value for <see cref="Logger"/></param>
		/// <param name="loggingIDProvider">The value for <see cref="LoggingIDProvider"/></param>
		/// <param name="serverConfig">The value for <see cref="ServerConfig"/></param>
		/// <param name="container">The value of <see cref="Container"/></param>
		public Instance(IInstanceConfig config, ILogger logger, ILoggingIDProvider loggingIDProvider, IServerConfig serverConfig, IDependencyInjector container)
		{
			LoggingID = loggingIDProvider.Get();
			Logger = logger;
			Config = config;
			ServerConfig = serverConfig;
			Container = container;

			LoggingIDProvider = loggingIDProvider;
			WriteInfo(String.Format("Instance {0} ({1}) assigned logging ID", Config.Name, Config.Directory), EventID.InstanceIDAssigned);

			try
			{
				autoUpdateTimer = new Timer() { AutoReset = true, Interval = Math.Max(Config.AutoUpdateInterval * AutoUpdateTimerMultiplier, 1) };
				autoUpdateTimer.Elapsed += (a, b) => HandleAutoUpdate();

				container.Register(ServerConfig);
				container.Register(Config);

				container.Register<IInstance>(this);
				container.Register<IInstanceLogger>(this);
				container.Register<IRepoConfigProvider>(this);
				container.Register<IConnectivityManager>(this);

				container.Register<IRepositoryProvider, RepositoryProvider>();
				container.Register<IIOManager, InstanceIOManager>();

				//register the internals to the implementations
				container.Register<ICompilerManager, CompilerManager>();
				container.Register<IByondManager, ByondManager>();
				container.Register<IDreamDaemonManager, DreamDaemonManager>();
				container.Register<IInteropManager, InteropManager>();
				container.Register<IChatManager, ChatManager>();
				container.Register<IRepositoryManager, RepositoryManager>();
				container.Register<IStaticManager, StaticManager>();
				container.Register<IAdministrationManager, AdministrationManager>();
				container.Register<IActionEventManager, ActionEventManager>();

				//now register the interfaces to the aggregator
				container.Register<ITGAdministration, WCFContractRelay>();
				container.Register<ITGByond, WCFContractRelay>();
				container.Register<ITGChat, WCFContractRelay>();
				container.Register<ITGCompiler, WCFContractRelay>();
				container.Register<ITGConnectivity, WCFContractRelay>();
				container.Register<ITGDreamDaemon, WCFContractRelay>();
				container.Register<ITGInstance, WCFContractRelay>();
				container.Register<ITGInterop, WCFContractRelay>();
				container.Register<ITGRepository, WCFContractRelay>();
				container.Register<ITGStatic, WCFContractRelay>();

				//Finally register the aggregator to itself, necessary so the container can resolve it
				container.Register<WCFContractRelay, WCFContractRelay>();

				//lock, load, and dependency cycle me daddy
				container.Setup();

				Chat = container.GetInstance<IChatManager>();
				IO = container.GetInstance<IIOManager>();
				Repo = container.GetInstance<IRepositoryManager>();
				Compiler = container.GetInstance<ICompilerManager>();
				Administration = container.GetInstance<IAdministrationManager>();

				if (Config.AutoUpdateInterval != 0)
					HandleAutoUpdate();
			}
			catch
			{
				loggingIDProvider.Release(LoggingID);
				throw;
			}
		}

		/// <summary>
		/// Automatically update the server
		/// </summary>
		void HandleAutoUpdate()
		{
			lock (this)
			{
				if (autoUpdateTimer == null)
					return;
				autoUpdateTimer.Stop();
				try
				{
					//TODO: Preserve test merges?
					if(Repo.UpdateImpl(true, false) == null)
						Compiler.Compile(true);
				}
				catch { }
				finally
				{
					if (Config.AutoUpdateInterval != 0)
						autoUpdateTimer.Start();
				}
			}
		}

		/// <summary>
		/// Cleans up the <see cref="Instance"/>
		/// </summary>
		public void Dispose()
		{
			lock(this)
				if(autoUpdateTimer != null)
				{
					autoUpdateTimer.Dispose();
					autoUpdateTimer = null;
				}
			Container.Dispose();
			Config.Save();
			LoggingIDProvider.Release(LoggingID);
		}

		/// <inheritdoc />
		public void WriteInfo(string message, EventID id)
		{
			Logger.WriteInfo(message, id, LoggingID);
		}

		/// <inheritdoc />
		public void WriteError(string message, EventID id)
		{
			Logger.WriteError(message, id, LoggingID);
		}

		/// <inheritdoc />
		public void WriteWarning(string message, EventID id)
		{
			Logger.WriteWarning(message, id, LoggingID);
		}

		/// <inheritdoc />
		public void WriteAccess(string username, bool authSuccess)
		{
			Logger.WriteAccess(String.Format("Access from: {0}", username), authSuccess, LoggingID);
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
				Chat.SendMessage("SERVICE: Update started...", MessageType.DeveloperInfo);
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

		/// <summary>
		/// Returns a configured a <see cref="ServiceHost"/> from <see cref="Container"/>
		/// </summary>
		/// <param name="baseAddresses">The base addresses for the <see cref="ServiceHost"/></param>
		/// <returns>The new <see cref="ServiceHost"/></returns>
		public ServiceHost CreateServiceHost(Uri[] baseAddresses)
		{
			var res = Container.CreateServiceHost(typeof(WCFContractRelay), baseAddresses);
			res.Authorization.ServiceAuthorizationManager = (Administration as ServiceAuthorizationManager) ?? res.Authorization.ServiceAuthorizationManager;
			return res;
		}

		/// <inheritdoc />
		public IRepoConfig GetRepoConfig()
		{
			return new RepoConfig(".", IO);
		}

		/// <inheritdoc />
		public string UpdateTGS3Json()
		{
			try
			{
				RepoConfig.Copy(RepositoryManager.RepoPath, ".", IO);
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public void SetAutoUpdateInterval(ulong newInterval)
		{
			lock (this)
			{
				Config.AutoUpdateInterval = newInterval;
				if (autoUpdateTimer == null)
					return;
				if (Config.AutoUpdateInterval != 0)
				{
					autoUpdateTimer.Interval = newInterval * AutoUpdateTimerMultiplier;
					autoUpdateTimer.Start();
				}
				else
					autoUpdateTimer.Stop();
			}
		}

		/// <inheritdoc />
		public ulong AutoUpdateInterval()
		{
			return Config.AutoUpdateInterval;
		}
	}
}
