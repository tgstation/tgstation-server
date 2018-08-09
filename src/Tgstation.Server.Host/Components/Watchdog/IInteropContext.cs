using System;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Represents a registration of an interop session
	/// </summary>
	interface IInteropContext : IDisposable
	{
		/// <summary>
		/// Register a <paramref name="handler"/> with the <see cref="IInteropContext"/>
		/// </summary>
		/// <param name="handler">The <see cref="IInteropHandler"/> to register</param>
		void RegisterHandler(IInteropHandler handler);
	}
}