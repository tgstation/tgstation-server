using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Authorization;

using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.GraphQL.Interfaces;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a tgstation-server swarm.
	/// </summary>
	public sealed class ServerSwarm
	{
		/// <summary>
		/// Access all instances in the <see cref="ServerSwarm"/>.
		/// </summary>
		/// <returns>Queryable <see cref="Instance"/> in the <see cref="ServerSwarm"/>.</returns>
		[Authorize]
		public IQueryable<Instance> Instances()
			=> throw new ErrorMessageException(ErrorCode.RemoteGatewaysNotImplemented);

		/// <summary>
		/// Gets the swarm protocol major version in use.
		/// </summary>
		/// <returns>The swarm protocol major version in use.</returns>
		[Authorize]
		public int ProtocolMajorVersion()
			=> Version.Parse(MasterVersionsAttribute.Instance.RawSwarmProtocolVersion).Major;

		/// <summary>
		/// Gets the swarm's <see cref="Types.Users"/>.
		/// </summary>
		/// <returns>A new <see cref="Types.Users"/>.</returns>
		[Authorize]
		public Users Users() => new();

		/// <summary>
		/// Gets the connected <see cref="SwarmNode"/> server.
		/// </summary>
		/// <param name="authorizationService">The <see cref="IAuthorizationService"/> to use.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptionsSnapshot{TOptions}"/> containing the current <see cref="SwarmConfiguration"/>.</param>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use.</param>
		/// <returns>A new <see cref="SwarmNode"/> for the local node if it is part of a swarm, <see langword="null"/> otherwise.</returns>
		public async ValueTask<IServerNode> CurrentNode(
			[Service] IAuthorizationService authorizationService,
			[Service] IOptionsSnapshot<SwarmConfiguration> swarmConfigurationOptions,
			[Service] ISwarmService swarmService)
		{
			ArgumentNullException.ThrowIfNull(authorizationService);
			ArgumentNullException.ThrowIfNull(swarmConfigurationOptions);
			ArgumentNullException.ThrowIfNull(swarmService);

			if (swarmConfigurationOptions.Value.PrivateKey == null)
				return new StandaloneNode();

			return ((IServerNode?)await SwarmNode.GetSwarmNode(
				swarmConfigurationOptions.Value.Identifier!,
				authorizationService,
				swarmService)) ?? new StandaloneNode();
		}

		/// <summary>
		/// Gets all <see cref="SwarmNode"/> servers in the swarm.
		/// </summary>
		/// <param name="authorizationService">The <see cref="IAuthorizationService"/> to use.</param>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use.</param>
		/// <returns>A <see cref="List{T}"/> of <see cref="SwarmNode"/>s if the local node is part of a swarm, <see langword="null"/> otherwise.</returns>
		[Authorize]
		public async ValueTask<List<SwarmNode>?> Nodes(
			[Service] IAuthorizationService authorizationService,
			[Service] ISwarmService swarmService)
		{
			ArgumentNullException.ThrowIfNull(authorizationService);
			ArgumentNullException.ThrowIfNull(swarmService);

			await authorizationService.CheckGraphQLAuthorized();

			return swarmService.GetSwarmServers()?.Select(x => new SwarmNode(x)).ToList();
		}

		/// <summary>
		/// Gets the <see cref="Types.UpdateInformation"/> for the swarm.
		/// </summary>
		/// <param name="authorizationService">The <see cref="IAuthorizationService"/> to use.</param>
		/// <returns>A new <see cref="Types.UpdateInformation"/>.</returns>
		[Authorize]
		public async ValueTask<UpdateInformation> UpdateInformation(
			[Service] IAuthorizationService authorizationService)
		{
			await authorizationService.CheckGraphQLAuthorized();
			return new();
		}
	}
}
