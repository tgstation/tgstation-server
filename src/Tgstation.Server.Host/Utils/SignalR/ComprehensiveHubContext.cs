using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Utils.SignalR
{
	/// <summary>
	/// An implementation of <see cref="IHubContext{THub}"/> with <see cref="User"/> connection ID mapping.
	/// </summary>
	/// <typeparam name="THub">The <see cref="Hub"/> the <see cref="ComprehensiveHubContext{THub, THubMethods}"/> is for.</typeparam>
	/// <typeparam name="THubMethods">The interface <see cref="IErrorHandlingHub"/> for implementing <see cref="Hub{T}"/> methods.</typeparam>
	sealed class ComprehensiveHubContext<THub, THubMethods> : IConnectionMappedHubContext<THub, THubMethods>, IHubConnectionMapper<THub, THubMethods>, IRestartHandler
		where THub : ConnectionMappingHub<THub, THubMethods>
		where THubMethods : class, IErrorHandlingHub
	{
		/// <inheritdoc />
		public IHubClients<THubMethods> Clients => wrappedHubContext.Clients;

		/// <inheritdoc />
		public IGroupManager Groups => wrappedHubContext.Groups;

		/// <summary>
		/// The <see cref="IHubContext{THub}"/> being wrapped.
		/// </summary>
		readonly IHubContext<THub, THubMethods> wrappedHubContext;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ComprehensiveHubContext{THub, THubMethods}"/>.
		/// </summary>
		readonly ILogger<ComprehensiveHubContext<THub, THubMethods>> logger;

		/// <summary>
		/// Map of <see cref="User"/> <see cref="Api.Models.EntityId.Id"/>s to their associated <see cref="HubCallerContext"/>s.
		/// </summary>
		readonly ConcurrentDictionary<long, Dictionary<string, HubCallerContext>> userConnections;

		/// <inheritdoc />
		public event Func<IAuthenticationContext, CancellationToken, ValueTask<IEnumerable<string>>> OnConnectionMapGroups;

		/// <summary>
		/// Initializes a new instance of the <see cref="ComprehensiveHubContext{THub, THubMethods}"/> class.
		/// </summary>
		/// <param name="wrappedHubContext">The value of <see cref="wrappedHubContext"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> to <see cref="IServerControl.RegisterForRestart(IRestartHandler)"/> with.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public ComprehensiveHubContext(
			IHubContext<THub, THubMethods> wrappedHubContext,
			IServerControl serverControl,
			ILogger<ComprehensiveHubContext<THub, THubMethods>> logger)
		{
			this.wrappedHubContext = wrappedHubContext ?? throw new ArgumentNullException(nameof(wrappedHubContext));
			ArgumentNullException.ThrowIfNull(serverControl);
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			userConnections = new ConcurrentDictionary<long, Dictionary<string, HubCallerContext>>();

			serverControl.RegisterForRestart(this);
		}

		/// <inheritdoc />
		public List<string> UserConnectionIds(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			var connectionIds = userConnections.GetOrAdd(user.Id.Value, _ => new Dictionary<string, HubCallerContext>());
			lock (connectionIds)
				return connectionIds.Keys.ToList();
		}

		/// <inheritdoc />
		public async ValueTask UserConnected(IAuthenticationContext authenticationContext, THub hub, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(authenticationContext);
			ArgumentNullException.ThrowIfNull(hub);

			var userId = authenticationContext.User.Id.Value;
			var context = hub.Context;
			logger.LogTrace(
				"Mapping user {userId} to hub connection ID: {connectionId}",
				userId,
				context.ConnectionId);

			var mappedGroupsTask = OnConnectionMapGroups(authenticationContext, cancellationToken);
			userConnections.AddOrUpdate(
				userId,
				_ => new Dictionary<string, HubCallerContext>
				{
					{ context.ConnectionId, context },
				},
				(_, old) =>
				{
					lock (old)
						old[context.ConnectionId] = context;

					return old;
				});

			var mappedGroups = await mappedGroupsTask;
			await Task.WhenAll(
				mappedGroups.Select(
					group => hub.Groups.AddToGroupAsync(context.ConnectionId, group, cancellationToken)));
		}

		/// <inheritdoc />
		public void UserDisconnected(string connectionId)
		{
			ArgumentNullException.ThrowIfNull(connectionId);
			foreach (var kvp in userConnections)
				lock (kvp.Value)
					if (kvp.Value.Remove(connectionId))
						logger.LogTrace("User {userId} disconnected connection ID: {connectionId}", kvp.Key, connectionId);
		}

		/// <inheritdoc />
		public ValueTask NotifyAndAbortUnauthedConnections(User user, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);
			logger.LogTrace("NotifyAndAbortUnauthedConnections. UID {userId}", user.Id.Value);

			List<HubCallerContext> connections = null;
			userConnections.AddOrUpdate(
				user.Id.Value,
				_ => new Dictionary<string, HubCallerContext>(),
				(_, old) =>
				{
					lock (old)
					{
						connections = old.Values.ToList();
						old.Clear();
					}

					return old;
				});

			async ValueTask NotifyAndAbortConnection(HubCallerContext context)
			{
				await Clients
					.Client(context.ConnectionId)
					.AbortingConnection(ConnectionAbortReason.TokenInvalid, cancellationToken);
				context.Abort();
			}

			return ValueTaskExtensions.WhenAll(connections.Select(NotifyAndAbortConnection));
		}

		/// <inheritdoc />
		public async ValueTask HandleRestart(Version updateVersion, bool handlerMayDelayShutdownWithExtremelyLongRunningTasks, CancellationToken cancellationToken)
		{
			logger.LogTrace("HandleRestart. {connectionCount} active connections", userConnections.Count);
			await Clients.All.AbortingConnection(ConnectionAbortReason.ServerRestart, cancellationToken);
			userConnections.Clear();
		}
	}
}
