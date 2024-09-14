using System;

using Tgstation.Server.Host.GraphQL.Interfaces;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// <see cref="IGateway"/> for accessing remote <see cref="SwarmNode"/>s.
	/// </summary>
	/// <remarks>This is currently unimplemented.</remarks>
	public sealed class RemoteGateway : IGateway
	{
		/// <inheritdoc />
		public GatewayInformation Information() => throw new NotImplementedException();
	}
}
