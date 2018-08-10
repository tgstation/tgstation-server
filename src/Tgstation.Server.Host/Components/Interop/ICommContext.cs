using System;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents a registration of an interop session
	/// </summary>
	interface ICommContext : IDisposable
	{
		/// <summary>
		/// Register a <paramref name="handler"/> with the <see cref="ICommContext"/>
		/// </summary>
		/// <param name="handler">The <see cref="ICommHandler"/> to register</param>
		void RegisterHandler(ICommHandler handler);
	}
}