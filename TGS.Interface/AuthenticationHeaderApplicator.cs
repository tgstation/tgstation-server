using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Description;

namespace TGS.Interface
{
	/// <summary>
	/// Used to attach windows credential headers to SOAP messages
	/// </summary>
	sealed class AuthenticationHeaderApplicator : IEndpointBehavior, IClientMessageInspector
	{
		/// <summary>
		/// The credentials to attach
		/// </summary>
		readonly RemoteLoginInfo remoteLoginInfo;
		
		/// <summary>
		/// Construct a <see cref="AuthenticationHeaderApplicator"/>
		/// </summary>
		/// <param name="loginInfo">The <see cref="RemoteLoginInfo"/> to use</param>
		public AuthenticationHeaderApplicator(RemoteLoginInfo loginInfo)
		{
			remoteLoginInfo = loginInfo;
		}

		/// <summary>
		/// Add <see langword="this"/> to the message inspectors for the channel
		/// </summary>
		/// <param name="endpoint">The <see cref="ServiceEndpoint"/></param>
		/// <param name="clientRuntime">The <see cref="ClientRuntime"/></param>
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

		/// <summary>
		/// Attach <see cref="RemoteLoginInfo.Username"/> and <see cref="RemoteLoginInfo.Password"/> to the <paramref name="request"/>
		/// </summary>
		/// <param name="request">The outgoing request</param>
		/// <param name="channel">The <see cref="IClientChannel"/></param>
		/// <returns></returns>
		public object BeforeSendRequest(ref Message request, IClientChannel channel)
		{
			request.Headers.Add(MessageHeader.CreateHeader("Username", "http://tempuri.org", remoteLoginInfo.Username));
			request.Headers.Add(MessageHeader.CreateHeader("Password", "http://tempuri.org", remoteLoginInfo.Password));
			return null;
		}

		/// <summary>
		/// Unused implementation of <see cref="IEndpointBehavior"/>
		/// </summary>
		public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
			//intentionally left blank
		}

		/// <summary>
		/// Unused implementation of <see cref="IClientMessageInspector"/>
		/// </summary>
		public void AfterReceiveReply(ref Message reply, object correlationState)
		{
			//intentionally left blank
		}

		/// <summary>
		/// Unused implementation of <see cref="IEndpointBehavior"/>
		/// </summary>
		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
			//intentionally left blank
		}

		/// <summary>
		/// Unused implementation of <see cref="IEndpointBehavior"/>
		/// </summary>
		public void Validate(ServiceEndpoint endpoint)
		{
			//intentionally left blank
		}
	}
}
