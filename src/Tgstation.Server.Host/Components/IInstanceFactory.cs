using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.IO;

#nullable disable

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Factory for creating <see cref="IInstance"/>s.
	/// </summary>
	interface IInstanceFactory : IComponentService
	{
		/// <summary>
		/// Create an <see cref="IInstance"/>.
		/// </summary>
		/// <param name="bridgeRegistrar">The <see cref="IBridgeRegistrar"/> to use.</param>
		/// <param name="metadata">The <see cref="Models.Instance"/>.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IInstance"/>.</returns>
		ValueTask<IInstance> CreateInstance(IBridgeRegistrar bridgeRegistrar, Models.Instance metadata);

		/// <summary>
		/// Create an <see cref="IIOManager"/> that resolves to the "Game" directory of the <see cref="Models.Instance"/> defined by <paramref name="metadata"/>.
		/// </summary>
		/// <param name="metadata">The <see cref="Models.Instance"/>.</param>
		/// <returns>The <see cref="IIOManager"/> for the instance's "Game" directory.</returns>
		IIOManager CreateGameIOManager(Models.Instance metadata);
	}
}
