using System;
using System.Net;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Provides access to the server's <see cref="HttpBindAddress"/>.
	/// </summary>
	public interface IServerAddressProvider
	{
		/// <summary>
		/// The bind address the server listens on as a URI ex: 127.0.0.1:1000 .
		/// </summary>
		Uri HttpBindAddress { get; }

		/// <summary>
		/// The bind address the server listens on, as an IPEndPoint.
		/// </summary>
		IPEndPoint AddressEndPoint { get; }
	}
}
