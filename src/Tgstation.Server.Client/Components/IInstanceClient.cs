using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// <see cref="IClient{TRights, TModel}"/> for server instances
	/// </summary>
	public interface IInstanceClient : IClient<InstanceRights, Instance>
	{
		/// <summary>
		/// Access the <see cref="IByondClient"/>
		/// </summary>
		IByondClient Byond { get; }
	}
}