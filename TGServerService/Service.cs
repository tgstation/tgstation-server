using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceProcess;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGServerService
{//only deprecate events, do not reuse them

	sealed partial class Service : ServiceBase
	{ 
		static Service ActiveService;   //So everyone else can write to our eventlog

		public static readonly string Version = "/tg/station 13 Server Service v" + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

		/// <summary>
		/// You can't write to logs while impersonating, call this to cancel WCF's impersonation first
		/// </summary>
		public static void CancelImpersonation()
		{
			WindowsIdentity.Impersonate(IntPtr.Zero);
		}

		public static void WriteInfo(string message, EventID id)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Information, (int)id);
		}
		public static void WriteError(string message, EventID id)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Error, (int)id);
		}
		public static void WriteWarning(string message, EventID id)
		{
			ActiveService.EventLog.WriteEntry(message, EventLogEntryType.Warning, (int)id);
		}

		public static void WriteAccess(string username, bool authSuccess)
		{
			ActiveService.EventLog.WriteEntry(String.Format("Access from: {0}", username), authSuccess ? EventLogEntryType.SuccessAudit : EventLogEntryType.FailureAudit, (int)EventID.Authentication);
		}

		ServiceHost host;   //the WCF host

		void MigrateSettings(int oldVersion, int newVersion)
		{
			if (oldVersion == newVersion && newVersion == 0)	//chat refactor
				Properties.Settings.Default.ChatProviderData = "NEEDS INITIALIZING";	//reset chat settings to be safe
		}
	
		//you should seriously not add anything here
		//Use OnStart instead
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

		//when babby is formed
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

		//shorthand for adding the WCF endpoint
		void AddEndpoint(Type typetype)
		{
			var bindingName = Interface.MasterInterfaceName + "/" + typetype.Name;
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

		//when we is kill
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
