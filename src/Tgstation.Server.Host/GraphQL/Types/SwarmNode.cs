using System;
using System.Linq;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types.Relay;

using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.GraphQL.Interfaces;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a node server in a swarm.
	/// </summary>
	[Node]
	public sealed class SwarmNode : IServerNode
	{
		/// <summary>
		/// The node ID.
		/// </summary>
		[ID]
		public string NodeId => Identifier;

		/// <summary>
		/// The swarm server ID.
		/// </summary>
		public string Identifier { get; }

		/// <summary>
		/// The swarm server's internal <see cref="Uri"/>.
		/// </summary>
		public Uri Address { get; }

		/// <summary>
		/// The swarm server's optional public address.
		/// </summary>
		public Uri? PublicAddress { get; }

		/// <summary>
		/// Whether or not the server is the swarm's controller.
		/// </summary>
		public bool Controller { get; }

		/// <summary>
		/// Node resolver for <see cref="SwarmNode"/>s.
		/// </summary>
		/// <param name="identifier">The <see cref="Identifier"/>.</param>
		/// <param name="authorizationService">The <see cref="IAuthorizationService"/> to use.</param>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to load from.</param>
		/// <returns>A new <see cref="SwarmNode"/> with the matching <paramref name="identifier"/> if found, <see langword="null"/> otherwise.</returns>
		[Authorize]
		public static async ValueTask<SwarmNode?> GetSwarmNode(
			string identifier,
			[Service] IAuthorizationService authorizationService,
			[Service] ISwarmService swarmService)
		{
			ArgumentNullException.ThrowIfNull(identifier);
			ArgumentNullException.ThrowIfNull(authorizationService);
			ArgumentNullException.ThrowIfNull(swarmService);

			await authorizationService.CheckGraphQLAuthorized();
			var info = swarmService
				.GetSwarmServers()
				?.FirstOrDefault(x => x.Identifier == identifier);

			if (info == null)
				return null;

			return new SwarmNode(info);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmNode"/> class.
		/// </summary>
		/// <param name="nodeInformation">The <see cref="SwarmServerInformation"/> to use for initialization.</param>
		public SwarmNode(SwarmServerInformation? nodeInformation)
		{
			ArgumentNullException.ThrowIfNull(nodeInformation);

			Identifier = nodeInformation.Identifier!;
			Address = nodeInformation.Address!;
			PublicAddress = nodeInformation.PublicAddress;
			Controller = nodeInformation.Controller;
		}

		/// <summary>
		/// Gets the <see cref="SwarmNode"/>'s <see cref="IGateway"/>.
		/// </summary>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the current <see cref="SwarmConfiguration"/>.</param>
		/// <returns>A new <see cref="IGateway"/>.</returns>
		/// <remarks>The <see cref="SwarmNode"/>'s <see cref="IGateway"/>.</remarks>
		public IGateway Gateway([Service] IOptionsSnapshot<SwarmConfiguration> swarmConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(swarmConfigurationOptions);

			bool local = Identifier == swarmConfigurationOptions.Value.Identifier;
			if (local)
				return new LocalGateway();

			throw new ErrorMessageException(ErrorCode.RemoteGatewaysNotImplemented);

			// return new RemoteGateway();
		}
	}
}
