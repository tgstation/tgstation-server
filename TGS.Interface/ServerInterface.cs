using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using TGS.Interface.Components;

namespace TGS.Interface
{
	/// <summary>
	/// Main <see langword="interface"/> for communicating the <see cref="ITGSService"/>
	/// </summary>
	public interface IServerInterface : IDisposable
	{
		/// <summary>
		/// The <see cref="Version"/> of the connected <see cref="ITGSService"/>
		/// </summary>
		Version ServerVersion { get; }

		/// <summary>
		/// The name of the current instance in use. Defaults to <see langword="null"/>
		/// </summary>
		string InstanceName { get; }

		/// <summary>
		/// The <see cref="RemoteLoginInfo"/> for the <see cref="IServerInterface"/>. Is <see langword="null"/> for local connections
		/// </summary>
		RemoteLoginInfo LoginInfo { get; }

		/// <summary>
		/// Checks if the <see cref="IServerInterface"/> is setup for a remote connection
		/// </summary>
		bool IsRemoteConnection { get; }

		/// <summary>
		/// Targets <paramref name="instanceName"/> as the instance to use with <see cref="GetComponent{T}"/>. Closes all connections to any previous instance
		/// </summary>
		/// <param name="instanceName">The name of the instance to connect to</param>
		/// <param name="skipChecks">If set to <see langword="true"/>, skips the connectivity and authentication checks, sets <see cref="InstanceName"/>, and returns <see cref="ConnectivityLevel.Connected"/></param>
		/// <returns>The apporopriate <see cref="ConnectivityLevel"/></returns>
		ConnectivityLevel ConnectToInstance(string instanceName = null, bool skipChecks = false);

		/// <summary>
		/// Returns <see langword="true"/> if the <see cref="IServerInterface"/> interface being used to connect to a service does not have the same release version as the service
		/// </summary>
		/// <param name="errorMessage">An error message to display to the user should this function return <see langword="true"/></param>
		/// <returns><see langword="true"/> if the <see cref="IServerInterface"/> interface being used to connect to a service does not have the same release version as the service</returns>
		bool VersionMismatch(out string errorMessage);

		/// <summary>
		/// Returns the requested <see cref="IServerInterface"/> component <see langword="interface"/> for the instance <see cref="InstanceName"/>. This does not guarantee a successful connection.
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> to retrieve</typeparam>
		/// <returns>The correct component <see langword="interface"/></returns>
		T GetComponent<T>() where T : class;

		/// <summary>
		/// Returns a root service component
		/// </summary>
		/// <returns>The <see cref="ITGSService"/> component for the service</returns>
		T GetServiceComponent<T>() where T : class;

		/// <summary>
		/// Used to test if the <see cref="ITGSService"/> is avaiable on the target machine. Note that state can change at any time and any call into the may throw an exception because of communcation errors
		/// </summary>
		/// <returns><see langword="null"/> on successful connection, error message <see cref="string"/> on failure</returns>
		ConnectivityLevel ConnectionStatus();

		/// <summary>
		/// Used to test if the <see cref="ITGSService"/> is avaiable on the target machine. Note that state can change at any time and any call into the may throw an exception because of communcation errors
		/// </summary>
		/// <param name="error">String of the error that prevented an elevated connectivity level</param>
		/// <returns>The apporopriate <see cref="ConnectivityLevel"/></returns>
		ConnectivityLevel ConnectionStatus(out string error);
	}

	/// <inheritdoc />
	sealed public class ServerInterface : IServerInterface
	{
		/// <summary>
		/// List of <see langword="interface"/>s that can be used with <see cref="GetServiceComponent{T}"/>
		/// </summary>
		public static readonly IList<Type> ValidServiceInterfaces = new List<Type> { typeof(ITGSService), typeof(ITGInstanceManager), typeof(ITGConnectivity), typeof(ITGLanding) };

		/// <summary>
		/// Version of the interface
		/// </summary>
		public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;

		/// <summary>
		/// List of <see langword="interface"/>s that can be used with <see cref="GetComponent{T}"/>
		/// </summary>
		public static readonly IList<Type> ValidInstanceInterfaces = CollectComponents();

		/// <summary>
		/// The maximum message size to and from a local server 
		/// </summary>
		public const long TransferLimitLocal = Int32.MaxValue;   //2GB can't go higher

		/// <summary>
		/// The maximum message size to and from a remote server
		/// </summary>
		public const long TransferLimitRemote = 10485760;   //10 MB

		/// <summary>
		/// Base name of communication URLs
		/// </summary>
		public const string MasterInterfaceName = "TGStationServerService";
		/// <summary>
		/// Base name of instance URLs
		/// </summary>
		public const string InstanceInterfaceName = MasterInterfaceName + "/Instance";

