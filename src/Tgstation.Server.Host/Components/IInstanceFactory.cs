using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Tgstation.Server.Host.Components.Interop.Bridge;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Factory for creating <see cref="IInstance"/>s.
	/// </summary>
	interface IInstanceFactory : IHostedService
	{
		/// <summary>
		/// Create an <see cref="IInstance"/>.
		/// </summary>
		/// <param name="bridgeRegistrar">The <see cref="IBridgeRegistrar"/> to use.</param>
		/// <param name="metadata">The <see cref="Models.Instance"/>.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IInstance"/>.</returns>
		Task<IInstance> CreateInstance(IBridgeRegistrar bridgeRegistrar, Models.Instance metadata);
	}
}
