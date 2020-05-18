using Microsoft.Extensions.Hosting;
using Tgstation.Server.Host.Components.Interop.Bridge;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Factory for creating <see cref="IInstance"/>s
	/// </summary>
	interface IInstanceFactory : IHostedService
	{
		/// <summary>
		/// Create an <see cref="IInstance"/>
		/// </summary>
		/// <param name="bridgeRegistrar">The <see cref="IBridgeRegistrar"/> to use.</param>
		/// <param name="metadata">The <see cref="Models.Instance"/></param>
		/// <returns>A new <see cref="IInstance"/></returns>
		IInstance CreateInstance(IBridgeRegistrar bridgeRegistrar, Models.Instance metadata);
	}
}