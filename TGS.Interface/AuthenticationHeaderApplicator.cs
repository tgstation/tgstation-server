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
#if __MonoCS__
			var prop = clientRuntime.GetType()
									.GetTypeInfo()
									.GetDeclaredProperty("MessageInspectors");
			var inspectors = (ICollection<IClientMessageInspector>)prop
						.GetValue(clientRuntime);
#else
			var inspectors = clientRuntime.ClientMessageInspectors;
#endif
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