		/// <summary>
		/// The <see cref="ServerVersion"/>
		/// </summary>
		Version _serverVersion;

		/// <inheritdoc />
		public Version ServerVersion { get
			{
				lock (this)
					if (_serverVersion == null)
					{
						string rawVersion;
						//check ITGSService first for compatiblity reasons
						try
						{
							rawVersion = GetServiceComponent<ITGSService>().Version();
						}
						catch
						{
							rawVersion = GetServiceComponent<ITGLanding>().Version();
						}
						var splits = rawVersion.Split(' ');
						_serverVersion = new Version(splits[splits.Length - 1].Substring(1));
					}
				return _serverVersion;
			} }

		/// <inheritdoc />
		public string InstanceName { get; private set; }

		/// <summary>
		/// Backing field for <see cref="LoginInfo"/>
		/// </summary>
		readonly RemoteLoginInfo _loginInfo;

		/// <inheritdoc />
		public RemoteLoginInfo LoginInfo { get { return _loginInfo; } }

		/// <summary>
		/// Associated list of open <see cref="ChannelFactory"/>s keyed by <see langword="interface"/> type name. A <see cref="ChannelFactory"/> in this list may close or fault at any time. Must be locked before being accessed
		/// </summary>
		IDictionary<string, ChannelFactory> ChannelFactoryCache = new Dictionary<string, ChannelFactory>();

		/// <summary>
		/// Returns a <see cref="IList{T}"/> of <see langword="interface"/> <see cref="Type"/>s that can be used with the service
		/// </summary>
		/// <returns>A <see cref="IList{T}"/> of <see langword="interface"/> <see cref="Type"/>s that can be used with the service</returns>
		static IList<Type> CollectComponents()
		{
			var ConnectivityComponent = typeof(ITGConnectivity);
																   //find all interfaces in this assembly in this namespace that have the service contract attribute
			var query = from t in Assembly.GetExecutingAssembly().GetTypes()
						where t.IsInterface
						&& t.Namespace == ConnectivityComponent.Namespace
						&& t.GetCustomAttribute(typeof(ServiceContractAttribute)) != null
						&& (t == ConnectivityComponent || !ValidServiceInterfaces.Contains(t))
						select t;
			return query.ToList();
		}

		/// <summary>
		/// Sets the function called when a remote login fails due to the server having an invalid SSL cert
		/// </summary>
		/// <param name="handler">The <see cref="Func{T, TResult}"/> to be called when a remote login is attempted while the server posesses a bad certificate. Passed a <see cref="string"/> of error information about the and should return <see langword="true"/> if it the connection should be made anyway</param>
		public static void SetBadCertificateHandler(Func<string, bool> handler)
		{
			ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, error) =>
			{
				string ErrorMessage = "This doesn't seem physically possible!";
				switch (error)
				{
					case SslPolicyErrors.None:
						return true;
					case SslPolicyErrors.RemoteCertificateChainErrors:
						ErrorMessage = "There are certificate chain errors.";
						break;
					case SslPolicyErrors.RemoteCertificateNameMismatch:
						ErrorMessage = "The certificate name does not match.";
						break;
					case SslPolicyErrors.RemoteCertificateNotAvailable:
						ErrorMessage = "The certificate doesn't exist in the trust store.";
						break;
				}
				ErrorMessage = String.Format("The server's certificate failed to verify! Error: {0} Cert: {1}", ErrorMessage, cert.ToString());
				return handler(ErrorMessage);
			};
		}

		/// <summary>
		/// Construct an <see cref="ServerInterface"/> for a local connection
		/// </summary>
		public ServerInterface() { }

		/// <summary>
		/// Construct an <see cref="ServerInterface"/> for a remote connection
		/// </summary>
		/// <param name="loginInfo">The <see cref="RemoteLoginInfo"/> for a remote connection</param>
		public ServerInterface(RemoteLoginInfo loginInfo)
		{
			if (!loginInfo.HasPassword)
				throw new InvalidOperationException("password must be set on loginInfo!");
			_loginInfo = loginInfo;
		}

