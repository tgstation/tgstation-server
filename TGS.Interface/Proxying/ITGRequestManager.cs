using System.Collections.Generic;
using System.ServiceModel;

namespace TGS.Interface.Proxying
{
	[ServiceContract]
	public interface ITGRequestManager
	{
		[OperationContract]
		RequestInfo BeginRequest(string componentName, string methodName, object[] parameters, string user, string password);

		[OperationContract]
		List<RequestInfo> QueryRequests(IEnumerable<RequestInfo> requestInfos);

		[OperationContract]
		object EndRequest(RequestInfo requestInfo);
	}
}
