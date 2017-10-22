using System;
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
		public static readonly string Version = "/tg/station 13 Server Service v" + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

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
		/// The WCF host that the <see cref="Interface"/> connects to
		/// </summary>
		ServiceHost host;

		//you should seriously not add anything here
		//Use OnStart instead
		/// <summary>
		/// Construct and run a <see cref="Service"/>. Can only execute in the context of the Windows service manager
		/// </summary>
		public Service()
		{
			try
			{
				if (Properties.Settings.Default.UpgradeRequired)
				{
					var newVersion = Properties.Settings.Default.SettingsVersion;
					Properties.Settings.Default.Upgrade();
					var oldVersion = Properties.Settings.Default.SettingsVersion;
					Properties.Settings.Default.SettingsVersion = newVersion;

					MigrateSettings(oldVersion, newVersion);

					Properties.Settings.Default.UpgradeRequired = false;
					Properties.Settings.Default.Save();
				}
				InitializeComponent();
				ActiveService = this;
				Run(this);
			}
			finally
			{
				Properties.Settings.Default.Save();
			}
		}


		/// <summary>
		/// Migrates the <see cref="Properties.Settings"/> file from an older version
		/// </summary>
		/// <param name="oldVersion">The previous version</param>
		/// <param name="newVersion">The new version</param>
		void MigrateSettings(int oldVersion, int newVersion)
		{
			if (oldVersion == newVersion && newVersion == 0)	//chat refactor
				Properties.Settings.Default.ChatProviderData = "NEEDS INITIALIZING";	//reset chat settings to be safe
		}

		/// <summary>
		/// Called by the Windows service manager. Initializes and starts the <see cref="ServerInstance"/>
		/// </summary>
		/// <param name="args">Command line arguments for the <see cref="Service"/></param>
		protected override void OnStart(string[] args)
		{
			var Config = Properties.Settings.Default;
			if (!Directory.Exists(Config.ServerDirectory))
			{
				EventLog.WriteEntry("Creating server directory: " + Config.ServerDirectory);
				Directory.CreateDirectory(Config.ServerDirectory);
			}
			Environment.CurrentDirectory = Config.ServerDirectory;

			var instance = new ServerInstance();

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
					catch(Exception e)
					{
						throw new Exception("Invalid argument for \"-port\"", e);
					}
					Config.Save();
					break;
				}

			host = new ServiceHost(instance, new Uri[] { new Uri("net.pipe://localhost"), new Uri(String.Format("https://localhost:{0}", Config.RemoteAccessPort)) })
			{
				CloseTimeout = new TimeSpan(0, 0, 5)
			};

			foreach (var I in Interface.ValidInterfaces)
				AddEndpoint(I);
			
			host.Authorization.ServiceAuthorizationManager = instance;

			try
			{
				host.Open();
			}
			catch (AddressAlreadyInUseException e)
			{
				throw new Exception("Can't start the service due to the configured remote access port being in use. To fix this change it by starting the service with the \"-port <port>\" argument.", e);
			}
		}

		/// <summary>
		/// Adds a WCF endpoint for a component <paramref name="type"/>
		/// </summary>
		/// <param name="type">The <see cref="Type"/> of the component <see langword="interface"/></param>
		void AddEndpoint(Type type)
		{
			var bindingName = Interface.MasterInterfaceName + "/" + type.Name;
			host.AddServiceEndpoint(type, new NetNamedPipeBinding() { SendTimeout = new TimeSpan(0, 0, 30), MaxReceivedMessageSize = Interface.TransferLimitLocal }, bindingName);
			var httpsBinding = new WSHttpBinding()
			{
				SendTimeout = new TimeSpan(0, 0, 40),
				MaxReceivedMessageSize = Interface.TransferLimitRemote
			};
			var requireAuth = type.Name != typeof(ITGConnectivity).Name;
			httpsBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
			httpsBinding.Security.Mode = requireAuth ? SecurityMode.TransportWithMessageCredential : SecurityMode.Transport;	//do not require auth for a connectivity check
			httpsBinding.Security.Message.ClientCredentialType = requireAuth ? MessageCredentialType.UserName : MessageCredentialType.None;
			host.AddServiceEndpoint(type, httpsBinding, bindingName);
		}

		/// <summary>
		/// Shutsdown the WCF <see cref="host"/> and calls <see cref="IDisposable.Dispose"/> on it's <see cref="ServerInstance"/>
		/// </summary>
		protected override void OnStop()
		{
			try
			{
				var instance = (ServerInstance)host.SingletonInstance;
				host.Close();
				instance.Dispose();
			}
			catch (Exception e)
			{
				WriteError(e.ToString(), EventID.ServiceShutdownFail);
			}
		}
	}
}
