using System;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class InteropContext : IInteropContext
	{
		/// <summary>
		/// The <see cref="Dispose"/> <see cref="Action"/>
		/// </summary>
		Action onDispose;

		/// <summary>
		/// Construct an <see cref="InteropContext"/>
		/// </summary>
		/// <param name="onDispose">The value of <see cref="onDispose"/></param>
		public InteropContext(Action onDispose)
		{
			this.onDispose = onDispose;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			onDispose?.Invoke();
			onDispose = null;
		}
	}
}