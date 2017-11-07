using System.Collections.Generic;
using System.ServiceModel;

namespace TGServiceInterface.Components
{
	/// <summary>
	/// Used for general authentication and listing <see cref="ITGInstance"/>s
	/// </summary>
	public interface ITGLanding
	{
		/// <summary>
		/// Retrieve's the service's version
		/// </summary>
		/// <returns>The service's version</returns>
		[OperationContract]
		string Version();

		/// <summary>
		/// List instances that the caller can access
		/// </summary>
		/// <returns>A <see cref="IDictionary{TKey, TValue}"/> of instance names relating to their paths</returns>
		[OperationContract]
		IList<InstanceMetadata> ListInstances();
	}
}