		/// <inheritdoc />
		public ConnectivityLevel ConnectToInstance(string instanceName = null, bool skipChecks = false)
		{
			if (instanceName == null)
				instanceName = InstanceName;
			if (!skipChecks && !ConnectionStatus().HasFlag(ConnectivityLevel.Connected))
				return ConnectivityLevel.None;
			var prevInstance = InstanceName;
			if (prevInstance != instanceName)
				CloseAllChannels(false);
			InstanceName = instanceName;
			if (skipChecks)
				return ConnectivityLevel.Connected;
			try
			{
				GetComponent<ITGConnectivity>().VerifyConnection();
			}
			catch
			{
				InstanceName = prevInstance;
				return ConnectivityLevel.None;
			}
			try
			{
				GetComponent<ITGInstance>().ServerDirectory();
			}
			catch
			{
				return ConnectivityLevel.Connected;
			}
			try
			{
				GetComponent<ITGAdministration>().GetCurrentAuthorizedGroup();
				return ConnectivityLevel.Administrator;
			}
			catch
			{
				return ConnectivityLevel.Authenticated;
			}
		}

		/// <inheritdoc />
		public bool IsRemoteConnection { get { return LoginInfo != null; } }

		/// <summary>
		/// Closes all <see cref="ChannelFactory"/>s stored in <see cref="ChannelFactoryCache"/> and clears it
		/// </summary>
		/// <param name="includingRoot">If set to <see langword="false"/>, doesn't clear the channels that are used by <see cref="ITGSService"/></param>
		void CloseAllChannels(bool includingRoot)
		{
			string[] RootThings = { typeof(ITGSService).Name, 'S' + typeof(ITGConnectivity).Name };
			lock (ChannelFactoryCache)
			{
				var toRemove = new List<string>();
				foreach (var I in ChannelFactoryCache)
				{
					if (RootThings.Contains(I.Key))
						continue;
					var cf = I.Value;
					try
					{
						cf.Close();
					}
					catch
					{
						cf.Abort();
					}
					toRemove.Add(I.Key);
				}
				foreach (var I in toRemove)
					ChannelFactoryCache.Remove(I);
			}
		}

		/// <inheritdoc />
		public bool VersionMismatch(out string errorMessage)
		{
			if(ServerVersion.Major != Version.Major || ServerVersion.Minor != Version.Minor || ServerVersion.Build != Version.Build)	//don't care about the patch level
			{
				errorMessage = String.Format("Version mismatch between interface version ({0}) and service version ({1}). Some functionality may crash this program.", Version, ServerVersion);
				return true;
			}
			errorMessage = null;
			return false;
		}

		/// <inheritdoc />
		public T GetComponent<T>() where T : class
		{
			var ToT = typeof(T);
			if (!ValidInstanceInterfaces.Contains(ToT))
				throw new Exception("Invalid type!");
			return GetComponentImpl<T>(true);
		}

		/// <summary>
		/// Returns the requested <see cref="ServerInterface"/> component <see langword="interface"/> for the instance <see cref="InstanceName"/>. This does not guarantee a successful connection. <see cref="ChannelFactory{TChannel}"/>s created this way are recycled for minimum latency and bandwidth usage
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> to retrieve</typeparam>
		/// <param name="useInstanceName">If <see cref="InstanceName"/> should be used to connect</param>
		/// <returns>The correct component <see langword="interface"/></returns>
		T GetComponentImpl<T>(bool useInstanceName) where T : class
		{
			if (useInstanceName & InstanceName == null)
				throw new Exception("Instance not selected!");
			var actualToT = typeof(T);
			var tot = actualToT.Name;
			if (actualToT == typeof(ITGConnectivity) && !useInstanceName)
				tot = 'S' + tot; 
			ChannelFactory<T> cf;

			lock (ChannelFactoryCache)
			{
				if (ChannelFactoryCache.ContainsKey(tot))
					try
					{
						cf = ((ChannelFactory<T>)ChannelFactoryCache[tot]);
						if (cf.State != CommunicationState.Opened)
							throw new Exception();
						return cf.CreateChannel();
					}
					catch
					{
						ChannelFactoryCache[tot].Abort();
						ChannelFactoryCache.Remove(tot);
					}
				cf = CreateChannel<T>(useInstanceName ? InstanceName : null);
				ChannelFactoryCache[tot] = cf;
			}
			return cf.CreateChannel();
		}

		/// <inheritdoc />
		public T GetServiceComponent<T>() where T : class
		{
			var ToT = typeof(T);
			if (!ValidServiceInterfaces.Contains(ToT))
				throw new Exception("Invalid type!");
			return GetComponentImpl<T>(false);
		}

		/// <summary>
		/// Directly creates a <see cref="ChannelFactory{TChannel}"/> for <typeparamref name="T"/> without caching. This should be eventually closed by the caller
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> of the channel to be created</typeparam>
		/// <param name="instanceName">The instance to connect to, if any</param>
		/// <returns>The correct <see cref="ChannelFactory{TChannel}"/></returns>
		ChannelFactory<T> CreateChannel<T>(string instanceName)
		{
			var accessPath = instanceName == null ? MasterInterfaceName : String.Format("{0}/{1}", InstanceInterfaceName, instanceName);
			if (!IsRemoteConnection)
				return CreateLocalChannel<T>(instanceName, accessPath);
			return CreateRemoteChannel<T>(instanceName, accessPath);
		}

