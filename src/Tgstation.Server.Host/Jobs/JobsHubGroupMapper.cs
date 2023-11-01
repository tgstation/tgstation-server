using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils.SignalR;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Handles mapping groups for the <see cref="JobsHub"/>.
	/// </summary>
	sealed class JobsHubGroupMapper : IPermissionsUpdateNotifyee
	{
		/// <summary>
		/// The <see cref="IHubContext"/> for the <see cref="JobsHub"/>.
		/// </summary>
		readonly IConnectionMappedHubContext<JobsHub, IJobsHub> hub;

		/// <summary>
		/// The <see cref="IServiceProvider"/> for the <see cref="JobService"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="JobService"/>.
		/// </summary>
		readonly ILogger<JobsHubGroupMapper> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobsHubGroupMapper"/> class.
		/// </summary>
		/// <param name="hub">The value of <see cref="hub"/>.</param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public JobsHubGroupMapper(IConnectionMappedHubContext<JobsHub, IJobsHub> hub, IDatabaseContextFactory databaseContextFactory, ILogger<JobsHubGroupMapper> logger)
		{
			this.hub = hub ?? throw new ArgumentNullException(nameof(hub));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			hub.OnConnectionMapGroups += MapConnectionGroups;
		}

		/// <inheritdoc />
		public ValueTask InstancePermissionSetCreated(InstancePermissionSet instancePermissionSet, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(instancePermissionSet);
			var permissionSetId = instancePermissionSet.PermissionSet.Id ?? instancePermissionSet.PermissionSetId;

			logger.LogTrace("InstancePermissionSetCreated");
			return RefreshHubGroups(
				permissionSetId,
				cancellationToken);
		}

		/// <inheritdoc />
		public ValueTask UserDisabled(User user, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);
			if (!user.Id.HasValue)
				throw new InvalidOperationException("user.Id was null!");

			logger.LogTrace("UserDisabled");
			return hub.NotifyAndAbortUnauthedConnections(user, cancellationToken);
		}

		/// <inheritdoc />
		public ValueTask InstancePermissionSetDeleted(PermissionSet permissionSet, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(permissionSet);
			logger.LogTrace("InstancePermissionSetDeleted");
			return RefreshHubGroups(
				permissionSet.Id ?? throw new InvalidOperationException("permissionSet?.Id was null!"),
				cancellationToken);
		}

		/// <summary>
		/// Implementation of <see cref="IConnectionMappedHubContext{THub, THubMethods}.OnConnectionMapGroups"/>.
		/// </summary>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> to map the groups for.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in an <see cref="IEnumerable{T}"/> of the <see cref="JobsHub"/> group names the user belongs in.</returns>
		async ValueTask<IEnumerable<string>> MapConnectionGroups(IAuthenticationContext authenticationContext, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(authenticationContext);

			List<long> permedInstanceIds = null;
			await databaseContextFactory.UseContext(
				async databaseContext =>
					permedInstanceIds = await databaseContext
						.InstancePermissionSets
						.AsQueryable()
						.Where(ips => ips.PermissionSetId == authenticationContext.PermissionSet.Id.Value)
						.Select(ips => ips.Id)
						.ToListAsync(cancellationToken));

			return permedInstanceIds.Select(JobsHub.HubGroupName);
		}

		/// <summary>
		/// Refresh the <see cref="hub"/> <see cref="Hub.Groups"/> for clients associated with a given <paramref name="permissionSetId"/>.
		/// </summary>
		/// <param name="permissionSetId">The <see cref="Api.Models.EntityId.Id"/> of the <see cref="PermissionSet"/> who's users need updating.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask RefreshHubGroups(long permissionSetId, CancellationToken cancellationToken)
			=> databaseContextFactory.UseContext(
				async databaseContext =>
				{
					logger.LogTrace("RefreshHubGroups");
					var permissionSetUsers = await databaseContext
						.Users
						.Where(x => x.PermissionSet.Id == permissionSetId)
						.ToListAsync(cancellationToken);
					var allInstanceIds = await databaseContext
						.Instances
						.Select(
							instance => instance.Id.Value)
						.ToListAsync(cancellationToken);
					var permissionSetAccessibleInstanceIds = await databaseContext
						.InstancePermissionSets
						.AsQueryable()
						.Where(ips => ips.PermissionSetId == permissionSetId)
						.Select(ips => ips.InstanceId)
						.ToListAsync(cancellationToken);

					var groupsToRemove = allInstanceIds
						.Except(permissionSetAccessibleInstanceIds)
						.Select(JobsHub.HubGroupName);

					var groupsToAdd = permissionSetAccessibleInstanceIds
						.Select(JobsHub.HubGroupName);

					var connectionIds = permissionSetUsers
						.SelectMany(user => hub.UserConnectionIds(user))
						.ToList();

					logger.LogTrace(
						"Updating groups for the {connectionCount} hub connections of permission set {permissionSetId}. They may access {allowed}/{total} instances.",
						connectionIds.Count,
						permissionSetId,
						permissionSetAccessibleInstanceIds.Count,
						allInstanceIds.Count);

					var removeTasks = connectionIds
						.SelectMany(connectionId => groupsToRemove
							.Select(groupName => hub
								.Groups
								.RemoveFromGroupAsync(connectionId, groupName, cancellationToken)));

					var addTasks = connectionIds
						.SelectMany(connectionId => groupsToAdd
							.Select(groupName => hub
								.Groups
								.AddToGroupAsync(connectionId, groupName, cancellationToken)));

					// Checked internally, the default implementations for these tasks complete synchronously
					// https://github.com/dotnet/aspnetcore/blob/ce330d9d12f7676ff35c2223bd8a3b1e252a4e86/src/SignalR/server/Core/src/DefaultHubLifetimeManager.cs#L34-L70
					await Task.WhenAll(removeTasks.Concat(addTasks));
				});
	}
}
