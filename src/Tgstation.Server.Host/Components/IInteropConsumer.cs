using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Consumes interop events
	/// </summary>
	interface IInteropConsumer
	{
		/// <summary>
		/// Handle an interop event
		/// </summary>
		/// <param name="query">The query from the world/Export request</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a jsonifiable object graph</returns>
		Task<object> HandleInterop(IQueryCollection query, CancellationToken cancellationToken);
	}
}