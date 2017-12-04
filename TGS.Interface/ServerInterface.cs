using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.ServiceModel;
using TGS.Interface.Components;
using TGS.Interface.Proxying;

namespace TGS.Interface
{
	/// <inheritdoc />
	sealed public class ServerInterface : IServerInterface
	{
		/// <summary>
		/// Version of the interface
		/// </summary>
		public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;

		/// <summary>
		/// List of <see langword="interface"/>s that can be used with <see cref="GetServiceComponent{T}"/>
		/// </summary>
		public static readonly IList<Type> ValidServiceInterfaces = new List<Type> { typeof(ITGLanding), typeof(ITGServer), typeof(ITGInstanceManager) };

		/// <summary>
		/// List of <see langword="interface"/>s that can be used with <see cref="GetComponent{T}"/>
		/// </summary>
		public static readonly IList<Type> ValidInstanceInterfaces = CollectComponents();

		/// <summary>
		/// The <see cref="ServerVersion"/>
		/// </summary>
		Version _serverVersion;

		/// <inheritdoc />
		public Version ServerVersion => serverConnection.ServerVersion;

		/// <inheritdoc />
		public string InstanceName { get; private set; }

		/// <inheritdoc />
		public bool IsRemoteConnection => serverConnection.IsRemoteConnection;

		/// <inheritdoc />
		public RemoteLoginInfo LoginInfo => serverConnection.LoginInfo;

		readonly IConnectionManager serverConnection;

		IConnectionManager instanceConnection;

		/// <summary>
		/// Returns a <see cref="IList{T}"/> of <see langword="interface"/> <see cref="Type"/>s that can be used with the service
		/// </summary>
		/// <returns>A <see cref="IList{T}"/> of <see langword="interface"/> <see cref="Type"/>s that can be used with the service</returns>
		static IList<Type> CollectComponents()
		{
			var ConnectivityComponent = typeof(ITGComponent);
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
		/// Construct an <see cref="ServerInterface"/> for a local connection
		/// </summary>
		public ServerInterface()
		{
			serverConnection = new ServerConnection(null, null);
		}

		/// <summary>
		/// Construct an <see cref="ServerInterface"/> for a remote connection
		/// </summary>
		/// <param name="loginInfo">The <see cref="RemoteLoginInfo"/> for a remote connection</param>
		public ServerInterface(RemoteLoginInfo loginInfo)
		{
			if (!loginInfo.HasPassword)
				throw new InvalidOperationException("password must be set on loginInfo!");
			serverConnection = new ServerConnection(loginInfo, null);
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
				instanceConnection?.Dispose();
			InstanceName = instanceName;
			if (skipChecks)
				return ConnectivityLevel.Connected;
			try
			{
				GetComponent<ITGInstance>().VerifyConnection();
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
			lock (this)
			{
				if (instanceConnection == null)
					instanceConnection = new ServerConnection(LoginInfo, InstanceName);
			}
			var ToT = typeof(T);
			if (!ValidInstanceInterfaces.Contains(ToT))
				throw new Exception("Invalid type!");
			return instanceConnection.GetComponent<T>();
		}

		/// <inheritdoc />
		public T GetServiceComponent<T>() where T : class
		{
			var ToT = typeof(T);
			if (!ValidServiceInterfaces.Contains(ToT))
				throw new Exception("Invalid type!");
			return serverConnection.GetComponent<T>();
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
				GetServiceComponent<ITGConnectivity>().VerifyConnection();
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
				GetServiceComponent<ITGServer>().Version().Wait();
				error = null;
				return ConnectivityLevel.Administrator;
			}
			catch(Exception e)
			{
				error = e.ToString();
				return ConnectivityLevel.Authenticated;
			}
		}
		
		public void Dispose()
		{
			instanceConnection?.Dispose();
			serverConnection.Dispose();
		}
	}
}
