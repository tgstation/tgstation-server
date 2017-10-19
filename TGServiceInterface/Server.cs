using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Principal;
using System.ServiceModel;

namespace TGServiceInterface
{
	/// <summary>
	/// Main inteface class for the service
	/// </summary>
	public class Server
	{
		/// <summary>
		/// List of <see langword="interface"/>s that can be used with GetComponen
		/// </summary>
		public static readonly IList<Type> ValidInterfaces = CollectComponents();

		/// <summary>
		/// The maximum message size to and from a local server 
		/// </summary>
		public static readonly long TransferLimitLocal = Int32.MaxValue;   //2GB can't go higher

		/// <summary>
		/// The maximum message size to and from a remote server
		/// </summary>
		public static readonly long TransferLimitRemote = 10485760;	//10 MB

		/// <summary>
		/// Base name of the communication pipe
		/// they are formatted as MasterPipeName/ComponentName
		/// </summary>
		public static string MasterInterfaceName = "TGStationServerService";

		/// <summary>
		/// If this is set, we will try and connect to an HTTPS server running at this address
		/// </summary>
		static string HTTPSURL;

		/// <summary>
		/// The port used by the service
		/// </summary>
		static ushort HTTPSPort = 38607;

		/// <summary>
		/// Username for remote operations
		/// </summary>
		static string HTTPSUsername;

		/// <summary>
		/// Password for remote operations
		/// </summary>
		static string HTTPSPassword;

		/// <summary>
		/// Associated list of open <see cref="ChannelFactory"/>s keyed by <see langword="interface"/> type. A <see cref="ChannelFactory"/> in this list may close or fault at any time. Must be locked before being accessed
		/// </summary>
		static Dictionary<Type, ChannelFactory> ChannelFactoryCache = new Dictionary<Type, ChannelFactory>();

		/// <summary>
		/// Returns a <see cref="IList{T}"/> of <see langword="interface"/> <see cref="Type"/>s that can be used with the service
		/// </summary>
		/// <returns>A <see cref="IList{T}"/> of <see langword="interface"/> <see cref="Type"/>s that can be used with the service</returns>
		static IList<Type> CollectComponents()
		{
			//find all interfaces in this assembly in this namespace that have the service contract attribute
			var query = from t in Assembly.GetExecutingAssembly().GetTypes()
						where t.IsInterface 
						&& t.Namespace == typeof(Server).Namespace
						&& t.GetCustomAttribute(typeof(ServiceContractAttribute)) != null
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
				ErrorMessage = String.Format("The certificate failed to verify for {0}:{1}. {2} {3}", HTTPSURL, HTTPSPort, ErrorMessage, cert.ToString());
				return handler(ErrorMessage);
			};
		}

		/// <summary>
		/// Set the interface to look for services on the current computer
		/// </summary>
		public static void MakeLocalConnection()
		{
			HTTPSURL = null;
			HTTPSPassword = null;
			ClearCachedChannels();
		}

		/// <summary>
		/// Closes all <see cref="ChannelFactory"/>s stored in <see cref="ChannelFactoryCache"/> and clears it
		/// </summary>
		static void ClearCachedChannels()
		{
			lock (ChannelFactoryCache)
			{
				foreach (var I in ChannelFactoryCache)
					CloseChannel(I.Value);
				ChannelFactoryCache.Clear();
			}
		}

		/// <summary>
		/// Returns <see langword="true"/> if the <see cref="Server"/> interface being used to connect to a service does not have the same release version as the service
		/// </summary>
		/// <param name="errorMessage">An error message to display to the user should this function return <see langword="true"/></param>
		/// <returns><see langword="true"/> if the <see cref="Server"/> interface being used to connect to a service does not have the same release version as the service</returns>
		public static bool VersionMismatch(out string errorMessage)
		{
			var splits = Server.GetComponent<ITGSService>().Version().Split(' ');
			var theirs = new Version(splits[splits.Length - 1].Substring(1));
			var ours = new Version(FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion);
			if(theirs != ours)
			{
				errorMessage = String.Format("Version mismatch between interface version ({0}) and service version ({1}). Some functionality may crash this program.", ours, theirs);
				return true;
			}
			errorMessage = null;
			return false;
		}

		/// <summary>
		/// Set the remote <see cref="Server"/> to connect to along with 
		/// </summary>
		/// <param name="address">The address of the remote server</param>
		/// <param name="port">The port the remote server runs on</param>
		/// <param name="username">Windows account username for the remote server</param>
		/// <param name="password">Windows account password for the remote server</param>
		public static void SetRemoteLoginInformation(string address, ushort port, string username, string password)
		{
			HTTPSURL = address;
			HTTPSPort = port;
			HTTPSUsername = username;
			HTTPSPassword = password;
			ClearCachedChannels();
		}

