using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Description;

namespace TGS.Interface
{
	sealed class AuthenticationHeaderApplicator : IEndpointBehavior, IClientMessageInspector
	{
		readonly RemoteLoginInfo remoteLoginInfo;
			
		public AuthenticationHeaderApplicator(RemoteLoginInfo loginInfo)
		{
			remoteLoginInfo = loginInfo;
		}

		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
			var t = Type.GetType("Mono.Runtime");
			ICollection<IClientMessageInspector> inspectors;
			if (t != null)
			{
				var prop = clientRuntime.GetType()
									.GetTypeInfo()
									.GetDeclaredProperty("MessageInspectors");
				inspectors = (ICollection<IClientMessageInspector>)prop
							.GetValue(clientRuntime);
			}
			else
				inspectors = clientRuntime.ClientMessageInspectors;
			inspectors.Add(this);
		}

		public object BeforeSendRequest(ref Message request, IClientChannel channel)
		{
			request.Headers.Add(MessageHeader.CreateHeader("Username", "http://tempuri.org", remoteLoginInfo.Username));
			request.Headers.Add(MessageHeader.CreateHeader("Password", "http://tempuri.org", remoteLoginInfo.Password));
			return null;
		}

		public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
			//intentionally left blank
		}

		public void AfterReceiveReply(ref Message reply, object correlationState)
		{
			//intentionally left blank
		}

		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
			//intentionally left blank
		}

		public void Validate(ServiceEndpoint endpoint)
		{
			//intentionally left blank
		}
	}
}
