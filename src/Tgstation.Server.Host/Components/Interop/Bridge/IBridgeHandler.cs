namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <inheritdoc />
	interface IBridgeHandler : IBridgeDispatcher
	{
		/// <summary>
		/// The <see cref="DMApiParameters"/> for the <see cref="IBridgeHandler"/>.
		/// </summary>
		DMApiParameters DMApiParameters { get; }
	}
}
