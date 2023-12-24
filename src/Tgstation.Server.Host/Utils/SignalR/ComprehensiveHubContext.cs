using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Utils.SignalR
{
	/// <summary>
	/// An implementation of <see cref="IHubContext{THub}"/> with <see cref="User"/> connection ID mapping.
	/// </summary>
	/// <typeparam name="THub">The <see cref="Hub"/> the <see cref="ComprehensiveHubContext{THub, THubMethods}"/> is for.</typeparam>
	/// <typeparam name="THubMethods">The <see langword="interface"/> for implementing <see cref="Hub{T}"/> methods.</typeparam>
	sealed class ComprehensiveHubContext<THub, THubMethods> : IConnectionMappedHubContext<THub, THubMethods>, IHubConnectionMapper<THub, THubMethods>
		where THub : ConnectionMappingHub<THub, THubMethods>
		where THubMethods : class
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
		public event Func<IAuthenticationContext, Func<IEnumerable<string>, Task>, CancellationToken, ValueTask>? OnConnectionMapGroups;

		/// <summary>
		/// Initializes a new instance of the <see cref="ComprehensiveHubContext{THub, THubMethods}"/> class.
		/// </summary>
		/// <param name="wrappedHubContext">The value of <see cref="wrappedHubContext"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public ComprehensiveHubContext(
			IHubContext<THub, THubMethods> wrappedHubContext,
			ILogger<ComprehensiveHubContext<THub, THubMethods>> logger)
		{
			this.wrappedHubContext = wrappedHubContext ?? throw new ArgumentNullException(nameof(wrappedHubContext));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			userConnections = new ConcurrentDictionary<long, Dictionary<string, HubCallerContext>>();
		}

		/// <inheritdoc />
		public List<string> UserConnectionIds(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			var connectionIds = userConnections.GetOrAdd(user.Require(x => x.Id), _ => new Dictionary<string, HubCallerContext>());
			lock (connectionIds)
				return connectionIds.Keys.ToList();
		}

		/// <inheritdoc />
		public ValueTask UserConnected(IAuthenticationContext authenticationContext, THub hub, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(authenticationContext);
			ArgumentNullException.ThrowIfNull(hub);

			var userId = authenticationContext.User.Require(x => x.Id);
			var context = hub.Context;
			logger.LogTrace(
				"Mapping user {userId} to hub connection ID: {connectionId}",
				userId,
				context.ConnectionId);

			var mappingTask = OnConnectionMapGroups?.Invoke(
				authenticationContext,
				mappedGroups =>
				{
					mappedGroups = mappedGroups.ToList();
					logger.LogTrace(
						"Mapping connection ID {connectionId} with groups: {mappedGroups}",
						context.ConnectionId,
						String.Join(", ", mappedGroups));
					return Task.WhenAll(
						mappedGroups.Select(
							group => hub.Groups.AddToGroupAsync(context.ConnectionId, group, cancellationToken)));
				},
				cancellationToken)
				?? ValueTask.CompletedTask;
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

			return mappingTask;
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
		public void AbortUnauthedConnections(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			var uid = user.Require(x => x.Id);
			logger.LogTrace("NotifyAndAbortUnauthedConnections. UID {userId}", uid);

			List<HubCallerContext>? connections = null;
			userConnections.AddOrUpdate(
				uid,
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

			foreach (var context in connections!)
				context.Abort();
		}
	}
}