		//NOTE: This needs to be kept seperate from CreateRemoteChannel because not all our client implementations have NetNamedPipeBinding and attempting to call this function on those platforms will throw an exception

		/// <summary>
		/// Directly creates a <see cref="ChannelFactory{TChannel}"/> for on a local connection <typeparamref name="T"/> without caching. This should be eventually closed by the caller
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> of the channel to be created</typeparam>
		/// <param name="instanceName">The instance to connect to, if any</param>
		/// <param name="accessPath">The URL of the interface to connect to</param>
		/// <returns>The correct <see cref="ChannelFactory{TChannel}"/></returns>
		ChannelFactory<T> CreateLocalChannel<T>(string instanceName, string accessPath)
		{
			var interfaceName = typeof(T).Name;
			var res2 = new ChannelFactory<T>(
			new NetNamedPipeBinding { SendTimeout = new TimeSpan(0, 0, 30), MaxReceivedMessageSize = TransferLimitLocal }, new EndpointAddress(String.Format("net.pipe://localhost/{0}/{1}", accessPath, interfaceName)));                                                      //10 megs
			res2.Credentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Impersonation;
			return res2;
		}

		/// <summary>
		/// Directly creates a <see cref="ChannelFactory{TChannel}"/> for on a remote connection <typeparamref name="T"/> without caching. This should be eventually closed by the caller
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> of the channel to be created</typeparam>
		/// <param name="instanceName">The instance to connect to, if any</param>
		/// <param name="accessPath">The URL of the interface to connect to</param>
		/// <returns>The correct <see cref="ChannelFactory{TChannel}"/></returns>
		ChannelFactory<T> CreateRemoteChannel<T>(string instanceName, string accessPath)
		{
			//okay we're going over
			var binding = new BasicHttpsBinding()
			{
				SendTimeout = new TimeSpan(0, 0, 40),
				MaxReceivedMessageSize = TransferLimitRemote
			};

			var interfaceName = typeof(T).Name;
			var requireAuth = interfaceName != typeof(ITGConnectivity).Name;
			var url = String.Format("https://{0}:{1}/{2}/{3}", LoginInfo.IP, LoginInfo.Port, accessPath, interfaceName);
			var address = new EndpointAddress(url);
			var res = new ChannelFactory<T>(binding, address);
			if (requireAuth)
			{
				var applicator = new AuthenticationHeaderApplicator(LoginInfo);
				var t = Type.GetType("Mono.Runtime");
				KeyedCollection<Type, IEndpointBehavior> behaviours;
				if (t != null)
				{
					var prop = res.Endpoint.GetType()
					 .GetTypeInfo()
					 .GetDeclaredProperty("Behaviors");
					behaviours = (KeyedCollection<Type, IEndpointBehavior>)prop
									 .GetValue(res.Endpoint);
				}
				else
					behaviours = res.Endpoint.EndpointBehaviors;
				behaviours.Add(applicator);
			}
			return res;
		}

		/// <inheritdoc />
		public ConnectivityLevel ConnectionStatus()
		{
			return ConnectionStatus(out string unused);
		}

		/// <inheritdoc />
		public ConnectivityLevel ConnectionStatus(out string error)
		{
			try
			{
				GetComponentImpl<ITGConnectivity>(false).VerifyConnection();
			}
			catch (Exception e)
			{
				error = e.ToString();
				return ConnectivityLevel.None;
			}
			try
			{
				GetServiceComponent<ITGLanding>().Version();
			}
			catch(Exception e)
			{
				error = e.ToString();
				return ConnectivityLevel.Connected;
			}
			try
			{
				GetServiceComponent<ITGSService>().Version();
				error = null;
				return ConnectivityLevel.Administrator;
			}
			catch(Exception e)
			{
				error = e.ToString();
				return ConnectivityLevel.Authenticated;
			}
		}

#region IDisposable Support
		/// <summary>
		/// To detect redundant <see cref="Dispose(bool)"/> calls
		/// </summary>
		private bool disposedValue = false;

		/// <summary>
		/// Implements the <see cref="IDisposable"/> pattern. Calls <see cref="CloseAllChannels"/>
		/// </summary>
		/// <param name="disposing"><see langword="true"/> if <see cref="Dispose()"/> was called manually, <see langword="false"/> if it was from the finalizer</param>
		void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					CloseAllChannels(true);
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~Interface() {
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
