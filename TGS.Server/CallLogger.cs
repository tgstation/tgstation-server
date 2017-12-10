using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace TGS.Server
{
	sealed class AddCallLoggerBehavior : IOperationBehavior
	{
		public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
		{
		}

		public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
		{
			clientOperation.ClientParameterInspectors.Add(new CallLogger());
		}

		public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
		{
			dispatchOperation.ParameterInspectors.Add(new CallLogger());
		}

		public void Validate(OperationDescription operationDescription)
		{
		}
	}
	sealed class CallLogger : IParameterInspector
	{
		static readonly IDictionary<string, int> ActiveCalls = new Dictionary<string, int>();

		public void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState)
		{
			lock (ActiveCalls)
				--ActiveCalls[operationName];
		}

		public object BeforeCall(string operationName, object[] inputs)
		{
			lock (ActiveCalls)
			{
				string res = String.Empty;
				foreach (var I in ActiveCalls)
				{
					if (I.Value > 0)
						res += Environment.NewLine + I.Key + ": " + I.Value;
				}
				if (res != String.Empty)
				{
					res = String.Format("Starting call {0}. Warning: other calls in progress!{1}", operationName, res);
					Server.Logger.WriteInfo(res, EventID.CallTracking, 0);
				}
				if (!ActiveCalls.ContainsKey(operationName))
					ActiveCalls.Add(operationName, 0);
				++ActiveCalls[operationName];
				return null;
			}
		}
	}
}
