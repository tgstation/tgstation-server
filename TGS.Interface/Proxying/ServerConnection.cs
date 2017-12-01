using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Principal;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;

namespace TGS.Interface.Proxying
{
	sealed class ServerConnection : ICallBinder, IConnectionManager
	{
		const int RequeryRate = 3000;
		public bool IsRemoteConnection => LoginInfo != null;

		public RemoteLoginInfo LoginInfo => _loginInfo;

		ITGRequestManager Connection => channelFactory.CreateChannel();

		readonly RemoteLoginInfo _loginInfo;
		readonly string instanceName;
		readonly IDictionary<UInt64, Action<object>> results;

		ChannelFactory<ITGRequestManager> channelFactory;
		List<RequestInfo> onGoingRequests;
		Thread queryThread;

		public ServerConnection(RemoteLoginInfo loginInfo, string InstanceName)
		{
			results = new Dictionary<UInt64, Action<object>>();
			onGoingRequests = new List<RequestInfo>();
			_loginInfo = loginInfo;
			instanceName = InstanceName;
			RebuildConnection();
		}

		public void Dispose()
		{
			try
			{
				channelFactory.Close();
			}
			catch
			{
				channelFactory.Abort();
			}
			channelFactory = null;
		}

		T WrapServerOp<T>(Func<T> action)
		{
			try
			{
				return action();
			}
			catch
			{
				RebuildConnection();
				return action();
			}
		}

		bool QueryRequests()
		{
			lock (this)
			{
				try
				{
					onGoingRequests = WrapServerOp(() => Connection.QueryRequests(onGoingRequests));
				}
				catch
				{
					foreach (var I in onGoingRequests)
						FinishRequest(I);
					onGoingRequests.Clear();
				}
				onGoingRequests.RemoveAll(x => CheckFinishRequest(x));
				return onGoingRequests.Count == 0;
			}
		}

		bool CheckFinishRequest(RequestInfo request)
		{
			if(request.RequestState != RequestState.InProgress)
			{
				FinishRequest(request);
				return true;
			}
			return false;
		}

		void FinishRequest(RequestInfo request)
		{
			Action<object> finisher;
			lock (results)
				if(results.TryGetValue(request.RequestID, out finisher))
					results.Remove(request.RequestID);

			//always call into the server regardless of finisher to remove the request result so it doesn't hang around
			object result;
			try
			{
				result = WrapServerOp(() => Connection.EndRequest(request));
			}
			catch (Exception e)
			{
				result = e;
			}

			finisher?.Invoke(result);
		}
		
		void QueryLoop()
		{
			Thread.CurrentThread.Name = "ITG Request Handler";

			while (true)
			{
				lock (this)
					if (QueryRequests())
					{
						queryThread = null;
						break;
					}
				Thread.Sleep(RequeryRate);
			}
		}

		public Task<T> HandleCall<T>(string componentName, MethodInfo method, object[] args)
		{
			if (channelFactory == null)
				throw new ObjectDisposedException(nameof(ServerConnection));

			var serializedArgs = new List<object>();
			foreach (var I in args)
				serializedArgs.Add(Serializer.SerializeObject(I));

			var request = WrapServerOp(() => Connection.BeginRequest(componentName, method.Name, serializedArgs, LoginInfo?.Username, LoginInfo?.Password));

			if (request.RequestState == RequestState.Invalid || request.RequestState == RequestState.BadToken)
				throw new CommunicationException(String.Format("Unable to begin request: {0}.{1}({2}) -> {3}", componentName, method.Name, args, request.RequestState));

			var tcs = new TaskCompletionSource<T>();
			lock (results)
				results.Add(request.RequestID, (obj) =>
				{
					if (obj is Exception asException)
						tcs.SetException(asException);
					else
						tcs.SetResult((T)Serializer.DeserializeObject(obj, typeof(T)));
				});

			lock (this)
			{
				onGoingRequests.Add(request);
				if (queryThread == null)
				{
					queryThread = new Thread(QueryLoop);
					queryThread.Start();
				}
			}
			return tcs.Task;
		}
		
		public void RebuildConnection()
		{
			channelFactory = CreateChannel();
		}

		/// <inheritdoc />
		public T GetComponent<T>() where T : class
		{
			if (channelFactory == null)
				throw new ObjectDisposedException(nameof(ServerConnection));
			return new ComponentProxy(typeof(T), this).ToComponent<T>();
		}

		/// <summary>
		/// Directly creates a <see cref="ChannelFactory{TChannel}"/> for <typeparamref name="T"/> without caching. This should be eventually closed by the caller
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> of the channel to be created</typeparam>
		/// <param name="instanceName">The instance to connect to, if any</param>
		/// <returns>The correct <see cref="ChannelFactory{TChannel}"/></returns>
		ChannelFactory<ITGRequestManager> CreateChannel()
		{
			var accessPath = String.Format("{0}/{1}", instanceName == null ? Definitions.MasterInterfaceName : String.Format("{0}/{1}", Definitions.InstanceInterfaceName, instanceName), nameof(ITGRequestManager));
			if (!IsRemoteConnection)
				return CreateLocalChannel(accessPath);
			return CreateRemoteChannel(accessPath);
		}

		//NOTE: This needs to be kept seperate from CreateRemoteChannel because not all our client implementations have NetNamedPipeBinding and attempting to call this function on those platforms will throw an exception

		/// <summary>
		/// Directly creates a <see cref="ChannelFactory{TChannel}"/> for on a local connection <typeparamref name="T"/> without caching. This should be eventually closed by the caller
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> of the channel to be created</typeparam>
		/// <param name="instanceName">The instance to connect to, if any</param>
		/// <param name="accessPath">The URL of the interface to connect to</param>
		/// <returns>The correct <see cref="ChannelFactory{TChannel}"/></returns>
		static ChannelFactory<ITGRequestManager> CreateLocalChannel(string accessPath)
		{
			var res2 = new ChannelFactory<ITGRequestManager>(
			new NetNamedPipeBinding { SendTimeout = new TimeSpan(0, 0, 30), MaxReceivedMessageSize = Definitions.TransferLimitLocal }, new EndpointAddress(String.Format("net.pipe://localhost/{0}", accessPath)));                                                      //10 megs
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
		ChannelFactory<ITGRequestManager> CreateRemoteChannel(string accessPath)
		{
			//okay we're going over
			var binding = new BasicHttpsBinding()
			{
				SendTimeout = new TimeSpan(0, 0, 40),
				MaxReceivedMessageSize = Definitions.TransferLimitRemote
			};

			var url = String.Format("https://{0}:{1}/{2}", LoginInfo.IP, LoginInfo.Port, accessPath);
			var address = new EndpointAddress(url);
			var res = new ChannelFactory<ITGRequestManager>(binding, address);

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

			return res;
		}
	}
}
