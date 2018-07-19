using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Factory for creating <see cref="IInstance"/>s
	/// </summary>
	interface IInstanceFactory
	{
		/// <summary>
		/// Create an <see cref="IInstance"/>
		/// </summary>
		/// <param name="metadata">The <see cref="Models.Instance"/></param>
		/// <param name="interopRegistrar">The <see cref="IInteropRegistrar"/> for the <see cref="IInstance"/></param>
		/// <param name="reattachInfoHandler">The <see cref="IReattachInfoHandler"/> for the <see cref="IInstance"/></param>
		/// <returns>A new <see cref="IInstance"/></returns>
		IInstance CreateInstance(Models.Instance metadata, IInteropRegistrar interopRegistrar, IReattachInfoHandler reattachInfoHandler);
	}
}