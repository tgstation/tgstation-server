using System;

using Grpc.Net.Client;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Factory for creating <see cref="GrpcChannel"/>.
	/// </summary>
	interface IGrpcChannelFactory
	{
		/// <summary>
		/// Create a <see cref="GrpcChannel"/> for a given <paramref name="address"/> that uses a given <paramref name="authorization"/> header.
		/// </summary>
		/// <param name="address">The <see cref="Uri"/> to connect to.</param>
		/// <param name="authorization">A <see cref="Func{TResult}"/> which provides the current Authorization header value.</param>
		/// <returns>A new <see cref="GrpcChannel"/>.</returns>
		GrpcChannel CreateChannel(Uri address, Func<string> authorization);
	}
}
