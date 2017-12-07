﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;
using TGS.Interface;
using TGS.Interface.Components;
using TGS.Interface.Proxying;
using TGS.Server.Security;
using TGS.Server.Proxying;

namespace TGS.Server
{
	/// <summary>
	/// The windows service the application runs as
	/// </summary>
	public sealed class Server : ITGServer, ITGLanding, ITGInstanceManager, IDisposable
	{
		/// <summary>
		/// The logging ID used for <see cref="Server"/> events
		/// </summary>
		public const byte LoggingID = 0;

		/// <summary>
		/// The directory to use when importing a .NET settings based config
		/// </summary>
		public const string MigrationConfigDirectory = "C:\\TGSSettingUpgradeTempDir";

		public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;
		/// <summary>
		/// The service version <see cref="string"/> based on the <see cref="AssemblyName"/>'s <see cref="System.Version"/>
		/// </summary>
		public static readonly string VersionString = String.Format("/tg/station 13 Server v{0}", Version);

		/// <summary>
		/// Singleton <see cref="ILogger"/>
		/// </summary>
		public static ILogger Logger { get; private set; }

		/// <summary>
		/// The <see cref="ServerConfig"/> for the <see cref="Server"/>
		/// </summary>
		public static ServerConfig Config { get; private set; }

		/// <summary>
		/// The directory to load and save <see cref="ServerConfig"/>s to
		/// </summary>
		static readonly string DefaultConfigDirectory = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TGS.Server")).FullName;

		/// <summary>
		/// Checks an <paramref name="instanceName"/> for illegal characters
		/// </summary>
		/// <param name="instanceName">The <see cref="Instance"/> name to check</param>
		/// <returns><see langword="null"/> if <paramref name="instanceName"/> contains no illegal characters, error message otherwise</returns>
		static string CheckInstanceName(string instanceName)
		{
			char[] bannedCharacters = { ';', '&', '=', '%' };
			foreach (var I in bannedCharacters)
				if (instanceName.Contains(I.ToString()))
					return "Instance names may not contain the following characters: ';', '&', '=', or '%'";
			return null;
		}

		/// <summary>
		/// The WCF host that contains <see cref="ITGSService"/> connects to
		/// </summary>
		ServiceHost serviceHost;
		/// <summary>
		/// Map of <see cref="InstanceConfig.Name"/> to the respective <see cref="ServiceHost"/> hosting the <see cref="Instance"/>
		/// </summary>
		IDictionary<string, ServiceHost> hosts;
		/// <summary>
		/// List of <see cref="Instance.LoggingID"/>s in use
		/// </summary>
		IList<int> UsedLoggingIDs = new List<int>();

		/// <summary>
		/// Construct a <see cref="Server"/>
		/// </summary>
		/// <param name="args">Command line arguments for the <see cref="Server"/></param>
		/// <param name="logger">The <see cref="ILogger"/> to use</param>
		public Server(string[] args, ILogger logger)
		{
			Logger = logger;

			Environment.CurrentDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name)).FullName;  //MOVE THIS POINTER BECAUSE ONE TIME I ALMOST ACCIDENTALLY NUKED MYSELF BY REFACTORING! http://imgur.com/zvGEpJD.png

			SetupConfig();

			ChangePortFromCommandLine(args);

			SetupService();

			SetupInstances();

