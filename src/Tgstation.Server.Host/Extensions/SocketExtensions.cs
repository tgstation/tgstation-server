using System;
using System.Net;
using System.Net.Sockets;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="Socket"/> class.
	/// </summary>
	static class SocketExtensions
	{
		/// <summary>
		/// Attempt to exclusively bind to a given <paramref name="port"/>.
		/// </summary>
		/// <param name="port">The port number to bind to.</param>
		/// <param name="includeIPv6">If IPV6 should be tested as well.</param>
		public static void BindTest(ushort port, bool includeIPv6)
		{
			void BindSocket(bool forCleanup)
			{
				using var socket = new Socket(
					includeIPv6
						? AddressFamily.InterNetworkV6
						: AddressFamily.InterNetwork,
					SocketType.Stream,
					ProtocolType.Tcp);

				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, !forCleanup);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, forCleanup);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
				if (includeIPv6)
					socket.DualMode = true;

				socket.Bind(
					new IPEndPoint(
						includeIPv6
							? IPAddress.IPv6Any
							: IPAddress.Any,
						port));
			}

			GC.Collect(Int32.MaxValue, GCCollectionMode.Forced, true, false);
			BindSocket(false);
			BindSocket(true);
			GC.Collect(Int32.MaxValue, GCCollectionMode.Forced, true, false);
		}
	}
}
