using System;

using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <inheritdoc />
	sealed class BridgeRegistration : DisposeInvoker, IBridgeRegistration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BridgeRegistration"/> class.
		/// </summary>
		/// <param name="disposeAction">The <see cref="IDisposable.Dispose"/> action for the <see cref="DisposeInvoker"/>.</param>
		public BridgeRegistration(Action disposeAction)
			: base(disposeAction)
		{
		}
	}
}
