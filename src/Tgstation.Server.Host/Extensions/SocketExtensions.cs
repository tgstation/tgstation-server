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
			using var socket = new Socket(
				includeIPv6
					? AddressFamily.InterNetworkV6
					: AddressFamily.InterNetwork,
				SocketType.Stream,
				ProtocolType.Tcp);

			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
			if (includeIPv6)
				socket.DualMode = true;

			socket.Bind(
				new IPEndPoint(
					includeIPv6
						? IPAddress.IPv6Any
						: IPAddress.Any,
					port));

			// BY ALL KNOWN LAWS OF AVIATION THERE'S NO FUCKING WAY THIS CALL SHOULD BE NECESSARY
			// YET, WITHOUT IT, OPENDREAM WILL RUN INTO PORT REUSE ISSUES
			// CTRL-CLICK IT THOUGH, ALL IT DOES IS CALL THE GODDAMN FUCKING DISPOSE()
			// AND THE DISPOSE() CAN'T BE DOUBLE CALLED DUE TO ATOMICS
			// REMOVE IT THOUGH, RUN THE TESTS ON A FAST PC AND THEY WILL FAIL DUE TO ADDRESS REUSE
			// HORY FUCKING SHIT
			socket.Close();
		}
	}
}
