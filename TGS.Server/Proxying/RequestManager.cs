using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Timers;
using System.Threading.Tasks;
using TGS.Interface.Components;
using TGS.Interface.Proxying;

namespace TGS.Server.Proxying
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	sealed class RequestManager : ITGRequestManager, IDisposable
	{
		const int GCInterval = 10000;

		static readonly Assembly interfaceAssembly = Assembly.GetAssembly(typeof(ITGSService));

		public object Implementation => _implementation;

		readonly object _implementation;
		readonly IDictionary<UInt64, Request> requests;
		readonly Timer garbageCollectionTimer;

		UInt64 nextRequest;

		public RequestManager(object implementation)
		{
			_implementation = implementation;
			requests = new Dictionary<UInt64, Request>();
			garbageCollectionTimer = new Timer(GCInterval);
			garbageCollectionTimer.Elapsed += GarbageCollectionTimer_Elapsed;
			garbageCollectionTimer.Start();
		}

		void GarbageCollectionTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			lock (requests)
				foreach (var I in requests.Where(x => x.Value.TimedOut()).Select(x => x.Key).ToList())
					requests.Remove(I);
		}

		public void Dispose()
		{
			garbageCollectionTimer.Dispose();
		}

		RequestInfo CreateRequest(MethodInfo methodInfo, Task result)
		{
			Request req;
			if (methodInfo.ReturnType == typeof(void))
				req = new Request(result);
			else
			{
				var ourType = GetType();
				var taskType = result.GetType().GenericTypeArguments[0];
				var resultingRequestConstructor = ourType.GetMethod(nameof(CreateResultingRequest), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { taskType });
				req = (Request)resultingRequestConstructor.Invoke(this, new object[] { result });
			}

			UInt64 id;
			lock (requests)
			{
				id = ++nextRequest;
				//not handling if the Dictionary gets full because it's fucking impossible
				requests.Add(id, req);
			}

			return req.GetRequestInfo(id, req.Token);
		}

		ResultingRequest<T> CreateResultingRequest<T>(Task<T> task)
		{
			return new ResultingRequest<T>(task);
		}

		public RequestInfo BeginRequest(string componentName, string methodName, IEnumerable<object> parameters, string user, string password)
		{
			//Check if the requested component is actually a component
			componentName = String.Format("{0}.{1}.{2}.{3}", nameof(TGS), nameof(Interface), nameof(Interface.Components), componentName);
			try
			{
				var componentType = interfaceAssembly.GetType(componentName, true);
				var methodType = componentType.GetMethod(methodName);

				var deserializedParams = new List<object>();
				var paras = methodType.GetParameters();

				foreach (var I in paras.Zip(parameters, (a, p) => new { ParameterType = a.ParameterType, Value = p }))
					deserializedParams.Add(Serializer.DeserializeObject(I.Value, I.ParameterType));

				var resultTask = (Task)methodType.Invoke(Implementation, deserializedParams.ToArray());

				return CreateRequest(methodType, resultTask);
			}
			catch
			{
				return new RequestInfo(RequestState.Invalid, 0, null);
			}
		}

		public object EndRequest(RequestInfo requestInfo)
		{
			Request req;
			lock (requests)
			{
				if (!requests.TryGetValue(requestInfo.RequestID, out req))
					return null;
				if (req.ValidateToken(requestInfo.RequestToken))
					requests.Remove(requestInfo.RequestID);
			}
			return Serializer.SerializeObject(req.GetResult(requestInfo.RequestToken));
		}

		public List<RequestInfo> QueryRequests(IEnumerable<RequestInfo> requestInfos)
		{
			var newReqInfo = new List<RequestInfo>();
			lock(requests)
				foreach(var I in requestInfos)
				{
					if (requests.TryGetValue(I.RequestID, out Request req))
						newReqInfo.Add(req.GetRequestInfo(I.RequestID, I.RequestToken));
					else
						newReqInfo.Add(new RequestInfo(RequestState.Invalid, I.RequestID, null));
				}
			return newReqInfo;
		}
	}
}
