using System;
using System.ServiceModel;

namespace TGServiceInterface
{
	/// <summary>
	/// Interface for managing the service
	/// </summary>
	[ServiceContract]
	public interface ITGSService
	{

		/// <summary>
		/// Next stop of the service will not close DD and sets a flag for it to reattach once it restarts
		/// </summary>
		[OperationContract]
		void PrepareForUpdate();

		/// <summary>
		/// Retrieve's the service's version
		/// </summary>
		/// <returns>The service's version</returns>
		[OperationContract]
		string Version();
	}
}
