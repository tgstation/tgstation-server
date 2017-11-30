using System;
using System.Runtime.Serialization;

namespace TGS.Interface.Proxying
{
	[DataContract]
	public sealed class RequestInfo
	{
		public RequestState RequestState { get; private set; }
		public UInt64 RequestID { get; private set; }
		public string RequestToken { get; private set; }

		public RequestInfo(RequestState requestState, UInt64 requestID, string requestToken)
		{
			RequestState = requestState;
			RequestID = requestID;
			RequestToken = RequestToken;
		}
	}
}
