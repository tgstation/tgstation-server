using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing a single <see cref="Instance"/>
	/// </summary>
	public interface IInstanceClient : IRightsClient<InstanceRights>
	{
		/// <summary>
		/// Get the <see cref="Instance"/> represented by the <see cref="IInstanceClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Instance"/> represented by the <see cref="IInstanceClient"/></returns>
		Task<Instance> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Access the <see cref="IByondClient"/>
		/// </summary>
		IByondClient Byond { get; }

		/// <summary>
		/// Access the <see cref="IRepositoryClient"/>
		/// </summary>
		IRepositoryClient Repository { get; }
	}
}