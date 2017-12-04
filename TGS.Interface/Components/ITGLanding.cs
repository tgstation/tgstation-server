using System.Collections.Generic;
using System.Threading.Tasks;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Used for general authentication and listing <see cref="ITGInstance"/>s
	/// </summary>
	public interface ITGLanding : ITGComponent
	{
		/// <summary>
		/// List instances that the caller can access
		/// </summary>
		/// <returns>A <see cref="Task"/> that results in a <see cref="IDictionary{TKey, TValue}"/> of instance names relating to their paths</returns>
		Task<IList<InstanceMetadata>> ListInstances();
	}
}
