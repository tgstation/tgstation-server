﻿using System;
using System.ServiceModel;
using System.Timers;
using TGS.Interface.Components;
using TGS.Server.IO;
using TGS.Server.IoC;
using TGS.Server.Configuration;
using TGS.Server.Logging;

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
				autoUpdateTimer.Elapsed += HandleAutoUpdate;

				Container.Register(ServerConfig);
				Container.Register(Config);

				Container.Register<IInstance>(this);
				Container.Register<IInstanceLogger>(this);
				Container.Register<IRepoConfigProvider>(this);
				Container.Register<IConnectivityManager>(this);

				Container.Register<IRepositoryProvider, RepositoryProvider>();
				Container.Register<IIOManager, InstanceIOManager>();

				//register the internals to the implementations
				Container.Register<ICompilerManager, CompilerManager>();
				Container.Register<IByondManager, ByondManager>();
				Container.Register<IDreamDaemonManager, DreamDaemonManager>();
				Container.Register<IInteropManager, InteropManager>();
				Container.Register<IChatManager, ChatManager>();
				Container.Register<IRepositoryManager, RepositoryManager>();
				Container.Register<IStaticManager, StaticManager>();
				Container.Register<IAdministrationManager, AdministrationManager>();
				Container.Register<IActionEventManager, ActionEventManager>();

				//now register the interfaces to the aggregator
				Container.Register<ITGAdministration, WCFContractRelay>();
				Container.Register<ITGByond, WCFContractRelay>();
				Container.Register<ITGChat, WCFContractRelay>();
				Container.Register<ITGCompiler, WCFContractRelay>();
				Container.Register<ITGConnectivity, WCFContractRelay>();
				Container.Register<ITGDreamDaemon, WCFContractRelay>();
				Container.Register<ITGInstance, WCFContractRelay>();
				Container.Register<ITGInterop, WCFContractRelay>();
				Container.Register<ITGRepository, WCFContractRelay>();
				Container.Register<ITGStatic, WCFContractRelay>();

				//Finally register the aggregator to itself, necessary so the container can resolve it
				Container.Register<WCFContractRelay, WCFContractRelay>();

				//lock, load, and dependency cycle me daddy
				Container.Setup();

				Chat = Container.GetComponent<IChatManager>();
				IO = Container.GetComponent<IIOManager>();
				Repo = Container.GetComponent<IRepositoryManager>();
				Compiler = Container.GetComponent<ICompilerManager>();
				Administration = Container.GetComponent<IAdministrationManager>();

				if (Config.AutoUpdateInterval != 0)
					HandleAutoUpdate(this, new EventArgs());
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
		void HandleAutoUpdate(object sender, EventArgs args)
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
			Config.Save(IO);
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

		/// <inheritdoc />
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
