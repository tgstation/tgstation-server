using System;
using System.Net;
using System.Net.Sockets;

using Tgstation.Server.Host.System;

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
		/// <param name="platformIdentifier">The <see cref="PlatformIdentifier"/> to use.</param>
		/// <param name="port">The port number to bind to.</param>
		/// <param name="includeIPv6">If IPV6 should be tested as well.</param>
		public static void BindTest(IPlatformIdentifier platformIdentifier, ushort port, bool includeIPv6)
		{
			ArgumentNullException.ThrowIfNull(platformIdentifier);
			ProcessExecutor.WithProcessLaunchExclusivity(() =>
			{
				using var socket = new Socket(
					includeIPv6
						? AddressFamily.InterNetworkV6
						: AddressFamily.InterNetwork,
					SocketType.Stream,
					ProtocolType.Tcp);

				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
				if (platformIdentifier.IsWindows)
					socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

				if (includeIPv6)
					socket.DualMode = true;

				socket.Bind(
					new IPEndPoint(
						includeIPv6
							? IPAddress.IPv6Any
							: IPAddress.Any,
						port));
			});
		}
	}
}
