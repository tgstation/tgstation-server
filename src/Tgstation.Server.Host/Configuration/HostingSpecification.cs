using System.Net;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Defines an IP endpoint a service is listening on.
	/// </summary>
	public sealed class HostingSpecification
	{
		/// <summary>
		/// The <see cref="global::System.Net.IPAddress"/> to listen on. If <see langword="null"/>, <see cref="global::System.Net.IPAddress.Any"/> will be used.
		/// </summary>
		public string? IPAddress { get; set; }

		/// <summary>
		/// The port the service will be hosted on.
		/// </summary>
		public ushort Port { get; set; }

		/// <summary>
		/// Get the <see cref="IPEndPoint"/> from the <see cref="HostingSpecification"/>.
		/// </summary>
		/// <returns>The <see cref="IPEndPoint"/> the <see cref="HostingSpecification"/> represents.</returns>
		public IPEndPoint ParseIPEndPoint()
			=> new(ParseIPAddress(), Port);

		/// <summary>
		/// Parse the hosting specification used <see cref="Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.RequireHost{TBuilder}(TBuilder, string[])"/>.
		/// </summary>
		/// <returns>The parsed host <see cref="string"/>.</returns>
		public string ParseEndPointSpecification()
		{
			var ipAddress = ParseIPAddress();
			if (ipAddress == global::System.Net.IPAddress.Any)
				return $"*:{Port}";

			return $"{ipAddress}:{Port}";
		}

		/// <summary>
		/// Parse <see cref="IPAddress"/> into a <see cref="global::System.Net.IPAddress"/>.
		/// </summary>
		/// <returns>The parsed <see cref="global::System.Net.IPAddress"/>.</returns>
		IPAddress ParseIPAddress()
		{
			var ipAddress = IPAddress == null
				? global::System.Net.IPAddress.Any
				: global::System.Net.IPAddress.Parse(IPAddress);

			return ipAddress;
		}
	}
}