		/// <summary>
		/// Safely shuts down a single <see cref="ChannelFactory"/>
		/// </summary>
		/// <param name="cf">The <see cref="ChannelFactory"/> to shutdown</param>
		public static void CloseChannel(ChannelFactory cf)
		{
			try
			{
				cf.Close();
			}
			catch
			{
				cf.Abort();
			}
		}

		/// <summary>
		/// Returns the requested <see cref="Server"/> component <see langword="interface"/>. This does not guarantee a successful connection. <see cref="ChannelFactory{TChannel}"/>s created this way are recycled for minimum latency and bandwidth usage
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> to retrieve</typeparam>
		/// <returns>The correct component <see langword="interface"/></returns>
		public static T GetComponent<T>()
		{
			var tot = typeof(T);
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
				cf = CreateChannel<T>();
				ChannelFactoryCache[tot] = cf;
			}
			return cf.CreateChannel();
		}

		/// <summary>
		/// Directly creates a <see cref="ChannelFactory{TChannel}"/> for <typeparamref name="T"/> without caching. This should be eventually closed by the caller
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> of the channel to be created</typeparam>
		/// <returns>The correct <see cref="ChannelFactory{TChannel}"/></returns>
		/// <exception cref="Exception">Thrown if <typeparamref name="T"/> isn't a valid component <see langword="interface"/></exception>
		public static ChannelFactory<T> CreateChannel<T>()
		{
			var ToT = typeof(T);
			if (!ValidInterfaces.Contains(ToT))
				throw new Exception("Invalid type!");
			var InterfaceName = typeof(T).Name;
			if (HTTPSURL == null)
			{
				var res2 = new ChannelFactory<T>(
				new NetNamedPipeBinding { SendTimeout = new TimeSpan(0, 0, 30), MaxReceivedMessageSize = TransferLimitLocal }, new EndpointAddress(String.Format("net.pipe://localhost/{0}/{1}", MasterInterfaceName, InterfaceName)));														//10 megs
				res2.Credentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Impersonation;
				return res2;
			}

			//okay we're going over
			var binding = new WSHttpBinding()
			{
				SendTimeout = new TimeSpan(0, 0, 40),
				MaxReceivedMessageSize = TransferLimitRemote
			};
			var requireAuth = InterfaceName != typeof(ITGConnectivity).Name;
			binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
			binding.Security.Mode = requireAuth ? SecurityMode.TransportWithMessageCredential : SecurityMode.Transport;    //do not require auth for a connectivity check
			binding.Security.Message.ClientCredentialType = requireAuth ? MessageCredentialType.UserName : MessageCredentialType.None;
			var address = new EndpointAddress(String.Format("https://{0}:{1}/{2}/{3}", HTTPSURL, HTTPSPort, MasterInterfaceName, InterfaceName));
			var res = new ChannelFactory<T>(binding, address);
			if (requireAuth)
			{
				res.Credentials.UserName.UserName = HTTPSUsername;
				res.Credentials.UserName.Password = HTTPSPassword;
			}
			return res;
		}

		/// <summary>
		/// Used to test if the service is avaiable on the machine. Note that state can technically change at any time and any call to the service may throw an exception because it failed
		/// </summary>
		/// <returns><see langword="null"/> on successful connection, error message <see cref="string"/> on failure</returns>
		public static string VerifyConnection()
		{
			try
			{
				GetComponent<ITGConnectivity>().VerifyConnection();
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		/// <summary>
		/// Checks if the supplied user's credentials have permission to use the service. Requires a successful prior call to <see cref="VerifyConnection"/>
		/// </summary>
		/// <returns><see langword="true"/> if credentials are valid, <see langword="false"/> otherwise</returns>
		public static bool Authenticate()
		{
			try
			{
				GetComponent<ITGSService>().Version();
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Checks if the current login can use <see cref="ITGAdministration"/>. Requires a successful prior call to <see cref="Authenticate"/>
		/// </summary>
		/// <returns><see langword="true"/> if the connection may use <see cref="ITGAdministration"/>, <see langword="false"/> otherwise</returns>
		public static bool AuthenticateAdmin()
		{
			try
			{
				GetComponent<ITGAdministration>().GetCurrentAuthorizedGroup();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
