using System.ServiceModel;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Used for testing connections to the service without authentication
	/// </summary>
	[ServiceContract]
	public interface ITGConnectivity
	{
		/// <summary>
		/// Does nothing on the server end, but if the call completes, you can be sure you are connected. WCF won't throw until you try until you actually use the API
		/// </summary>
		[OperationContract]
		void VerifyConnection();
	}
}
