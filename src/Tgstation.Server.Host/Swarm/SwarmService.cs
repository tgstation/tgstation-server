using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Common;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Extensions.Converters;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Helps keep servers connected to the same database in sync by coordinating updates.
	/// </summary>
	sealed class SwarmService : ISwarmService, ISwarmServiceController, ISwarmOperations, IDisposable
	{
		/// <summary>
		/// Interval at which the swarm controller makes health checks on nodes.
		/// </summary>
		const int ControllerHealthCheckIntervalMinutes = 3;

		/// <summary>
		/// Interval at which the node makes health checks on the controller if it has not received one.
		/// </summary>
		const int NodeHealthCheckIntervalMinutes = 5;

		/// <summary>
		/// Number of minutes the controller waits to receive a ready-commit from all nodes before aborting an update.
		/// </summary>
		const int UpdateCommitTimeoutMinutes = 10;

		/// <summary>
		/// Number of seconds between <see cref="forceHealthCheckTcs"/> triggering and a health check being performed.
		/// </summary>
		const int SecondsToDelayForcedHealthChecks = 15;

		/// <summary>
		/// See <see cref="JsonSerializerSettings"/> for the swarm system.
		/// </summary>
		internal static JsonSerializerSettings SerializerSettings { get; }

		/// <inheritdoc />
		public bool ExpectedNumberOfNodesConnected
		{
			get
			{
				if (!SwarmMode)
					return true;

				lock (swarmServers)
					return swarmServers.Count - 1 >= swarmConfiguration.UpdateRequiredNodeCount;
			}
		}

		/// <summary>
		/// If the swarm system is enabled.
		/// </summary>
		bool SwarmMode => swarmConfiguration.PrivateKey != null;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IDatabaseSeeder"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly IDatabaseSeeder databaseSeeder;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IAbstractHttpClientFactory"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly IAbstractHttpClientFactory httpClientFactory;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IServerUpdater"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly IServerUpdater serverUpdater;

		/// <summary>
		/// The <see cref="IFileTransferTicketProvider"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly IFileTransferTicketProvider transferService;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly ILogger<SwarmService> logger;

		/// <summary>
		/// The <see cref="SwarmConfiguration"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly SwarmConfiguration swarmConfiguration;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="serverHealthCheckTask"/>.
		/// </summary>
		readonly CancellationTokenSource serverHealthCheckCancellationTokenSource;

		/// <summary>
		/// <see cref="List{T}"/> of connected <see cref="SwarmServerResponse"/>s.
		/// </summary>
		readonly List<SwarmServerResponse> swarmServers;

		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="Api.Models.Internal.SwarmServer.Identifier"/>s to registration <see cref="Guid"/>s.
		/// </summary>
		readonly Dictionary<string, Guid> registrationIds;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> used for accessing <see cref="targetUpdateVersion"/>.
		/// </summary>
		readonly object updateSynchronizationLock;

		/// <summary>
		/// If the current server is the swarm controller.
		/// </summary>
		readonly bool swarmController;

		/// <summary>
		/// A <see cref="TaskCompletionSource"/> that is used to force a health check.
		/// </summary>
		TaskCompletionSource forceHealthCheckTcs;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> that is used to proceed with committing an update.
		/// </summary>
		TaskCompletionSource<bool> updateCommitTcs;

		/// <summary>
		/// <see cref="List{T}"/> of <see cref="Api.Models.Internal.SwarmServer.Identifier"/>s that need to send a ready-commit before the update can proceed.
		/// </summary>
		List<string> nodesThatNeedToBeReadyToCommit;

		/// <summary>
		/// The <see cref="Task"/> for the <see cref="HealthCheckLoop(CancellationToken)"/>.
		/// </summary>
		Task serverHealthCheckTask;

		/// <summary>
		/// The <see cref="Version"/> set for a two phase commit update.
		/// </summary>
		Version targetUpdateVersion;

		/// <summary>
		/// The registration <see cref="Guid"/> provided by the swarm controller.
		/// </summary>
		Guid? controllerRegistration;

		/// <summary>
		/// The last <see cref="DateTimeOffset"/> when the controller checked on this node.
		/// </summary>
		DateTimeOffset? lastControllerHealthCheck;

		/// <summary>
		/// If the <see cref="swarmServers"/> list has been updated and needs to be resent to clients.
		/// </summary>
		bool serversDirty;

		/// <summary>
		/// If we've sent out a remote commit message for an update operation.
		/// </summary>
		bool updateCommitSent;

		/// <summary>
		/// Initializes static members of the <see cref="SwarmService"/> class.
		/// </summary>
		static SwarmService()
		{
			SerializerSettings = new ()
			{
				ContractResolver = new DefaultContractResolver
				{
					NamingStrategy = new CamelCaseNamingStrategy(),
				},
				Converters = new JsonConverter[]
				{
					new VersionConverter(),
					new BoolConverter(),
				},
				DefaultValueHandling = DefaultValueHandling.Ignore,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			};
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmService"/> class.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="databaseSeeder">The value of <see cref="databaseSeeder"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="serverUpdater">The value of <see cref="serverUpdater"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="transferService">The value of <see cref="transferService"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public SwarmService(
			IDatabaseContextFactory databaseContextFactory,
			IDatabaseSeeder databaseSeeder,
			IAssemblyInformationProvider assemblyInformationProvider,
			IAbstractHttpClientFactory httpClientFactory,
			IAsyncDelayer asyncDelayer,
			IServerUpdater serverUpdater,
			IFileTransferTicketProvider transferService,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<SwarmService> logger)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.serverUpdater = serverUpdater ?? throw new ArgumentNullException(nameof(serverUpdater));
			this.transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			if (SwarmMode)
			{
				if (swarmConfiguration.Address == null)
					throw new InvalidOperationException("Swarm configuration missing Address!");
				if (String.IsNullOrWhiteSpace(swarmConfiguration.Identifier))
					throw new InvalidOperationException("Swarm configuration missing Identifier!");
			}

			swarmController = !SwarmMode || swarmConfiguration.ControllerAddress == null;
			if (SwarmMode)
			{
				serverHealthCheckCancellationTokenSource = new CancellationTokenSource();
				forceHealthCheckTcs = new TaskCompletionSource();
				if (swarmController)
					registrationIds = new Dictionary<string, Guid>();

				swarmServers = new List<SwarmServerResponse>
				{
					new SwarmServerResponse
					{
						Address = swarmConfiguration.Address,
						Controller = swarmController,
						Identifier = swarmConfiguration.Identifier,
					},
				};

				updateSynchronizationLock = new object();
			}
		}

		/// <inheritdoc />
		public void Dispose() => serverHealthCheckCancellationTokenSource?.Dispose();

		/// <inheritdoc />
		public async Task RemoteAbortUpdate(CancellationToken cancellationToken)
		{
			if (targetUpdateVersion == null)
			{
				logger.LogTrace("Not remote aborting non-existent update");
				return;
			}

			await AbortUpdate(cancellationToken);
		}

		/// <inheritdoc />
		public async Task AbortUpdate(CancellationToken cancellationToken)
		{
			if (!SwarmMode)
				return;

			logger.LogInformation("Aborting swarm update!");
			var commitTcs = updateCommitTcs;
			updateCommitTcs = null;
			commitTcs?.TrySetResult(false);
			nodesThatNeedToBeReadyToCommit = null;
			targetUpdateVersion = null;

			using var httpClient = httpClientFactory.CreateClient();
			async Task SendRemoteAbort(SwarmServerResponse swarmServer)
			{
				using var request = PrepareSwarmRequest(
					swarmServer,
					HttpMethod.Delete,
					SwarmConstants.UpdateRoute,
					null);

				try
				{
					using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
					response.EnsureSuccessStatusCode();
				}
				catch (Exception ex)
				{
					logger.LogWarning(
						ex,
						"Unable to set remote abort to {nodeOrController}!",
						swarmController
							? $"node {swarmServer.Identifier}"
							: "controller");
				}
			}

			Task task;
			if (!swarmController)
				task = SendRemoteAbort(new SwarmServerResponse
				{
					Address = swarmConfiguration.ControllerAddress,
				});
			else
			{
				lock (swarmServers)
					task = Task.WhenAll(
						swarmServers
							.Where(x => !x.Controller)
							.Select(SendRemoteAbort));
			}

			await task;
		}

		/// <inheritdoc />
		public async Task<SwarmCommitResult> CommitUpdate(CancellationToken cancellationToken)
		{
			if (!SwarmMode)
				return SwarmCommitResult.ContinueUpdateNonCommitted;

			// wait for the update commit TCS
			var commitTcsTask = updateCommitTcs?.Task;
			if (commitTcsTask == null)
			{
				logger.LogDebug("Update commit failed, no pending task completion source!");
				await AbortUpdate(cancellationToken);
				return SwarmCommitResult.AbortUpdate;
			}

			logger.LogInformation("Waiting to commit update...");
			using var httpClient = httpClientFactory.CreateClient();
			if (!swarmController)
			{
				// let the controller know we're ready
				logger.LogTrace("Sending ready-commit to swarm controller...");
				using var commitReadyRequest = PrepareSwarmRequest(
					null,
					HttpMethod.Post,
					SwarmConstants.UpdateRoute,
					null);

				try
				{
					using var commitReadyResponse = await httpClient.SendAsync(commitReadyRequest, HttpCompletionOption.ResponseContentRead, cancellationToken);
					commitReadyResponse.EnsureSuccessStatusCode();
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Unable to send ready-commit to swarm controller!");
					await AbortUpdate(cancellationToken);
					return SwarmCommitResult.AbortUpdate;
				}
			}

			var timeoutTask = swarmController
				? asyncDelayer.Delay(
					TimeSpan.FromMinutes(UpdateCommitTimeoutMinutes),
					cancellationToken)
				: Extensions.TaskExtensions.InfiniteTask.WithToken(cancellationToken);

			var commitTask = Task.WhenAny(commitTcsTask, timeoutTask);

			await commitTask;

			var commitGoAhead = commitTcsTask.IsCompleted
				&& commitTcsTask.Result
				&& updateCommitTcs?.Task == commitTcsTask;
			if (!commitGoAhead)
			{
				logger.LogDebug(
					"Update commit failed!{maybeTimeout}",
					timeoutTask.IsCompleted
						? " Timed out!"
						: String.Empty);
				await AbortUpdate(cancellationToken);
				return SwarmCommitResult.AbortUpdate;
			}

			logger.LogTrace("Update commit task complete");

			// on nodes, it means we can go straight ahead
			if (!swarmController)
				return SwarmCommitResult.MustCommitUpdate;

			// on the controller, we first need to signal for nodes to go ahead
			// if anything fails at this point, there's nothing we can do
			logger.LogDebug("Sending remote commit message to nodes...");
			async Task SendRemoteCommitUpdate(SwarmServerResponse swarmServer)
			{
				using var request = PrepareSwarmRequest(
					swarmServer,
					HttpMethod.Post,
					SwarmConstants.UpdateRoute,
					null);

				try
				{
					// I know using the cancellationToken after this point doesn't seem very sane
					// It's the token for Ctrl+C on server's console though, so we must respect it
					using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
					response.EnsureSuccessStatusCode();
				}
				catch (Exception ex)
				{
					logger.LogCritical(ex, "Failed to send update commit request to node {nodeId}!", swarmServer.Identifier);
				}
			}

			updateCommitSent = true;

			Task task;
			lock (swarmServers)
				task = Task.WhenAll(
					swarmServers
						.Where(x => !x.Controller)
						.Select(SendRemoteCommitUpdate));

			await task;
			return SwarmCommitResult.MustCommitUpdate;
		}

		/// <inheritdoc />
		public ICollection<SwarmServerResponse> GetSwarmServers()
		{
			if (!SwarmMode)
				return null;

			lock (swarmServers)
				return swarmServers.ToList();
		}

		/// <inheritdoc />
		public Task<SwarmPrepareResult> PrepareUpdate(ISeekableFileStreamProvider fileStreamProvider, Version version, CancellationToken cancellationToken)
		{
			if (fileStreamProvider == null)
				throw new ArgumentNullException(nameof(fileStreamProvider));

			if (version == null)
				throw new ArgumentNullException(nameof(version));

			logger.LogTrace("Begin PrepareUpdate...");
			return PrepareUpdateImpl(
				fileStreamProvider,
				new SwarmUpdateRequest
				{
					UpdateVersion = version,
				},
				cancellationToken);
		}

		/// <inheritdoc />
		public async Task<bool> PrepareUpdateFromController(SwarmUpdateRequest updateRequest, CancellationToken cancellationToken)
		{
			if (updateRequest == null)
				throw new ArgumentNullException(nameof(updateRequest));

			logger.LogInformation("Received remote update request from {nodeType}", !swarmController ? "controller" : "node");
			var result = await PrepareUpdateImpl(
				null,
				updateRequest,
				cancellationToken);

			return result != SwarmPrepareResult.Failure;
		}

		/// <inheritdoc />
		public async Task<SwarmRegistrationResult> Initialize(CancellationToken cancellationToken)
		{
			if (SwarmMode)
				logger.LogInformation(
					"Swarm mode enabled: {nodeType} {nodeId}",
					swarmController
						? "Controller"
						: "Node",
					swarmConfiguration.Identifier);
			else
				logger.LogTrace("Swarm mode disabled");

			SwarmRegistrationResult result;
			if (swarmController)
			{
				if (swarmConfiguration.UpdateRequiredNodeCount > 0)
					logger.LogInformation("Expecting connections from {expectedNodeCount} nodes", swarmConfiguration.UpdateRequiredNodeCount);

				await databaseContextFactory.UseContext(
					databaseContext => databaseSeeder.Initialize(databaseContext, cancellationToken));

				result = SwarmRegistrationResult.Success;
			}
			else
				result = await RegisterWithController(cancellationToken);

			if (SwarmMode && result == SwarmRegistrationResult.Success)
				serverHealthCheckTask = HealthCheckLoop(serverHealthCheckCancellationTokenSource.Token);

			return result;
		}

		/// <inheritdoc />
		public async Task Shutdown(CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin Shutdown");

			async Task SendUnregistrationRequest(SwarmServerResponse swarmServer)
			{
				using var httpClient = httpClientFactory.CreateClient();
				using var request = PrepareSwarmRequest(
					swarmServer,
					HttpMethod.Delete,
					SwarmConstants.RegisterRoute,
					null);

				try
				{
					using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
					response.EnsureSuccessStatusCode();
				}
				catch (Exception ex)
				{
					logger.LogWarning(
						ex,
						"Error unregistering {nodeType}!",
						swarmController
							? $"node {swarmServer.Identifier}"
							: "from controller");
				}
			}

			if (serverHealthCheckTask != null)
			{
				serverHealthCheckCancellationTokenSource.Cancel();
				await serverHealthCheckTask;
			}

			if (!swarmController)
			{
				if (controllerRegistration != null)
				{
					logger.LogInformation("Unregistering from swarm controller...");
					await SendUnregistrationRequest(null);
				}

				return;
			}

			// downgrade the db if necessary
			if (targetUpdateVersion != null
				&& targetUpdateVersion < assemblyInformationProvider.Version)
				await databaseContextFactory.UseContext(
					db => databaseSeeder.Downgrade(db, targetUpdateVersion, cancellationToken));

			if (SwarmMode)
			{
				// Put the nodes into a reconnecting state
				if (targetUpdateVersion == null)
				{
					logger.LogInformation("Unregistering nodes...");
					Task task;
					lock (swarmServers)
					{
						task = Task.WhenAll(
							swarmServers
								.Where(x => !x.Controller)
								.Select(SendUnregistrationRequest));
						swarmServers.RemoveRange(1, swarmServers.Count - 1);
						registrationIds.Clear();
					}

					await task;
				}

				logger.LogTrace("Swarm controller shutdown");
			}
		}

		/// <inheritdoc />
		public void UpdateSwarmServersList(IEnumerable<SwarmServerResponse> swarmServers)
		{
			if (swarmServers == null)
				throw new ArgumentNullException(nameof(swarmServers));

			if (swarmController)
				throw new InvalidOperationException("Cannot UpdateSwarmServersList on swarm controller!");

			lock (this.swarmServers)
			{
				this.swarmServers.Clear();
				this.swarmServers.AddRange(swarmServers);
				logger.LogDebug("Updated swarm server list with {nodeCount} total nodes", this.swarmServers.Count);
			}
		}

		/// <inheritdoc />
		public bool ValidateRegistration(Guid registrationId)
		{
			if (swarmController)
				lock (swarmServers)
					return registrationIds.Values.Any(x => x == registrationId);

			if (registrationId != controllerRegistration)
				return false;

			lastControllerHealthCheck = DateTimeOffset.UtcNow;
			return true;
		}

		/// <inheritdoc />
		public bool RegisterNode(Api.Models.Internal.SwarmServer node, Guid registrationId)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));

			if (node.Identifier == null)
				throw new ArgumentException("Node missing Identifier!", nameof(node));

			if (node.Address == null)
				throw new ArgumentException("Node missing Address!", nameof(node));

			if (!swarmController)
				throw new InvalidOperationException("Cannot RegisterNode on swarm node!");

			lock (updateSynchronizationLock)
			{
				if (targetUpdateVersion != null)
				{
					logger.LogInformation("Not registering node {nodeId} as a distributed update is in progress.", node.Identifier);
					return false;
				}

				lock (swarmServers)
				{
					if (registrationIds.Any(x => x.Value == registrationId))
					{
						var preExistingRegistrationKvp = registrationIds.FirstOrDefault(x => x.Value == registrationId);
						if (preExistingRegistrationKvp.Key == node.Identifier)
						{
							logger.LogWarning("Node {nodeId} has already registered!", node.Identifier);
							return true;
						}

						logger.LogWarning(
							"Registration ID collision! Node {nodeId} tried to register with {otherNodeId}'s registration ID: {registrationId}",
							node.Identifier,
							preExistingRegistrationKvp.Key,
							registrationId);
						return false;
					}

					if (registrationIds.TryGetValue(node.Identifier, out var oldRegistration))
					{
						logger.LogInformation("Node {nodeId} is re-registering without first unregistering. Indicative of restart.", node.Identifier);
						swarmServers.RemoveAll(x => x.Identifier == node.Identifier);
						registrationIds.Remove(node.Identifier);
					}

					swarmServers.Add(new SwarmServerResponse
					{
						Address = node.Address,
						Identifier = node.Identifier,
						Controller = false,
					});
					registrationIds.Add(node.Identifier, registrationId);
				}
			}

			logger.LogInformation("Registered node {nodeId} ({nodeIP}) with ID {registrationId}", node.Identifier, node.Address, registrationId);
			MarkServersDirty();
			return true;
		}

		/// <inheritdoc />
		public async Task<bool> RemoteCommitRecieved(Guid registrationId, CancellationToken cancellationToken)
		{
			if (!swarmController)
			{
				logger.LogDebug("Received remote commit go ahead");
				var commitTcs = updateCommitTcs;
				commitTcs?.TrySetResult(true);
				return commitTcs != null;
			}

			var nodeIdentifier = NodeIdentifierFromRegistration(registrationId);
			if (nodeIdentifier == null)
			{
				// Something fucky is happening, take no chances.
				logger.LogDebug("Aborting update due to unforseen circumstances!");
				await AbortUpdate(cancellationToken);
				return false;
			}

			var nodeList = nodesThatNeedToBeReadyToCommit;
			if (nodeList == null)
			{
				logger.LogDebug("Ignoring ready-commit from node {nodeId} as the update appears to have been aborted.", nodeIdentifier);
				return false;
			}

			logger.LogDebug("Node {nodeId} is ready to commit.", nodeIdentifier);
			lock (nodeList)
			{
				nodeList.Remove(nodeIdentifier);
				if (nodeList.Count == 0)
				{
					logger.LogTrace("All nodes ready, update commit is a go once controller is ready");
					var commitTcs = updateCommitTcs;
					return commitTcs?.TrySetResult(true) == true;
				}
			}

			return true;
		}

		/// <inheritdoc />
		public async Task UnregisterNode(Guid registrationId, CancellationToken cancellationToken)
		{
			if (!swarmController)
			{
				// immediately trigger a health check
				logger.LogInformation("Controller unregistering, will attempt re-registration...");
				controllerRegistration = null;
				TriggerHealthCheck();
				return;
			}

			logger.LogTrace("UnregisterNode {registrationId}", registrationId);
			var nodeIdentifier = NodeIdentifierFromRegistration(registrationId);
			if (nodeIdentifier == null)
				return;

			logger.LogInformation("Unregistering node {nodeId}...", nodeIdentifier);

			if (!updateCommitSent)
				await AbortUpdate(cancellationToken);
			else
				logger.LogTrace("Not aborting update because we have committed");

			lock (swarmServers)
			{
				swarmServers.RemoveAll(x => x.Identifier == nodeIdentifier);
				registrationIds.Remove(nodeIdentifier);
			}

			MarkServersDirty();
		}

		/// <summary>
		/// Create the <see cref="RequestFileStreamProvider"/> for an update package retrieval from a given <paramref name="sourceNode"/>.
		/// </summary>
		/// <param name="sourceNode">The <see cref="SwarmServerResponse"/> to download the update package from.</param>
		/// <param name="ticket">The <see cref="FileTicketResponse"/> to use for the download.</param>
		/// <returns>A new <see cref="RequestFileStreamProvider"/> for the update package.</returns>
		RequestFileStreamProvider CreateUpdateStreamProvider(SwarmServerResponse sourceNode, FileTicketResponse ticket)
		{
			var httpClient = httpClientFactory.CreateClient();
			try
			{
				var request = PrepareSwarmRequest(
					sourceNode,
					HttpMethod.Get,
					$"{SwarmConstants.UpdateRoute}?ticket={HttpUtility.UrlEncode(ticket.FileTicket)}",
					null);

				try
				{
					request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Octet));
					return new RequestFileStreamProvider(httpClient, request);
				}
				catch
				{
					request.Dispose();
					throw;
				}
			}
			catch
			{
				httpClient.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Implementation of <see cref="PrepareUpdate(ISeekableFileStreamProvider, Version, CancellationToken)"/>,.
		/// </summary>
		/// <param name="initiatorProvider">The <see cref="ISeekableFileStreamProvider"/> containing the update package if this is the initiating server, <see langword="null"/> otherwise.</param>
		/// <param name="updateRequest">The <see cref="SwarmUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="SwarmPrepareResult"/>.</returns>
		async Task<SwarmPrepareResult> PrepareUpdateImpl(ISeekableFileStreamProvider initiatorProvider, SwarmUpdateRequest updateRequest, CancellationToken cancellationToken)
		{
			if (!SwarmMode)
				return SwarmPrepareResult.SuccessProviderNotRequired;

			var version = updateRequest.UpdateVersion;
			var initiator = initiatorProvider != null;
			logger.LogTrace("PrepareUpdateImpl {version}...", version);

			lock (updateSynchronizationLock)
				if (version == targetUpdateVersion)
				{
					logger.LogDebug("Prepare update short circuit!");
					return SwarmPrepareResult.SuccessProviderNotRequired;
				}

			var shouldAbort = false;
			try
			{
				lock (updateSynchronizationLock)
				{
					if (targetUpdateVersion == version)
					{
						logger.LogTrace("PrepareUpdateFromController early out, already prepared!");
						return SwarmPrepareResult.SuccessProviderNotRequired;
					}

					if (targetUpdateVersion != null)
					{
						logger.LogWarning("Aborting update preparation, version {targetUpdateVersion} already prepared!", targetUpdateVersion);
						shouldAbort = true;
						return SwarmPrepareResult.Failure;
					}

					targetUpdateVersion = version;
				}

				if (!swarmController && initiator)
				{
					var downloadTickets = CreateDownloadTickets(initiatorProvider);

					logger.LogInformation("Forwarding update request to swarm controller...");
					using var httpClient = httpClientFactory.CreateClient();
					using var request = PrepareSwarmRequest(
						null,
						HttpMethod.Put,
						SwarmConstants.UpdateRoute,
						new SwarmUpdateRequest
						{
							UpdateVersion = version,
							SourceNode = swarmConfiguration.Identifier,
							DownloadTickets = downloadTickets,
						});

					// File transfer service will hold the necessary streams
					using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
					if (response.IsSuccessStatusCode)
					{
						updateCommitTcs = new TaskCompletionSource<bool>();
						return SwarmPrepareResult.SuccessHoldProviderUntilCommit;
					}

					return SwarmPrepareResult.Failure;
				}

				if (!initiator)
				{
					logger.LogTrace("Beginning local update process...");

					if (!updateRequest.DownloadTickets.TryGetValue(swarmConfiguration.Identifier, out var ticket))
					{
						logger.LogWarning("Missing node entry for download ticket in update request!");
						return SwarmPrepareResult.Failure;
					}

					SwarmServerResponse sourceNode;
					lock (swarmServers)
						sourceNode = swarmServers
							.Where(node => node.Identifier == updateRequest.SourceNode)
							.FirstOrDefault();

					if (sourceNode == null)
					{
						logger.LogWarning("Missing local node entry for update source node: {sourceNode}", updateRequest.SourceNode);
						return SwarmPrepareResult.Failure;
					}

					var downloaderStream = CreateUpdateStreamProvider(sourceNode, ticket);
					ServerUpdateResult updateApplyResult;
					try
					{
						updateCommitTcs = new TaskCompletionSource<bool>();
						updateApplyResult = await serverUpdater.BeginUpdate(
							this,
							downloaderStream,
							version,
							cancellationToken);
					}
					catch
					{
						await downloaderStream.DisposeAsync();
						throw;
					}

					if (updateApplyResult != ServerUpdateResult.Started)
					{
						logger.LogWarning("Failed to prepare update! Result: {serverUpdateResult}", updateApplyResult);
						shouldAbort = true;
						return SwarmPrepareResult.Failure;
					}
				}
				else
				{
					logger.LogTrace("No need to re-initiate update as it originated here on the swarm controller");
					updateCommitTcs = new TaskCompletionSource<bool>();
				}

				logger.LogDebug("Local node prepared for update to version {version}", version);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to prepare update!");
				shouldAbort = true;
				return SwarmPrepareResult.Failure;
			}
			finally
			{
				if (shouldAbort)
					await AbortUpdate(cancellationToken);
			}

			if (!swarmController)
				return SwarmPrepareResult.SuccessProviderNotRequired;

			bool abortUpdate = false;
			try
			{
				logger.LogInformation("Sending remote prepare to nodes...");
				List<SwarmServerResponse> serversToPrepare;
				lock (swarmServers)
				{
					nodesThatNeedToBeReadyToCommit = new List<string>(
						swarmServers
							.Where(x => !x.Controller)
							.Select(x => x.Identifier));

					if (nodesThatNeedToBeReadyToCommit.Count < swarmConfiguration.UpdateRequiredNodeCount)
					{
						logger.LogWarning(
							"Aborting update, controller expects to be in sync with {requiredNodeCount} nodes but currently only has {currentNodeCount}!",
							swarmConfiguration.UpdateRequiredNodeCount,
							nodesThatNeedToBeReadyToCommit.Count);
						abortUpdate = true;
						return SwarmPrepareResult.Failure;
					}

					if (nodesThatNeedToBeReadyToCommit.Count == 0)
					{
						logger.LogDebug("Controller has no nodes, setting commit-ready.");
						var commitTcs = updateCommitTcs;
						commitTcs?.TrySetResult(true);
						if (commitTcs != null)
							return SwarmPrepareResult.SuccessProviderNotRequired;

						logger.LogDebug("Update appears to have been aborted");
						return SwarmPrepareResult.Failure;
					}

					serversToPrepare = swarmServers
						.Where(x => !x.Controller)
						.ToList();
				}

				var downloadTicketDictionary = initiator
					? CreateDownloadTickets(initiatorProvider)
					: updateRequest.DownloadTickets;

				var sourceNode = initiator
					? swarmConfiguration.Identifier
					: updateRequest.SourceNode;

				using var httpClient = httpClientFactory.CreateClient();
				using var transferSemaphore = new SemaphoreSlim(1);

				bool anyFailed = false;
				var updateRequests = serversToPrepare
					.Select(node =>
					{
						// only send the necessary ticket to each node from the controller
						Dictionary<string, FileTicketResponse> localTicketDictionary;
						if (!downloadTicketDictionary.TryGetValue(node.Identifier, out var ticket)
							&& node.Identifier != sourceNode)
						{
							logger.LogWarning("Missing download ticket for node {missingNodeId}!", node.Identifier);
							anyFailed = true;
							return null;
						}
						else
							localTicketDictionary = new Dictionary<string, FileTicketResponse>
							{
								{ node.Identifier, ticket },
							};

						var request = new SwarmUpdateRequest
						{
							UpdateVersion = version,
							SourceNode = sourceNode,
							DownloadTickets = localTicketDictionary,
						};

						return Tuple.Create(node, request);
					})
					.ToList();

				if (anyFailed)
					return SwarmPrepareResult.Failure;

				var tasks = updateRequests
					.Select(async tuple =>
					{
						var node = tuple.Item1;
						var body = tuple.Item2;

						// no need to provide every server with every ticket
						using var request = PrepareSwarmRequest(
							node,
							HttpMethod.Put,
							SwarmConstants.UpdateRoute,
							body);

						using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
						return response.IsSuccessStatusCode;
					})
					.ToList();

				await Task.WhenAll(tasks);

				// if all succeeds...
				if (tasks.All(x => x.Result))
				{
					logger.LogInformation("Distributed prepare for update to version {version} complete.", version);
					return initiator
						? SwarmPrepareResult.SuccessHoldProviderUntilCommit
						: SwarmPrepareResult.SuccessProviderNotRequired;
				}

				abortUpdate = true;
				logger.LogDebug("Distrubuted prepare failed!");
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error remotely preparing updates!");
			}
			finally
			{
				if (abortUpdate)
					await AbortUpdate(cancellationToken);
			}

			return SwarmPrepareResult.Failure;
		}

		/// <summary>
		/// Create a <see cref="FileTicketResponse"/> for downloading the content of a given <paramref name="initiatorProvider"/> for the rest of the swarm nodes.
		/// </summary>
		/// <param name="initiatorProvider">The <see cref="ISeekableFileStreamProvider"/> containing the server update package.</param>
		/// <returns>A new <see cref="Dictionary{TKey, TValue}"/> of unique <see cref="FileTicketResponse"/>s keyed by their <see cref="Api.Models.Internal.SwarmServer.Identifier"/>.</returns>
		Dictionary<string, FileTicketResponse> CreateDownloadTickets(ISeekableFileStreamProvider initiatorProvider)
		{
			var downloadProvider = new FileDownloadProvider(
				() => initiatorProvider.Disposed
					? Api.Models.ErrorCode.ResourceNotPresent
					: null,
				async downloadToken => await initiatorProvider.GetOwnedResult(downloadToken), // let it throw if disposed, shouldn't happen regardless
				"<Swarm Update Package Provider>",
				false);

			Dictionary<string, FileTicketResponse> downloadTickets;
			lock (swarmServers)
			{
				logger.LogTrace("Creating download tickets...");
				downloadTickets = new Dictionary<string, FileTicketResponse>(swarmServers.Count - 1);
				foreach (var node in swarmServers)
				{
					if (node.Identifier == swarmConfiguration.Identifier)
						continue;

					downloadTickets.Add(
						node.Identifier,
						transferService.CreateDownload(downloadProvider));
				}
			}

			return downloadTickets;
		}

		/// <summary>
		/// Ping each node to see that they are still running.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task HealthCheckNodes(CancellationToken cancellationToken)
		{
			using var httpClient = httpClientFactory.CreateClient();

			List<SwarmServerResponse> currentSwarmServers;
			lock (swarmServers)
				currentSwarmServers = swarmServers.ToList();

			async Task HealthRequestForServer(SwarmServerResponse swarmServer)
			{
				using var request = PrepareSwarmRequest(
					swarmServer,
					HttpMethod.Get,
					String.Empty,
					null);

				try
				{
					using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
					response.EnsureSuccessStatusCode();
					return;
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					logger.LogWarning(
						ex,
						"Error during swarm server health check on node '{nodeId}'! Unregistering...",
						swarmServer.Identifier);
				}

				lock (swarmServers)
				{
					swarmServers.Remove(swarmServer);
					registrationIds.Remove(swarmServer.Identifier);
				}
			}

			await Task.WhenAll(
				currentSwarmServers
					.Where(x => !x.Controller)
					.Select(HealthRequestForServer));

			lock (swarmServers)
				if (swarmServers.Count != currentSwarmServers.Count)
					MarkServersDirty();

			if (serversDirty)
				await SendUpdatedServerListToNodes(cancellationToken);
		}

		/// <summary>
		/// Set <see cref="serversDirty"/> and complete the current <see cref="forceHealthCheckTcs"/>.
		/// </summary>
		void MarkServersDirty()
		{
			serversDirty = true;
			if (TriggerHealthCheck())
				logger.LogTrace("Server list is dirty!");
		}

		/// <summary>
		/// Complete the current <see cref="forceHealthCheckTcs"/>.
		/// </summary>
		/// <returns><see langword="true"/> the result of the call to <see cref="TaskCompletionSource{TResult}.TrySetResult(TResult)"/>.</returns>
		bool TriggerHealthCheck()
		{
			var currentTcs = forceHealthCheckTcs;
			forceHealthCheckTcs = new TaskCompletionSource();
			return currentTcs.TrySetResult();
		}

		/// <summary>
		/// Ping the swarm controller to see that it is still running. If need be, reregister.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task HealthCheckController(CancellationToken cancellationToken)
		{
			using var httpClient = httpClientFactory.CreateClient();

			if (controllerRegistration.HasValue)
				try
				{
					using var request = PrepareSwarmRequest(
						null,
						HttpMethod.Get,
						String.Empty,
						null);
					using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
					response.EnsureSuccessStatusCode();
					logger.LogTrace("Controller health check successful");
					return;
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Error during swarm controller health check! Attempting to re-register...");
					controllerRegistration = null;
					await AbortUpdate(cancellationToken);
				}

			SwarmRegistrationResult registrationResult;
			for (var registrationAttempt = 1UL; ; ++registrationAttempt)
			{
				logger.LogInformation("Swarm re-registration attempt {attemptNumber}...", registrationAttempt);
				registrationResult = await RegisterWithController(cancellationToken);

				if (registrationResult == SwarmRegistrationResult.Success)
					return;

				if (registrationResult == SwarmRegistrationResult.Unauthorized)
				{
					logger.LogError("Swarm re-registration failed, controller's private key has changed!");
					break;
				}

				if (registrationResult == SwarmRegistrationResult.VersionMismatch)
				{
					logger.LogError("Swarm re-registration failed, controller's TGS version has changed!");
					break;
				}

				await asyncDelayer.Delay(TimeSpan.FromSeconds(5), cancellationToken);
			}

			// we could do something here... but what?
			// best to just let the health check loop keep retrying... we won't be able to update at least
		}

		/// <summary>
		/// Attempt to register the node with the controller.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="SwarmRegistrationResult"/>.</returns>
		async Task<SwarmRegistrationResult> RegisterWithController(CancellationToken cancellationToken)
		{
			logger.LogInformation("Attempting to register with swarm controller at {controllerAddress}...", swarmConfiguration.ControllerAddress);
			var requestedRegistrationId = Guid.NewGuid();

			using var httpClient = httpClientFactory.CreateClient();
			using var registrationRequest = PrepareSwarmRequest(
				null,
				HttpMethod.Post,
				SwarmConstants.RegisterRoute,
				new SwarmRegistrationRequest
				{
					ServerVersion = assemblyInformationProvider.Version,
					Identifier = swarmConfiguration.Identifier,
					Address = swarmConfiguration.Address,
				},
				requestedRegistrationId);

			try
			{
				using var response = await httpClient.SendAsync(registrationRequest, HttpCompletionOption.ResponseContentRead, cancellationToken);
				if (response.IsSuccessStatusCode)
				{
					logger.LogInformation("Sucessfully registered with ID {registrationId}", requestedRegistrationId);
					controllerRegistration = requestedRegistrationId;
					lastControllerHealthCheck = DateTimeOffset.UtcNow;
					return SwarmRegistrationResult.Success;
				}

				logger.LogWarning("Unable to register with swarm: HTTP {statusCode}!", response.StatusCode);

				if (response.StatusCode == HttpStatusCode.Unauthorized)
					return SwarmRegistrationResult.Unauthorized;

				if (response.StatusCode == HttpStatusCode.UpgradeRequired)
					return SwarmRegistrationResult.VersionMismatch;

				try
				{
					var responseData = await response.Content.ReadAsStringAsync(cancellationToken);
					if (!String.IsNullOrWhiteSpace(responseData))
						logger.LogDebug("Response:{newLine}{responseData}", Environment.NewLine, responseData);
				}
				catch (Exception ex)
				{
					logger.LogDebug(ex, "Error reading registration response content stream!");
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error sending registration request!");
			}

			return SwarmRegistrationResult.CommunicationFailure;
		}

		/// <summary>
		/// Sends the controllers list of nodes to all nodes.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task SendUpdatedServerListToNodes(CancellationToken cancellationToken)
		{
			List<SwarmServerResponse> currentSwarmServers;
			lock (swarmServers)
			{
				serversDirty = false;
				currentSwarmServers = swarmServers.ToList();
			}

			if (currentSwarmServers.Count == 1)
			{
				logger.LogTrace("Skipping server list broadcast as no nodes are connected!");
				return;
			}

			logger.LogDebug("Sending updated server list to all {nodeCount} nodes...", currentSwarmServers.Count - 1);

			using var httpClient = httpClientFactory.CreateClient();
			async Task UpdateRequestForServer(SwarmServerResponse swarmServer)
			{
				using var request = PrepareSwarmRequest(
					swarmServer,
					HttpMethod.Post,
					String.Empty,
					new SwarmServersUpdateRequest
					{
						SwarmServers = currentSwarmServers,
					});

				try
				{
					using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
					response.EnsureSuccessStatusCode();
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					logger.LogWarning(ex, "Error during swarm server list update for node '{nodeId}'! Unregistering...", swarmServer.Identifier);

					lock (swarmServers)
					{
						swarmServers.Remove(swarmServer);
						registrationIds.Remove(swarmServer.Identifier);
					}
				}
			}

			await Task.WhenAll(
				currentSwarmServers
					.Where(x => !x.Controller)
					.Select(UpdateRequestForServer));
		}

		/// <summary>
		/// Prepares a <see cref="HttpRequestMessage"/> for swarm communication.
		/// </summary>
		/// <param name="swarmServer">The <see cref="SwarmServerResponse"/> the message is for, if null will be sent to swarm controller.</param>
		/// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
		/// <param name="route">The route on <see cref="SwarmConstants.ControllerRoute"/> to use.</param>
		/// <param name="body">The body <see cref="object"/> if any.</param>
		/// <param name="registrationIdOverride">An optional override to the <see cref="SwarmConstants.RegistrationIdHeader"/>.</param>
		/// <returns>A new <see cref="HttpRequestMessage"/>.</returns>
		HttpRequestMessage PrepareSwarmRequest(
			SwarmServerResponse swarmServer,
			HttpMethod httpMethod,
			string route,
			object body,
			Guid? registrationIdOverride = null)
		{
			swarmServer ??= new SwarmServerResponse
			{
				Address = swarmConfiguration.ControllerAddress,
			};

			var fullRoute = $"{SwarmConstants.ControllerRoute}/{route}";
			logger.LogTrace(
				"{method} {route} to swarm server {nodeIdOrAddress}",
				httpMethod,
				fullRoute,
				swarmServer.Identifier ?? swarmServer.Address.ToString());

			var request = new HttpRequestMessage(
				httpMethod,
				swarmServer.Address + fullRoute[1..]);
			try
			{
				request.Headers.Accept.Clear();
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

				request.Headers.Add(SwarmConstants.ApiKeyHeader, swarmConfiguration.PrivateKey);
				if (registrationIdOverride.HasValue)
					request.Headers.Add(SwarmConstants.RegistrationIdHeader, registrationIdOverride.Value.ToString());
				else if (swarmController)
				{
					lock (swarmServers)
						if (registrationIds.TryGetValue(swarmServer.Identifier, out var registrationId))
							request.Headers.Add(SwarmConstants.RegistrationIdHeader, registrationId.ToString());
				}
				else if (controllerRegistration.HasValue)
					request.Headers.Add(SwarmConstants.RegistrationIdHeader, controllerRegistration.Value.ToString());

				if (body != null)
					request.Content = new StringContent(
						JsonConvert.SerializeObject(body, SerializerSettings),
						Encoding.UTF8,
						MediaTypeNames.Application.Json);

				return request;
			}
			catch
			{
				request.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Timed loop for calling <see cref="HealthCheckNodes(CancellationToken)"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the rinning operation.</returns>
		async Task HealthCheckLoop(CancellationToken cancellationToken)
		{
			logger.LogTrace("Starting HealthCheckLoop...");
			try
			{
				var nextForceHealthCheckTask = forceHealthCheckTcs.Task;
				while (!cancellationToken.IsCancellationRequested)
				{
					TimeSpan delay;
					if (swarmController)
						delay = TimeSpan.FromMinutes(ControllerHealthCheckIntervalMinutes);
					else
					{
						delay = TimeSpan.FromMinutes(NodeHealthCheckIntervalMinutes);
						if (lastControllerHealthCheck.HasValue)
						{
							var recommendedTimeOfNextCheck = lastControllerHealthCheck.Value + delay;

							if (recommendedTimeOfNextCheck > DateTimeOffset.UtcNow)
								delay = recommendedTimeOfNextCheck - DateTimeOffset.UtcNow;
						}
					}

					var delayTask = asyncDelayer.Delay(
						delay,
						cancellationToken);

					var awakeningTask = Task.WhenAny(
						delayTask,
						nextForceHealthCheckTask);

					await awakeningTask;

					if (nextForceHealthCheckTask.IsCompleted && swarmController)
					{
						// Intentionally wait a few seconds for the other server to start up before interogating it
						logger.LogTrace("Next health check triggering in {delaySeconds}s...", SecondsToDelayForcedHealthChecks);
						await asyncDelayer.Delay(TimeSpan.FromSeconds(SecondsToDelayForcedHealthChecks), cancellationToken);
					}
					else if (!swarmController && !nextForceHealthCheckTask.IsCompleted)
					{
						if (!lastControllerHealthCheck.HasValue)
						{
							logger.LogTrace("Not initially registered with controller, skipping health check.");
							continue; // unregistered
						}

						if ((DateTimeOffset.UtcNow - lastControllerHealthCheck.Value).TotalMinutes < NodeHealthCheckIntervalMinutes)
						{
							logger.LogTrace("Controller seems to be active, skipping health check.");
							continue;
						}
					}

					nextForceHealthCheckTask = forceHealthCheckTcs.Task;

					logger.LogTrace("Performing swarm health check...");
					try
					{
						if (swarmController)
							await HealthCheckNodes(cancellationToken);
						else
							await HealthCheckController(cancellationToken);
					}
					catch (Exception ex) when (ex is not OperationCanceledException)
					{
						logger.LogError(ex, "Health check error!");
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, "Health check loop cancelled!");
			}

			logger.LogTrace("Stopped HealthCheckLoop");
		}

		/// <summary>
		/// Gets the <see cref="Api.Models.Internal.SwarmServer.Identifier"/> from a given <paramref name="registrationId"/>.
		/// </summary>
		/// <param name="registrationId">The registration <see cref="Guid"/>.</param>
		/// <returns>The registered <see cref="Api.Models.Internal.SwarmServer.Identifier"/> or <see langword="null"/> if it does not exist.</returns>
		string NodeIdentifierFromRegistration(Guid registrationId)
		{
			if (!swarmController)
				throw new InvalidOperationException("NodeIdentifierFromRegistration on node!");

			lock (swarmServers)
			{
				var exists = registrationIds.Any(x => x.Value == registrationId);
				if (!exists)
				{
					logger.LogWarning("A node that was to be looked up ({registrationId}) disappeared from our records!", registrationId);
					return null;
				}

				return registrationIds.First(x => x.Value == registrationId).Key;
			}
		}
	}
}
