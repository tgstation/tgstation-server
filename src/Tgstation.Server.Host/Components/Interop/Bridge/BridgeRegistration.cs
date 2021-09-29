using System;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <inheritdoc />
	sealed class BridgeRegistration : IBridgeRegistration
	{
		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for accessing <see cref="onDispose"/>.
		/// </summary>
		readonly object lockObject;

		/// <summary>
		/// <see cref="Action"/> to run when <see cref="Dispose"/>d.
		/// </summary>
		Action? onDispose;

		/// <summary>
		/// Initializes a new instance of the <see cref="BridgeRegistration"/> class.
		/// </summary>
		/// <param name="onDispose">The value of <see cref="onDispose"/>.</param>
		public BridgeRegistration(Action onDispose)
		{
			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
			lockObject = new object();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (lockObject)
			{
				onDispose?.Invoke();
				onDispose = null;
			}
		}
	}
}
