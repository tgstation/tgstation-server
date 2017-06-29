using System;
using System.Collections.Generic;
using System.ServiceModel;
namespace TGServiceInterface
{
	public class Service
	{
		/// <summary>
		/// List of types that can be used with GetComponen
		/// </summary>
		public static readonly IList<Type> ValidInterfaces = new List<Type> { typeof(ITGByond), typeof(ITGChat), typeof(ITGCompiler), typeof(ITGConfig), typeof(ITGDreamDaemon), typeof(ITGRepository), typeof(ITGInstance) };

		/// <summary>
		/// Base name of the communication pipe
		/// they are formatted as MasterPipeName/ComponentName
		/// </summary>
		public static readonly string MasterPipeName = "TGStationServerService";
		public static readonly string InstanceFormat = "Instance-{0}";

		public static T CreateChanneledInterface<T>(string PipeName)
		{
			return new ChannelFactory<T>(new NetNamedPipeBinding { SendTimeout = new TimeSpan(0, 10, 0) }, new EndpointAddress(String.Format("net.pipe://localhost/{0}/{1}", MasterPipeName, PipeName))).CreateChannel();
		}

		/// <summary>
		/// Returns the requested server component interface. This does not guarantee a successful connection
		/// </summary>
		/// <typeparam name="T">The type of the component to retrieve</typeparam>
		/// <returns></returns>
		public static T GetComponent<T>(int instance)
		{
			var ToT = typeof(T);
			if (!ValidInterfaces.Contains(ToT))
				throw new Exception("Invalid type!");

			if (instance <= 0 || instance >= 1000)
				throw new Exception("Invalid instance ID");

			return CreateChanneledInterface<T>(String.Format(InstanceFormat + "/{1}", instance, typeof(T).Name));
		}

		public static ITGSService Get()
		{
			return CreateChanneledInterface<ITGSService>(typeof(ITGSService).Name);
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
				Get().VerifyConnection();
				return null;
			}
			catch(Exception e)
			{
				return e.ToString();
			}
		}
	}

	/// <summary>
	/// Interface for managing the service
	/// </summary>
	[ServiceContract]
	public interface ITGSService
	{
		/// <summary>
		/// Does nothing on the server end, but if the call completes, you can be sure you are connected. WCF won't throw until you try until you actually use the API
		/// </summary>
		[OperationContract]
		void VerifyConnection();

		/// <summary>
		/// Next stop of the service will not close DD and sets a flag for it to reattach once it restarts
		/// </summary>
		[OperationContract]
		void PrepareForUpdate();

		[OperationContract]
		[Obsolete("StopForUpdate is deprecated, please use PrepareForUpdate instead")]
		void StopForUpdate();

		/// <summary>
		/// Retrieve's the service's version
		/// </summary>
		/// <returns>The service's version</returns>
		[OperationContract]
		string Version();

		/// <summary>
		/// Lists all instances the service manages
		/// </summary>
		/// <returns>A list of instance ids -> names</returns> of the instance</param>
		/// <returns></returns>
		[OperationContract]
		IDictionary<int, string> ListInstances();

		/// <summary>
		/// Creates a new instance at the specified path
		/// </summary>
		/// <param name="name">The name of the instance</param>
		/// <param name="path">The path of the instance</param>
		/// <returns></returns>
		[OperationContract]
		string CreateInstance(string name, string path);
	}
}
