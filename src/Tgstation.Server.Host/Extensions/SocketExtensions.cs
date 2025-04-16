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
		/// Attempt to exclusively bind to a given <paramref name="endPoint"/>.
		/// </summary>
		/// <param name="platformIdentifier">The <see cref="PlatformIdentifier"/> to use.</param>
		/// <param name="endPoint">The <see cref="IPEndPoint"/> to bind to.</param>
		/// <param name="udp">If we're bind testing for UDP. If <see langword="false"/> TCP will be checked.</param>
		public static void BindTest(IPlatformIdentifier platformIdentifier, IPEndPoint endPoint, bool udp)
		{
			ArgumentNullException.ThrowIfNull(platformIdentifier);
			ProcessExecutor.WithProcessLaunchExclusivity(() =>
			{
				var ipAddress = endPoint.Address;
				var ipv6 = ipAddress.AddressFamily == AddressFamily.InterNetworkV6;
				using var socket = new Socket(
					ipv6
						? AddressFamily.InterNetworkV6
						: AddressFamily.InterNetwork,
					udp
						? SocketType.Dgram
						: SocketType.Stream,
					udp
						? ProtocolType.Udp
						: ProtocolType.Tcp);

				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
				if (!udp && platformIdentifier.IsWindows)
					socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

				if (ipv6)
					socket.DualMode = true;

				socket.Bind(endPoint);
			});
		}
	}
}
