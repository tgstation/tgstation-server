using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceProcess;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGServerService
{
	/// <summary>
	/// The windows service the application runs as
	/// </summary>
	sealed partial class Service : ServiceBase
	{
		/// <summary>
		/// The service version <see cref="string"/> based on the <see cref="FileVersionInfo"/>
		/// </summary>
		public static readonly string VersionString = "/tg/station 13 Server Service v" + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

		/// <summary>
		/// Singleton instance
		/// </summary>
		static Service ActiveService;

		/// <summary>
		/// Cancels WCF's user impersonation to allow clean access to writing log files
		/// </summary>
		public static void CancelImpersonation()
		{
			WindowsIdentity.Impersonate(IntPtr.Zero);
		}

		/// <summary>
		/// Writes information to Windows the event log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		public static void WriteInfo(string message, EventID id)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Information, (int)id);
		}
		/// <summary>
		/// Writes an error to the Windows event log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		public static void WriteError(string message, EventID id)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Error, (int)id);
		}
		/// <summary>
		/// Writes a warning to the Windows event log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		public static void WriteWarning(string message, EventID id)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Warning, (int)id);
		}

		/// <summary>
		/// Writes an access event to the Windows event log
		/// </summary>
		/// <param name="username">The (un)authenticated Windows user's name</param>
		/// <param name="authSuccess"><see langword="true"/> if <paramref name="username"/> authenticated sucessfully, <see langword="false"/> otherwise</param>
		public static void WriteAccess(string username, bool authSuccess)
		{
			ActiveService.EventLog.WriteEntry(String.Format("Access from: {0}", username), authSuccess ? EventLogEntryType.SuccessAudit : EventLogEntryType.FailureAudit, (int)EventID.Authentication);
		}

		/// <summary>
		/// The WCF host that contains <see cref="ITGSService"/> connects to
		/// </summary>
		ServiceHost serviceHost;
		IDictionary<string, ServiceHost> hosts;

		/// <summary>
		/// Migrates the .NET config from <paramref name="oldVersion"/> to <paramref name="newVersion"/>
		/// </summary>
		/// <param name="oldVersion">The version to migrate from</param>
		/// <param name="newVersion">The version to migrate to</param>
		void MigrateSettings(int oldVersion, int newVersion)
		{
			//Uneeded... So far...
		}
	
		//you should seriously not add anything here
		//Use OnStart instead
		/// <summary>
		/// Construct and run a <see cref="Service"/>. Can only execute in the context of the Windows service manager
		/// </summary>
		public Service()
		{
			var Config = Properties.Settings.Default;
			try
			{
				Environment.CurrentDirectory = Directory.CreateDirectory(Path.GetTempPath() + "/TGStationServerService").FullName;  //MOVE THIS POINTER BECAUSE ONE TIME I ALMOST ACCIDENTALLY NUKED MYSELF BY REFACTORING! http://imgur.com/zvGEpJD.png
				if (Properties.Settings.Default.UpgradeRequired)
				{
					var newVersion = Config.SettingsVersion;
					Config.Upgrade();
					var oldVersion = Config.SettingsVersion;
					Config.SettingsVersion = newVersion;

					MigrateSettings(oldVersion, newVersion);

					Config.UpgradeRequired = false;
					Config.Save();
				}
				ActiveService = this;
				ServiceName = "TG Station Server";
				Run(this);
			}
			finally
			{
				Config.Save();
			}
		}

		void ChangePortFromCommandLine(string[] args)
		{
			var Config = Properties.Settings.Default;

			for (var I = 0; I < args.Length - 1; ++I)
				if (args[I].ToLower() == "-port")
				{
					try
					{
						var res = Convert.ToUInt16(args[I + 1]);
						if (res == 0)
							throw new Exception("Cannot bind to port 0");
						Config.RemoteAccessPort = res;
					}
					catch (Exception e)
					{
						throw new Exception("Invalid argument for \"-port\"", e);
					}
					Config.Save();
					break;
				}
		}
		//when babby is formed
		protected override void OnStart(string[] args)
		{
			ChangePortFromCommandLine(args);

			SetupService();

			SetupInstances();

			OnlineAllHosts();
		}

		void SetupService()
		{
			serviceHost = CreateHost(this);
			AddEndpoint(serviceHost, typeof(ITGSService), Interface.MasterInterfaceName);
			serviceHost.Authorization.ServiceAuthorizationManager = new AdministrativeAuthorizationManager();	//only admins can diddle us
		}

		void OnlineAllHosts()
		{
			serviceHost.Open();
			foreach (var I in hosts)
				I.Value.Open();
		}

		static ServiceHost CreateHost(object singleton)
		{
			return new ServiceHost(singleton, new Uri[] { new Uri("net.pipe://localhost"), new Uri(String.Format("https://localhost:{0}", Properties.Settings.Default.RemoteAccessPort)) })
			{
				CloseTimeout = new TimeSpan(0, 0, 5)
			};
		}

		void SetupInstances()
		{
			var pathsToRemove = new List<string>();
			foreach (var I in Properties.Settings.Default.InstancePaths)
				if (SetupInstance(I) != null)
					pathsToRemove.Add(I);
		}
		ServiceHost SetupInstance(string path)
		{
			ServerInstance instance;
			try
			{
				var config = InstanceConfig.Load(path);
				if (hosts.ContainsKey(path))
				{
					var datInstance = ((ServerInstance)hosts[path].SingletonInstance);
					WriteError(String.Format("Unable to start instance at path {0}. Has the same name as instance at path {1} ({2}). Detaching...", path, datInstance.ServerDirectory(), datInstance.Config.Name), EventID.InstanceInitializationFailure);
					return null;
				}
				if (!config.Enabled)
					return null;
				instance = new ServerInstance(config);
			}
			catch (Exception e)
			{
				WriteError(String.Format("Unable to start instance at path {0}. Detaching... Error: {1}", path, e.ToString()), EventID.InstanceInitializationFailure);
				return null;
			}

			var host = CreateHost(instance);
			hosts.Add(instance.Config.Name, host);

			var endpointPrefix = String.Format("{0}/{1}", Interface.MasterInterfaceName, instance.Config.Name);
			foreach (var J in Interface.ValidInterfaces)
				AddEndpoint(host, J, endpointPrefix);

			host.Authorization.ServiceAuthorizationManager = instance;
			return host;
		}

		/// <summary>
		/// Adds a WCF endpoint for a component <paramref name="type"/>
		/// </summary>
		/// <param name="host"></param>
		/// <param name="typetype"></param>
		/// <param name="PipePrefix"></param>
		void AddEndpoint(ServiceHost host, Type typetype, string PipePrefix)
		{
			var bindingName = PipePrefix + "/" + typetype.Name;
			host.AddServiceEndpoint(typetype, new NetNamedPipeBinding() { SendTimeout = new TimeSpan(0, 0, 30), MaxReceivedMessageSize = Interface.TransferLimitLocal }, bindingName);
			var httpsBinding = new WSHttpBinding()
			{
				SendTimeout = new TimeSpan(0, 0, 40),
				MaxReceivedMessageSize = Interface.TransferLimitRemote
			};
			var requireAuth = typetype.Name != typeof(ITGConnectivity).Name;
			httpsBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
			httpsBinding.Security.Mode = requireAuth ? SecurityMode.TransportWithMessageCredential : SecurityMode.Transport;	//do not require auth for a connectivity check
			httpsBinding.Security.Message.ClientCredentialType = requireAuth ? MessageCredentialType.UserName : MessageCredentialType.None;
			host.AddServiceEndpoint(typetype, httpsBinding, bindingName);
		}

		/// <summary>
		/// Shutsdown the WCF <see cref="host"/> and calls <see cref="IDisposable.Dispose"/> on it's <see cref="ServerInstance"/>
		/// </summary>
		protected override void OnStop()
		{
			try
			{
				foreach (var I in hosts)
				{
					var host = I.Value;
					var instance = (ServerInstance)host.SingletonInstance;
					host.Close();
					instance.Dispose();
				}
			}
			catch (Exception e)
			{
				WriteError(e.ToString(), EventID.ServiceShutdownFail);
			}
		}

		/// <inheritdoc />
		public void VerifyConnection() { }

		/// <inheritdoc />
		public void PrepareForUpdate()
		{
			foreach (var I in hosts)
				((ServerInstance)I.Value.SingletonInstance).Reattach(false);
		}

		/// <inheritdoc />
		public ushort RemoteAccessPort()
		{
			return Properties.Settings.Default.RemoteAccessPort;
		}

		/// <inheritdoc />
		public string SetRemoteAccessPort(ushort port)
		{
			if (port == 0)
				return "Cannot bind to port 0";
			Properties.Settings.Default.RemoteAccessPort = port;
			return null;
		}

		/// <inheritdoc />
		public string Version()
		{
			return VersionString;
		}

		/// <inheritdoc />
		public IDictionary<string, string> ListInstances()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string CreateInstance(string Name, string path)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string ImportInstance(string path)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool InstanceEnabled(string Name)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string SetInstanceEnabled(string Name, bool enabled)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string RenameInstance(string name, string new_name)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string DetachInstance(string name)
		{
			throw new NotImplementedException();
		}
	}
}
