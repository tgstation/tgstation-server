using System;
using System.Runtime.Serialization;

namespace TGS.Interface.Proxying
{
	[DataContract]
	public sealed class RequestInfo
	{
		[DataMember]
		public RequestState RequestState { get; private set; }
		[DataMember]
		public UInt64 RequestID { get; private set; }
		[DataMember]
		public string RequestToken { get; private set; }

		public RequestInfo(RequestState requestState, UInt64 requestID, string requestToken)
		{
			RequestState = requestState;
			RequestID = requestID;
			RequestToken = requestToken;
		}
	}
}
