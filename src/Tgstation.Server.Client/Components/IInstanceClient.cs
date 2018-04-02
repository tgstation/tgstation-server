using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// <see cref="IClient{TRights}"/> for server instances
	/// </summary>
	public interface IInstanceClient : IClient<InstanceRights>
	{
		/// <summary>
		/// The <see cref="Instance"/> of the <see cref="IInstanceClient"/>
		/// </summary>
		Instance Metadata { get; }

		/// <summary>
		/// Access the <see cref="IByondClient"/>
		/// </summary>
		IByondClient Byond { get; }
	}
}