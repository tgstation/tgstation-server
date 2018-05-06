using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For retrieving current <see cref="DreamDaemonLaunchParameters"/>
	/// </summary>
	interface ILaunchParametersFactory
	{
		/// <summary>
		/// Get the latest <see cref="DreamDaemonLaunchParameters"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the latest <see cref="DreamDaemonLaunchParameters"/></returns>
		Task<DreamDaemonLaunchParameters> GetLaunchParameters(CancellationToken cancellationToken);
	}
}
