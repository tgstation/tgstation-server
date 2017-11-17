using SimpleInjector;
using SimpleInjector.Integration.Wcf;
using System;
using System.IO;
using System.ServiceModel;
using TGS.Interface.Components;
using TGS.Server.Components;

namespace TGS.Server
{
	/// <summary>
	/// The class which holds all interface components. There are no safeguards for call race conditions so these must be guarded against internally
	/// </summary>
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	sealed class Instance : IDisposable, ITGConnectivity, IInstance, IInstanceLogger
	{
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
		/// Container for <see cref="Components"/> of this instance
		/// </summary>
		Container container;

		/// <summary>
		/// Constructs and a <see cref="Instance"/>
		/// </summary>
		/// <param name="config">The value for <see cref="Config"/></param>
		/// <param name="logger">The value for <see cref="Logger"/></param>
		/// <param name="loggingIDProvider">The value for <see cref="LoggingIDProvider"/></param>
		/// <param name="serverConfig">The value for <see cref="ServerConfig"/></param>
		public Instance(IInstanceConfig config, ILogger logger, ILoggingIDProvider loggingIDProvider, IServerConfig serverConfig)
		{
			LoggingID = loggingIDProvider.Get();
			Logger = logger;
			Config = config;
			ServerConfig = serverConfig;

			LoggingIDProvider = loggingIDProvider;
			WriteInfo(String.Format("Instance {0} ({1}) assigned logging ID", Config.Name, Config.Directory), EventID.InstanceIDAssigned);

			try
			{
				container = new Container();

				container.Options.DefaultLifestyle = new WcfOperationLifestyle();

				container.RegisterSingleton(ServerConfig);
				container.RegisterSingleton(Config);

				container.RegisterSingleton<IInstance>(this);
				container.RegisterSingleton<IInstanceLogger>(this);
				container.RegisterSingleton<ITGConnectivity>(this);

				container.Register<IByondManager, ByondManager>();
				container.Register<IDreamDaemonManager, DreamDaemonManager>();
				container.Register<IInteropManager, InteropManager>();
				container.Register<IChatManager, ChatManager>();
				container.Register<IRepositoryManager, RepositoryManager>();
				container.Register<IRepositoryProvider, RepositoryProvider>();

				container.Verify();

				Chat = container.GetInstance<IChatManager>();
			}
			catch
			{
				loggingIDProvider.Release(LoggingID);
				throw;
			}
		}

		/// <summary>
		/// Cleans up the <see cref="Instance"/>
		/// </summary>
		public void Dispose()
		{
			if (container != null)
			{
				container.Dispose();
				container = null;
				Config.Save();
				LoggingIDProvider.Release(LoggingID);
			}
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
		/// Creates a <see cref="SimpleInjectorServiceHost"/> based on <see cref="container"/>
		/// </summary>
		/// <param name="baseAddresses">The base addresses for the <see cref="ServiceHost"/></param>
		/// <returns>The new <see cref="ServiceHost"/></returns>
		public ServiceHost CreateServiceHost(Uri[] baseAddresses)
		{
			return new SimpleInjectorServiceHost(container, this, baseAddresses);
		}
	}
}
