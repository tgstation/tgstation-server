using System;

using Grpc.Core;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Factory for creating <see cref="CallInvoker"/>s.
	/// </summary>
	interface ICallInvokerlFactory
	{
		/// <summary>
		/// Create a <see cref="CallInvoker"/> for a given <paramref name="address"/> that uses a given <paramref name="authorization"/> header.
		/// </summary>
		/// <param name="address">The <see cref="Uri"/> to connect to.</param>
		/// <param name="authorization">A <see cref="Func{TResult}"/> which provides the current Authorization header value.</param>
		/// <returns>A new <see cref="CallInvoker"/>.</returns>
		CallInvoker CreateCallInvoker(Uri address, Func<string> authorization);
	}
}
