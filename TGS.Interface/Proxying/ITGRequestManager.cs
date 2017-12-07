using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace TGS.Interface.Proxying
{
	[ServiceContract]
	public interface ITGRequestManager
	{
		/// <summary>
		/// Retrieve's the server's version
		/// </summary>
		/// <returns>A <see cref="Task"/> that results in the server's version <see cref="string"/></returns>
		[OperationContract]
		Version Version();

		[OperationContract]
		RequestInfo BeginRequest(string componentName, string methodName, IEnumerable<object> parameters, string user, string password);

		[OperationContract]
		List<RequestInfo> QueryRequests(IEnumerable<RequestInfo> requestInfos);

		[OperationContract]
		object EndRequest(RequestInfo requestInfo);
	}
}
