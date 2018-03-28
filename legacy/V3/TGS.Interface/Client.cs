using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using TGS.Interface.Components;

namespace TGS.Interface
{
	/// <inheritdoc />
	sealed public class Client : IClient
	{
		/// <summary>
		/// Version of the <see cref="Client"/>
		/// </summary>
		public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;

		/// <inheritdoc />
		public IServer Server => server;

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
							rawVersion = GetComponent<ITGSService>(null).Version();
						}
						catch
						{
							rawVersion = GetComponent<ITGLanding>(null).Version();
						}
						var splits = rawVersion.Split(' ');
						_serverVersion = new Version(splits[splits.Length - 1].Substring(1));
					}
				return _serverVersion;
			} }

		/// <inheritdoc />
		public string InstanceName { get; private set; }

		/// <inheritdoc />
		public RemoteLoginInfo LoginInfo { get { return _loginInfo; } }

		/// <summary>
		/// Backing field for <see cref="Server"/>
		/// </summary>
		readonly IServer server;

		/// <summary>
		/// Backing field for <see cref="LoginInfo"/>
		/// </summary>
		readonly RemoteLoginInfo _loginInfo;

		/// <summary>
		/// The <see cref="ServerVersion"/>
		/// </summary>
		Version _serverVersion;

		/// <summary>
		/// Associated list of open <see cref="ChannelFactory"/>s keyed by <see langword="interface"/> type name. A <see cref="ChannelFactory"/> in this list may close or fault at any time. <see langword="this"/> must be locked before being accessed
		/// </summary>
		IDictionary<string, ChannelFactory> ChannelFactoryCache;

		/// <summary>
		/// Sets the function called when a remote login fails due to the server having an invalid SSL cert
		/// </summary>
		/// <param name="handler">The <see cref="Func{T, TResult}"/> to be called when a remote login is attempted while the server posesses a bad certificate. Passed a <see cref="string"/> of error information about the and should return <see langword="true"/> if it the connection should be made anyway</param>
		public static void SetBadCertificateHandler(Func<string, bool> handler)
		{
			ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, error) =>
			{
				string ErrorMessage;
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
					default:
						ErrorMessage = "An unknown error occurred.";
						break;
				}
				ErrorMessage = String.Format("The server's certificate failed to verify! Error: {0} Cert: {1}", ErrorMessage, cert.ToString());
				return handler(ErrorMessage);
			};
		}

		/// <summary>
		/// Construct an <see cref="Client"/> for a local connection
		/// </summary>
		public Client()
		{
			ChannelFactoryCache = new Dictionary<string, ChannelFactory>();
			server = new Server(this);
		}

		/// <summary>
		/// Construct an <see cref="Client"/> for a remote connection
		/// </summary>
		/// <param name="loginInfo">The <see cref="RemoteLoginInfo"/> for a remote connection</param>
		public Client(RemoteLoginInfo loginInfo) : this()
		{
			if (!loginInfo.HasPassword)
				throw new InvalidOperationException("password must be set on loginInfo!");
			_loginInfo = loginInfo;
		}

		/// <inheritdoc />
		public bool IsRemoteConnection { get { return LoginInfo != null; } }

		/// <summary>
		/// Closes all <see cref="ChannelFactory"/>s stored in <see cref="ChannelFactoryCache"/> and <see langword="nulls"/> it
		/// </summary>
		public void Dispose()
		{
			lock (this)
			{
				if (ChannelFactoryCache == null)
					return;
				foreach (var I in ChannelFactoryCache)
				{
					var cf = I.Value;
					try
					{
						cf.Close();
					}
					catch
					{
						cf.Abort();
					}
				}
				ChannelFactoryCache = null;
			}
		}

		/// <inheritdoc />
		public bool VersionMismatch(out string errorMessage)
		{
			if (ServerVersion.Major != Version.Major || ServerVersion.Minor != Version.Minor || ServerVersion.Build != Version.Build)	//don't care about the patch level
			{
				errorMessage = String.Format("Version mismatch between interface version ({0}) and service version ({1}). Some functionality may crash this program.", Version, ServerVersion);
				return true;
			}
			errorMessage = null;
			return false;
		}

		/// <summary>
		/// Returns the requested <see cref="Client"/> component <see langword="interface"/> for the instance <see cref="InstanceName"/>. This does not guarantee a successful connection. <see cref="ChannelFactory{TChannel}"/>s created this way are recycled for minimum latency and bandwidth usage
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> to retrieve</typeparam>
		/// <param name="instanceName">The name of the <see cref="IInstance"/> to use</param>
		/// <returns>The correct component <see langword="interface"/></returns>
		internal T GetComponent<T>(string instanceName)
		{
			var actualToT = typeof(T);
			var tot = actualToT.Name;
			if (instanceName != null)
				tot = instanceName + tot; 
			ChannelFactory<T> cf;

			lock (this)
			{
				if (ChannelFactoryCache == null)
					throw new ObjectDisposedException(GetType().Name);
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
				cf = CreateChannel<T>(instanceName);
				ChannelFactoryCache[tot] = cf;
			}
			return cf.CreateChannel();
		}

		/// <summary>
		/// Directly creates a <see cref="ChannelFactory{TChannel}"/> for <typeparamref name="T"/> without caching. This should be eventually closed by the caller
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> of the channel to be created</typeparam>
		/// <param name="instanceName">The instance to connect to, if any</param>
		/// <returns>The correct <see cref="ChannelFactory{TChannel}"/></returns>
		ChannelFactory<T> CreateChannel<T>(string instanceName)
		{
			var accessPath = instanceName == null ? Definitions.MasterInterfaceName : String.Format("{0}/{1}", Definitions.InstanceInterfaceName, instanceName);
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
			new NetNamedPipeBinding { SendTimeout = new TimeSpan(0, 0, 30), MaxReceivedMessageSize = Definitions.TransferLimitLocal }, new EndpointAddress(String.Format("net.pipe://localhost/{0}/{1}", accessPath, interfaceName)));                                                      //10 megs
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
			//tls memes
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			//okay we're going over
			var binding = new BasicHttpsBinding()
			{
				SendTimeout = new TimeSpan(0, 0, 40),
				MaxReceivedMessageSize = Definitions.TransferLimitRemote
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
				GetComponent<ITGConnectivity>(null).VerifyConnection();
			}
			catch (Exception e)
			{
				error = e.ToString();
				return ConnectivityLevel.None;
			}

			try
			{
				GetComponent<ITGLanding>(null).Version();
			}
			catch(Exception e)
			{
				error = e.ToString();
				return ConnectivityLevel.Connected;
			}

			try
			{
				GetComponent<ITGSService>(null).Version();
				error = null;
				return ConnectivityLevel.Administrator;
			}
			catch(Exception e)
			{
				error = e.ToString();
				return ConnectivityLevel.Authenticated;
			}
		}
	}
}