			OnlineAllHosts();
		}

		/// <summary>
		/// Enumerates configured <see cref="IInstanceConfig"/>s. Detaches those that fail to load
		/// </summary>
		/// <returns>Each configured <see cref="IInstanceConfig"/></returns>
		IEnumerable<IInstanceConfig> GetInstanceConfigs()
		{
			var pathsToRemove = new List<string>();
			lock (this)
			{
				var IPS = Config.InstancePaths;
				foreach (var I in IPS)
				{
					IInstanceConfig ic;
					try
					{
						ic = InstanceConfig.Load(I);
					}
					catch (Exception e)
					{
						Logger.WriteError(String.Format("Unable load instance config at path {0}. Error: {1} Detaching...", I, e.ToString()), EventID.InstanceInitializationFailure, LoggingID);
						pathsToRemove.Add(I);
						continue;
					}
					yield return ic;
				}
				foreach (var I in pathsToRemove)
					IPS.Remove(I);
			}
		}

		/// <summary>
		/// Overrides and saves the configured <see cref="ServerConfig.RemoteAccessPort"/> if requested by command line parameters
		/// </summary>
		/// <param name="args">The command line parameters for the <see cref="Server"/></param>
		void ChangePortFromCommandLine(string[] args)
		{
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
					Config.Save(DefaultConfigDirectory);
					break;
				}
		}

		/// <summary>
		/// Upgrades up the service configuration
		/// </summary>
		void SetupConfig()
		{
			Directory.CreateDirectory(DefaultConfigDirectory);
			try
			{
				Config = ServerConfig.Load(DefaultConfigDirectory);
			}
			catch
			{
				try
				{
					//assume we're upgrading
					Config = ServerConfig.Load(MigrationConfigDirectory);
					Config.Save(DefaultConfigDirectory);
					Helpers.DeleteDirectory(MigrationConfigDirectory);
				}
				catch
				{
					//new baby
					Config = new ServerConfig();
				}
			}
		}

		/// <summary>
		/// Creates the <see cref="ServiceHost"/> for <see cref="ITGSService"/>
		/// </summary>
		void SetupService()
		{
			serviceHost = CreateHost(this, Definitions.MasterInterfaceName, new RootAuthorizationManager());
			//foreach (var I in ServerInterface.ValidServiceInterfaces)
			//AddEndpoint(serviceHost, I);
			AddEndpoint(serviceHost, typeof(ITGRequestManager));
		}

		/// <summary>
		/// Opens all created <see cref="ServiceHost"/>s
		/// </summary>
		void OnlineAllHosts()
		{
			serviceHost.Open();
			foreach (var I in hosts)
				I.Value.Open();
		}

		/// <summary>
		/// Creates a <see cref="ServiceHost"/> for <paramref name="singleton"/> using the default pipe, CloseTimeout for the <see cref="ServiceHost"/>, and the configured <see cref="ServerConfig.RemoteAccessPort"/>
		/// </summary>
		/// <param name="singleton">The <see cref="ServiceHost.SingletonInstance"/></param>
		/// <param name="endpointPostfix">The URL to access components on the <see cref="ServiceHost"/></param>
		/// <param name="authorizationManager">The <see cref="IAuthorizationManager"/> for the <see cref="RequestManager"/></param>
		/// <returns>The created <see cref="ServiceHost"/></returns>
		static ServiceHost CreateHost(object singleton, string endpointPostfix, IAuthorizationManager authorizationManager)
		{
			return new ServiceHost(new RequestManager(singleton, authorizationManager), new Uri[] { new Uri(String.Format("net.pipe://localhost/{0}", endpointPostfix)), new Uri(String.Format("https://localhost:{0}/{1}", Config.RemoteAccessPort, endpointPostfix)) })
			{
				CloseTimeout = new TimeSpan(0, 0, 5)
			};
		}

		/// <summary>
		/// Creates <see cref="ServiceHost"/>s for all <see cref="Instance"/>s as listed in <see cref="ServerConfig.InstancePaths"/>, detaches bad ones
		/// </summary>
		void SetupInstances()
		{
			hosts = new Dictionary<string, ServiceHost>();
			var pathsToRemove = new List<string>();
			var seenNames = new List<string>();
			foreach (var I in GetInstanceConfigs())
			{
				if (seenNames.Contains(I.Name))
				{
					Logger.WriteError(String.Format("Instance at {0} has a duplicate name! Detaching...", I.Directory), EventID.InstanceInitializationFailure, LoggingID);
					pathsToRemove.Add(I.Directory);
				}
				if (I.Enabled && SetupInstance(I) == null)
					pathsToRemove.Add(I.Directory);
				else
					seenNames.Add(I.Name);
			}
			foreach (var I in pathsToRemove)
				Config.InstancePaths.Remove(I);
		}

		/// <summary>
		/// Unlocks a <see cref="Instance.LoggingID"/> acquired with <see cref="LockLoggingID"/>
		/// </summary>
		/// <param name="ID">The <see cref="Instance.LoggingID"/> to unlock</param>
		void UnlockLoggingID(byte ID)
		{
			lock (UsedLoggingIDs)
			{
				UsedLoggingIDs.Remove(ID);
			}
		}

		/// <summary>
		/// Gets and locks a <see cref="Instance.LoggingID"/>
		/// </summary>
		/// <returns>A logging ID for the <see cref="Instance"/> must be released using <see cref="UnlockLoggingID(byte)"/></returns>
		byte LockLoggingID()
		{
			lock (UsedLoggingIDs)
			{
				for (byte I = 1; I < 100; ++I)
					if (!UsedLoggingIDs.Contains(I))
					{
						UsedLoggingIDs.Add(I);
						return I;
					}
			}
			throw new Exception("All logging IDs in use!");
		}

		/// <summary>
		/// Creates and starts a <see cref="ServiceHost"/> for a <see cref="Instance"/> at <paramref name="config"/>
		/// </summary>
		/// <param name="config">The <see cref="IInstanceConfig"/> for the <see cref="Instance"/></param>
		/// <returns>The inactive <see cref="ServiceHost"/> on success, <see langword="null"/> on failure</returns>
		ServiceHost SetupInstance(IInstanceConfig config)
		{
			Instance instance;
			string instanceName;
			try
			{
				if (hosts.ContainsKey(config.Directory))
				{
					var datInstance = Helpers.ServiceHostToInstance(hosts[config.Directory]);
					Logger.WriteError(String.Format("Unable to start instance at path {0}. Has the same name as instance at path {1}. Detaching...", config.Directory, datInstance.ServerDirectory()), EventID.InstanceInitializationFailure, LoggingID);
					return null;
				}
				if (!config.Enabled)
					return null;
				var ID = LockLoggingID();
				Logger.WriteInfo(String.Format("Instance {0} ({1}) assigned logging ID {2}", config.Name, config.Directory, ID), EventID.InstanceIDAssigned, ID);
				instanceName = config.Name;
				instance = new Instance(config, ID);
			}
			catch (Exception e)
			{
				Logger.WriteError(String.Format("Unable to start instance at path {0}. Detaching... Error: {1}", config.Directory, e.ToString()), EventID.InstanceInitializationFailure, LoggingID);
				return null;
			}

			var host = CreateHost(instance, String.Format("{0}/{1}", Definitions.InstanceInterfaceName, instanceName), instance);
			hosts.Add(instanceName, host);
			
			AddEndpoint(host, typeof(ITGRequestManager));

			return host;
		}

		/// <summary>
		/// Adds a WCF endpoint for a component <paramref name="typetype"/>
		/// </summary>
		/// <param name="host">The service host to add the component to</param>
		/// <param name="typetype">The type of the component</param>
		void AddEndpoint(ServiceHost host, Type typetype)
		{
			var bindingName = typetype.Name;
			host.AddServiceEndpoint(typetype, new NetNamedPipeBinding() { SendTimeout = new TimeSpan(0, 0, 30), MaxReceivedMessageSize = Definitions.TransferLimitLocal }, bindingName);
			var httpsBinding = new BasicHttpsBinding()
			{
				SendTimeout = new TimeSpan(0, 0, 40),
				MaxReceivedMessageSize = Definitions.TransferLimitRemote
			};
			host.AddServiceEndpoint(typetype, httpsBinding, bindingName);
		}

		/// <summary>
		/// Shuts down all active <see cref="ServiceHost"/>s and calls <see cref="IDisposable.Dispose"/> on it's <see cref="Instance"/>
		/// </summary>
		void OnStop()
		{
			lock (this)
			{
				try
				{
					foreach (var I in hosts)
					{
						var host = I.Value;
						var instance = Helpers.ServiceHostToInstance(host);
						host.Close();
						instance.Dispose();
						UnlockLoggingID(instance.LoggingID);
					}
				}
				catch (Exception e)
				{
					Logger.WriteError(e.ToString(), EventID.ServiceShutdownFail, LoggingID);
				}
				serviceHost.Close();
			}
			Config.Save(DefaultConfigDirectory);
		}

		/// <inheritdoc />
		public Task PrepareForUpdate()
		{
			return Task.Run(() =>
			{
				foreach (var I in hosts)
					Helpers.ServiceHostToInstance(I.Value).Reattach(false);
			});
		}

		/// <inheritdoc />
		public Task<ushort> RemoteAccessPort()
		{
			return Task.Run(() => Config.RemoteAccessPort);
		}

		/// <inheritdoc />
		public Task<string> SetRemoteAccessPort(ushort port)
		{
			return Task.Run(() =>
			{
				if (port == 0)
					return "Cannot bind to port 0";
				Config.RemoteAccessPort = port;
				return null;
			});
		}

		/// <inheritdoc />
		public Task<bool> SetPythonPath(string path)
		{
			return Task.Run(() =>
			{
				if (!Directory.Exists(path))
					return false;
				Config.PythonPath = Path.GetFullPath(path);
				return true;
			});
		}

		/// <inheritdoc />
		public Task<string> PythonPath()
		{
			return Task.Run(() => Config.PythonPath);
		}

		/// <inheritdoc />
		public Task<IList<InstanceMetadata>> ListInstances()
		{
			return Task.Run(() =>
			{
				IList<InstanceMetadata> result = new List<InstanceMetadata>();
				lock (this)
					foreach (var ic in GetInstanceConfigs())
						result.Add(new InstanceMetadata
						{
							Name = ic.Name,
							Path = ic.Directory,
							Enabled = ic.Enabled,
							LoggingID = (byte)(ic.Enabled ? Helpers.ServiceHostToInstance(hosts[ic.Name]).LoggingID : 0)
						});
				return result;
			});
		}

		/// <inheritdoc />
		public Task<string> CreateInstance(string Name, string path)
		{
			return Task.Run(() =>
			{
				path = Helpers.NormalizePath(path);
				var res = CheckInstanceName(Name);
				if (res != null)
					return res;
				if (File.Exists(path) || Directory.Exists(path))
					return "Cannot create instance at pre-existing path!";
				lock (this)
				{
					if (Config.InstancePaths.Contains(path))
						return String.Format("Instance at {0} already exists!", path);
					foreach (var oic in GetInstanceConfigs())
						if (Name == oic.Name)
							return String.Format("Instance named {0} already exists!", oic.Name);
					IInstanceConfig ic;
					try
					{
						ic = new InstanceConfig(path)
						{
							Name = Name
						};
						Directory.CreateDirectory(path);
						ic.Save();
						Config.InstancePaths.Add(path);
					}
					catch (Exception e)
					{
						return e.ToString();
					}
					return SetupOneInstance(ic).Result;
				}
			});
		}
			

		/// <summary>
		/// Starts and onlines an instance located at <paramref name="config"/>
		/// </summary>
		/// <param name="config">The <see cref="IInstanceConfig"/> for the <see cref="Instance"/></param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		Task<string> SetupOneInstance(IInstanceConfig config)
		{
			return Task.Run(() =>
			{
				if (!config.Enabled)
					return null;
				try
				{
					var host = SetupInstance(config);
					if (host != null)
						host.Open();
					else
						lock (this)
							Config.InstancePaths.Remove(config.Directory);
					return null;
				}
				catch (Exception e)
				{
					return "Instance set up but an error occurred while starting it: " + e.ToString();
				}
			});
		}

		/// <inheritdoc />
		public Task<string> ImportInstance(string path)
		{
			return Task.Run(() =>
			{
				path = Helpers.NormalizePath(path);
				lock (this)
				{
					if (Config.InstancePaths.Contains(path))
						return String.Format("Instance at {0} already exists!", path);
					if (!Directory.Exists(path))
						return String.Format("There is no instance located at {0}!", path);
					IInstanceConfig ic;
					try
					{
						ic = InstanceConfig.Load(path);
						foreach (var oic in GetInstanceConfigs())
							if (ic.Name == oic.Name)
								return String.Format("Instance named {0} already exists!", oic.Name);
						ic.Save();
						Config.InstancePaths.Add(path);
					}
					catch (Exception e)
					{
						return e.ToString();
					}
					return SetupOneInstance(ic).Result;
				}
			});
		}

		/// <inheritdoc />
		public Task<bool> InstanceEnabled(string Name)
		{
			return Task.Run(() =>
			{
				lock (this)
				{
					return hosts.ContainsKey(Name);
				}
			});
		}

		/// <inheritdoc />
		public Task<string> SetInstanceEnabled(string Name, bool enabled)
		{
			return Task.Run(() => SetInstanceEnabledImpl(Name, enabled, out string path));
		}

		/// <summary>
		/// Sets a <see cref="Instance"/>'s enabled status
		/// </summary>
		/// <param name="Name">The <see cref="Instance"/> whom's status should be changed</param>
		/// <param name="enabled"><see langword="true"/> to enable the <see cref="Instance"/>, <see langword="false"/> to disable it</param>
		/// <param name="path">The path to the modified <see cref="Instance"/></param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string SetInstanceEnabledImpl(string Name, bool enabled, out string path)
		{
			path = null;
			lock (this)
			{
				var hostIsOnline = hosts.ContainsKey(Name);
				if (enabled)
				{
					if (hostIsOnline)
						return null;
					foreach (var ic in GetInstanceConfigs())
						if (ic.Name == Name)
						{
							path = ic.Directory;
							ic.Enabled = true;
							ic.Save();
							return SetupOneInstance(ic).Result;
						}
					return String.Format("Instance {0} does not exist!", Name);
				}
				else
				{
					if (!hostIsOnline)
						return null;
					var host = hosts[Name];
					hosts.Remove(Name);
					var inst = Helpers.ServiceHostToInstance(host);
					host.Close();
					path = inst.ServerDirectory().Result;
					inst.Offline();
					inst.Dispose();
					UnlockLoggingID(inst.LoggingID);
					return null;
				}
			}
		}

		/// <inheritdoc />
		public Task<string> RenameInstance(string name, string new_name)
		{
			return Task.Run(() =>
			{
				if (name == new_name)
					return null;
				var res = CheckInstanceName(new_name);
				if (res != null)
					return res;
				lock (this)
				{
					//we have to check em all anyway
					IInstanceConfig the_droid_were_looking_for = null;
					foreach (var ic in GetInstanceConfigs())
						if (ic.Name == name)
						{
							the_droid_were_looking_for = ic;
							break;
						}
						else if (ic.Name == new_name)
							return String.Format("There is already another instance named {0}!", new_name);
					if (the_droid_were_looking_for == null)
						return String.Format("There is no instance named {0}!", name);
					var ie = InstanceEnabled(name).Result;
					if (ie)
						SetInstanceEnabled(name, false);
					the_droid_were_looking_for.Name = new_name;
					string result = "";
					try
					{
						the_droid_were_looking_for.Save();
						result = null;
					}
					catch (Exception e)
					{
						result = "Could not save instance config! Error: " + e.ToString();
					}
					finally
					{
						if (ie)
						{
							var resRestore = SetInstanceEnabled(new_name, true);
							if (resRestore != null)
								result = (result + " " + resRestore).Trim();
						}
					}
					return result;
				}
			});
		}

		/// <inheritdoc />
		public Task<string> DetachInstance(string name)
		{
			return Task.Run(() =>
			{
				lock (this)
				{
					var res = SetInstanceEnabledImpl(name, false, out string path);
					if (res != null)
						return res;
					if (path == null)    //gotta find it ourselves
						foreach (var ic in GetInstanceConfigs())
							if (ic.Name == name)
							{
								path = ic.Directory;
								break;
							}
					if (path == null)
						return String.Format("No instance named {0} exists!", name);
					Config.InstancePaths.Remove(path);
					return null;
				}
			});
		}

		#region IDisposable Support
		/// <summary>
		/// To detect redundant <see cref="Dispose()"/> calls
		/// </summary>
		private bool disposedValue = false;

		/// <summary>
		/// Implements the <see cref="IDisposable"/> pattern. Calls <see cref="OnStop"/>
		/// </summary>
		/// <param name="disposing"><see langword="true"/> if <see cref="Dispose()"/> was called manually, <see langword="false"/> if it was from the finalizer</param>
		void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					OnStop();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~Server() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

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
