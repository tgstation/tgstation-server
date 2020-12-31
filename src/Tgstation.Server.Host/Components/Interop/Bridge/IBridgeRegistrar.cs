namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// Registers <see cref="IBridgeHandler"/>s.
	/// </summary>
	interface IBridgeRegistrar
	{
		/// <summary>
		/// Register a given <paramref name="bridgeHandler"/>.
		/// </summary>
		/// <param name="bridgeHandler">The <see cref="IBridgeHandler"/> to register.</param>
		/// <returns>A representative <see cref="IBridgeRegistration"/>.</returns>
		IBridgeRegistration RegisterHandler(IBridgeHandler bridgeHandler);
	}
}
