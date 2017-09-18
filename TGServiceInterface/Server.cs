using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
namespace TGServiceInterface
{
	public class Server
	{
		/// <summary>
		/// List of types that can be used with GetComponen
		/// </summary>
		public static readonly IList<Type> ValidInterfaces = new List<Type> { typeof(ITGByond), typeof(ITGChat), typeof(ITGCompiler), typeof(ITGConfig), typeof(ITGDreamDaemon), typeof(ITGRepository), typeof(ITGSService), typeof(ITGConnectivity), typeof(ITGAdministration) };

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
		/// Set the interface to look for services on the current computer
		/// </summary>
		public static void MakeLocalConnection()
		{
			HTTPSURL = null;
		}

		/// <summary>
		/// Set the interface to look for services on a remote computer
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		public static void SetRemoteLoginInformation(string address, ushort port, string username, string password)
		{
			HTTPSURL = address;
			HTTPSPort = port;
			HTTPSUsername = username;
			HTTPSPassword = password;
		}

		/// <summary>
		/// Returns the requested server component interface. This does not guarantee a successful connection
		/// </summary>
		/// <typeparam name="T">The type of the component to retrieve</typeparam>
		/// <returns></returns>
		public static T GetComponent<T>()
		{
			var ToT = typeof(T);
			if (!ValidInterfaces.Contains(ToT))
				throw new Exception("Invalid type!");
			var InterfaceName = typeof(T).Name;
			if (HTTPSURL == null)
				return new ChannelFactory<T>(new NetNamedPipeBinding { SendTimeout = new TimeSpan(0, 10, 0) }, new EndpointAddress(String.Format("net.pipe://localhost/{0}/{1}", MasterInterfaceName, InterfaceName))).CreateChannel();

			//okay we're going over
			var binding = new WSHttpBinding();
			var requireAuth = InterfaceName != typeof(ITGConnectivity).Name;
			binding.Security.Mode = requireAuth ? SecurityMode.TransportWithMessageCredential : SecurityMode.Transport;    //do not require auth for a connectivity check
			binding.Security.Message.ClientCredentialType = requireAuth ? MessageCredentialType.UserName : MessageCredentialType.None;
			var address = new EndpointAddress(String.Format("https://{0}:{1}/{2}/{3}", HTTPSURL, HTTPSPort, MasterInterfaceName, InterfaceName));
			var cf = new ChannelFactory<T>(binding, address);
			if (requireAuth)
			{
				cf.Credentials.UserName.UserName = HTTPSUsername;
				cf.Credentials.UserName.Password = HTTPSPassword;
			}
#if DEBUG
			//allow self signed certs in debug mode
			ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, error) => true;
#endif
			return cf.CreateChannel();
		}
		
		/// <summary>
		/// Used to test if the service is avaiable on the machine
		/// Note that state can technically change at any time
		/// and any call to the service may throw an exception because it failed
		/// </summary>
		/// <returns>null on successful connection, error message on failure</returns>
		public static string VerifyConnection()
		{
			try
			{
				GetComponent<ITGConnectivity>().VerifyConnection();
				return null;
			}
			catch(Exception e)
			{
				return e.ToString();
			}
		}

		/// <summary>
		/// As opposed to VerifyConnection(), this check user credentials
		/// Requires a prior call to <see cref="VerifyConnection"/>
		/// </summary>
		/// <returns>true if credentials are valid, false otherwise</returns>
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
		/// As opposed to Authentication() this returns true if the current login can use the <see cref="ITGAdministration"/> interface.
		/// Requires a prior call to <see cref="Authenticate"/>
		/// </summary>
		/// <returns>true if the connection may use the <see cref="ITGAdministration"/> interface, false otherwise</returns>
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
