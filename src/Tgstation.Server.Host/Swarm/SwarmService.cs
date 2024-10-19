using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Helps keep servers connected to the same database in sync by coordinating updates.
	/// </summary>
#pragma warning disable CA1506 // TODO: Decomplexify
	sealed class SwarmService : ISwarmService, ISwarmServiceController, ISwarmOperations, IDisposable
#pragma warning restore CA1506
	{
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
		[MemberNotNullWhen(true, nameof(serverHealthCheckTask), nameof(forceHealthCheckTcs), nameof(serverHealthCheckCancellationTokenSource), nameof(swarmServers))]
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
		/// The <see cref="ITokenFactory"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly ITokenFactory tokenFactory;

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
		readonly CancellationTokenSource? serverHealthCheckCancellationTokenSource;

		/// <summary>
		/// <see cref="List{T}"/> of connected <see cref="SwarmServerInformation"/>s.
		/// </summary>
		readonly List<SwarmServerInformation>? swarmServers;

		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="SwarmServer.Identifier"/>s to registration <see cref="Guid"/>s and when they were created.
		/// </summary>
		readonly Dictionary<string, (Guid RegistrationId, DateTimeOffset RegisteredAt)>? registrationIdsAndTimes;

		/// <summary>
		/// If the current server is the swarm controller.
		/// </summary>
		readonly bool swarmController;

		/// <summary>
		/// A <see cref="SwarmUpdateOperation"/> that is currently in progress.
		/// </summary>
		volatile SwarmUpdateOperation? updateOperation;

		/// <summary>
		/// A <see cref="TaskCompletionSource"/> that is used to force a health check.
		/// </summary>
		volatile TaskCompletionSource? forceHealthCheckTcs;

		/// <summary>
		/// The <see cref="Task"/> for the <see cref="HealthCheckLoop(CancellationToken)"/>.
		/// </summary>
		Task? serverHealthCheckTask;

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
		/// Initializes a new instance of the <see cref="SwarmService"/> class.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="databaseSeeder">The value of <see cref="databaseSeeder"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="serverUpdater">The value of <see cref="serverUpdater"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="transferService">The value of <see cref="transferService"/>.</param>
		/// <param name="tokenFactory">The value of <see cref="tokenFactory"/>.</param>
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
			ITokenFactory tokenFactory,
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
			this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			if (SwarmMode)
			{
				if (swarmConfiguration.Address == null)
					throw new InvalidOperationException("Swarm configuration missing Address!");

				if (String.IsNullOrWhiteSpace(swarmConfiguration.Identifier))
					throw new InvalidOperationException("Swarm configuration missing Identifier!");

				swarmController = swarmConfiguration.ControllerAddress == null;
				if (swarmController)
					registrationIdsAndTimes = new();

				serverHealthCheckCancellationTokenSource = new CancellationTokenSource();
				forceHealthCheckTcs = new TaskCompletionSource();

				swarmServers = new List<SwarmServerInformation>
				{
					new()
					{
						Address = swarmConfiguration.Address,
						PublicAddress = swarmConfiguration.PublicAddress,
						Controller = swarmController,
						Identifier = swarmConfiguration.Identifier,
					},
				};
			}
			else
				swarmController = true;
		}

		/// <inheritdoc />
		public void Dispose() => serverHealthCheckCancellationTokenSource?.Dispose();

		/// <inheritdoc />
		public async ValueTask AbortUpdate()
		{
			if (!SwarmMode)
				return;

			var localUpdateOperation = Interlocked.Exchange(ref updateOperation, null);
			var abortResult = localUpdateOperation?.Abort();
			switch (abortResult)
			{
				case SwarmUpdateAbortResult.Aborted:
					break;
				case SwarmUpdateAbortResult.AlreadyAborted:
					logger.LogDebug("Another context already aborted this update.");
					return;
				case SwarmUpdateAbortResult.CantAbortCommitted:
					logger.LogDebug("Not aborting update because we have committed!");
					return;
				case null:
					logger.LogTrace("Attempted update abort but no operation was found!");
					return;
				default:
					throw new InvalidOperationException($"Invalid return value for SwarmUpdateOperation.Abort(): {abortResult}");
			}

			await RemoteAbortUpdate();
		}

		/// <inheritdoc />
		public async ValueTask<SwarmCommitResult> CommitUpdate(CancellationToken cancellationToken)
		{
			if (!SwarmMode)
				return SwarmCommitResult.ContinueUpdateNonCommitted;

			// wait for the update commit TCS
			var localUpdateOperation = updateOperation;
			if (localUpdateOperation == null)
			{
				logger.LogDebug("Update commit failed, no pending operation!");
				await AbortUpdate(); // unnecessary, but can never be too safe
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
					await AbortUpdate();
					return SwarmCommitResult.AbortUpdate;
				}
			}

			var timeoutTask = swarmController
				? asyncDelayer.Delay(
					TimeSpan.FromMinutes(SwarmConstants.UpdateCommitTimeoutMinutes),
					cancellationToken)
				: Extensions.TaskExtensions.InfiniteTask.WaitAsync(cancellationToken);

			var commitTask = Task.WhenAny(localUpdateOperation.CommitGate, timeoutTask);

			await commitTask;

			var commitGoAhead = localUpdateOperation.CommitGate.IsCompleted
				&& localUpdateOperation.CommitGate.Result
				&& localUpdateOperation == updateOperation;
			if (!commitGoAhead)
			{
				logger.LogDebug(
					"Update commit failed!{maybeTimeout}",
					timeoutTask.IsCompleted
						? " Timed out!"
						: String.Empty);
				await AbortUpdate();
				return SwarmCommitResult.AbortUpdate;
			}

			logger.LogTrace("Update commit task complete");

			// on nodes, it means we can go straight ahead
			if (!swarmController)
				return SwarmCommitResult.MustCommitUpdate;

			// on the controller, we first need to signal for nodes to go ahead
			// if anything fails at this point, there's nothing we can do
			logger.LogDebug("Sending remote commit message to nodes...");
			async ValueTask SendRemoteCommitUpdate(SwarmServerInformation swarmServer)
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

			ValueTask task;
			lock (swarmServers)
				task = ValueTaskExtensions.WhenAll(
					swarmServers
						.Where(x => !x.Controller)
						.Select(SendRemoteCommitUpdate)
						.ToList());

			await task;
			return SwarmCommitResult.MustCommitUpdate;
		}

		/// <inheritdoc />
		public List<SwarmServerInformation>? GetSwarmServers()
		{
			if (!SwarmMode)
				return null;

			lock (swarmServers)
				return swarmServers.ToList();
		}

		/// <inheritdoc />
		public ValueTask<SwarmPrepareResult> PrepareUpdate(ISeekableFileStreamProvider fileStreamProvider, Version version, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(fileStreamProvider);

			ArgumentNullException.ThrowIfNull(version);

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
		public async ValueTask<bool> PrepareUpdateFromController(SwarmUpdateRequest updateRequest, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(updateRequest);

			logger.LogInformation("Received remote update request from {nodeType}", !swarmController ? "controller" : "node");
			var result = await PrepareUpdateImpl(
				null,
				updateRequest,
				cancellationToken);

			return result != SwarmPrepareResult.Failure;
		}

		/// <inheritdoc />
		public async ValueTask<SwarmRegistrationResult> Initialize(CancellationToken cancellationToken)
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
		public async ValueTask Shutdown(CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin Shutdown");

			async ValueTask SendUnregistrationRequest(SwarmServerInformation? swarmServer)
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
						swarmServer != null
							? $"node {swarmServer.Identifier}"
							: "from controller");
				}
			}

			if (SwarmMode && serverHealthCheckTask != null)
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

			// We're in a single-threaded-like context now so touching updateOperation directly is fine

			// downgrade the db if necessary
			if (updateOperation != null
				&& updateOperation.TargetVersion < assemblyInformationProvider.Version)
				await databaseContextFactory.UseContext(
					db => databaseSeeder.Downgrade(db, updateOperation.TargetVersion, cancellationToken));

			if (SwarmMode)
			{
				// Put the nodes into a reconnecting state
				if (updateOperation == null)
				{
					logger.LogInformation("Unregistering nodes...");
					ValueTask task;
					lock (swarmServers)
					{
						task = ValueTaskExtensions.WhenAll(
							swarmServers
								.Where(x => !x.Controller)
								.Select(SendUnregistrationRequest)
								.ToList());
						swarmServers.RemoveRange(1, swarmServers.Count - 1);
						registrationIdsAndTimes!.Clear();
					}

					await task;
				}

				logger.LogTrace("Swarm controller shutdown");
			}
		}

		/// <inheritdoc />
		public void UpdateSwarmServersList(IEnumerable<SwarmServerInformation> swarmServers)
		{
			ArgumentNullException.ThrowIfNull(swarmServers);

			if (!SwarmMode)
				throw new InvalidOperationException("Swarm mode not enabled!");

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
			if (!SwarmMode)
				throw new InvalidOperationException("Swarm mode not enabled!");

			if (swarmController)
				lock (swarmServers)
					return registrationIdsAndTimes!.Values.Any(x => x.RegistrationId == registrationId);

			if (registrationId != controllerRegistration)
				return false;

			lastControllerHealthCheck = DateTimeOffset.UtcNow;
			return true;
		}

		/// <inheritdoc />
		public async ValueTask<SwarmRegistrationResponse?> RegisterNode(SwarmServer node, Guid registrationId, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(node);

			if (node.Identifier == null)
				throw new ArgumentException("Node missing Identifier!", nameof(node));

			if (node.Address == null)
				throw new ArgumentException("Node missing Address!", nameof(node));

			if (!SwarmMode)
				throw new InvalidOperationException("Swarm mode not enabled!");

			if (!swarmController)
				throw new InvalidOperationException("Cannot RegisterNode on swarm node!");

			logger.LogTrace("RegisterNode");

			await AbortUpdate();

			SwarmRegistrationResponse CreateResponse() => new()
			{
				TokenSigningKeyBase64 = Convert.ToBase64String(tokenFactory.SigningKeyBytes),
			};

			var registrationIdsAndTimes = this.registrationIdsAndTimes!;
			lock (swarmServers)
			{
				if (registrationIdsAndTimes.Any(x => x.Value.RegistrationId == registrationId))
				{
					var preExistingRegistrationKvp = registrationIdsAndTimes.FirstOrDefault(x => x.Value.RegistrationId == registrationId);
					if (preExistingRegistrationKvp.Key == node.Identifier)
					{
						logger.LogWarning("Node {nodeId} has already registered!", node.Identifier);
						return CreateResponse();
					}

					logger.LogWarning(
						"Registration ID collision! Node {nodeId} tried to register with {otherNodeId}'s registration ID: {registrationId}",
						node.Identifier,
						preExistingRegistrationKvp.Key,
						registrationId);
					return null;
				}

				if (registrationIdsAndTimes.TryGetValue(node.Identifier, out var oldRegistration))
				{
					logger.LogInformation("Node {nodeId} is re-registering without first unregistering. Indicative of restart.", node.Identifier);
					swarmServers.RemoveAll(x => x.Identifier == node.Identifier);
					registrationIdsAndTimes.Remove(node.Identifier);
				}

				swarmServers.Add(new SwarmServerInformation
				{
					PublicAddress = node.PublicAddress,
					Address = node.Address,
					Identifier = node.Identifier,
					Controller = false,
				});
				registrationIdsAndTimes.Add(node.Identifier, (RegistrationId: registrationId, DateTimeOffset.UtcNow));
			}

			logger.LogInformation("Registered node {nodeId} ({nodeIP}) with ID {registrationId}", node.Identifier, node.Address, registrationId);
			MarkServersDirty();
			return CreateResponse();
		}

		/// <inheritdoc />
		public async ValueTask<bool> RemoteCommitReceived(Guid registrationId, CancellationToken cancellationToken)
		{
			var localUpdateOperation = updateOperation;
			if (!swarmController)
			{
				logger.LogDebug("Received remote commit go ahead");
				return localUpdateOperation?.Commit() == true;
			}

			var nodeIdentifier = NodeIdentifierFromRegistration(registrationId);
			if (nodeIdentifier == null)
			{
				// Something fucky is happening, take no chances.
				logger.LogError("Aborting update due to unforseen circumstances!");
				await AbortUpdate();
				return false;
			}

			if (localUpdateOperation == null)
			{
				logger.LogDebug("Ignoring ready-commit from node {nodeId} as the update appears to have been aborted.", nodeIdentifier);
				return false;
			}

			if (!localUpdateOperation.MarkNodeReady(nodeIdentifier))
			{
				logger.LogError(
					"Attempting to mark {nodeId} as ready to commit resulted in the update being aborted!",
					nodeIdentifier);

				// bit racy here, localUpdateOperation has already been aborted.
				// now if, FOR SOME GODFORSAKEN REASON, there's a new update operation, abort that too.
				if (Interlocked.CompareExchange(ref updateOperation, null, localUpdateOperation) == localUpdateOperation)
					await RemoteAbortUpdate();
				else
				{
					// marking as an error because how the actual fuck
					logger.LogError("Aborting new update due to unforseen consequences!");
					await AbortUpdate();
				}

				return false;
			}

			logger.LogDebug("Node {nodeId} is ready to commit.", nodeIdentifier);
			return true;
		}

		/// <inheritdoc />
		public async ValueTask UnregisterNode(Guid registrationId, CancellationToken cancellationToken)
		{
			if (!SwarmMode)
				throw new InvalidOperationException("Swarm mode not enabled!");

			logger.LogTrace("UnregisterNode {registrationId}", registrationId);
			await AbortUpdate();

			if (!swarmController)
			{
				// immediately trigger a health check
				logger.LogInformation("Controller unregistering, will attempt re-registration...");
				controllerRegistration = null;
				TriggerHealthCheck();
				return;
			}

			var nodeIdentifier = NodeIdentifierFromRegistration(registrationId);
			if (nodeIdentifier == null)
				return;

			logger.LogInformation("Unregistering node {nodeId}...", nodeIdentifier);

			lock (swarmServers)
			{
				swarmServers.RemoveAll(x => x.Identifier == nodeIdentifier);
				registrationIdsAndTimes!.Remove(nodeIdentifier);
			}

			MarkServersDirty();
		}

		/// <summary>
		/// Sends out remote abort update requests.
		/// </summary>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		/// <remarks>The aborted <see cref="updateOperation"/> should be cleared out before calling this. This method does not accept a <see cref="CancellationToken"/> because aborting an update should never be cancelled.</remarks>
		ValueTask RemoteAbortUpdate()
		{
			logger.LogInformation("Aborting swarm update!");

			using var httpClient = httpClientFactory.CreateClient();
			async ValueTask SendRemoteAbort(SwarmServerInformation swarmServer)
			{
				using var request = PrepareSwarmRequest(
					swarmServer,
					HttpMethod.Delete,
					SwarmConstants.UpdateRoute,
					null);

				try
				{
					// DCT: Intentionally should not be cancelled
					using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
					response.EnsureSuccessStatusCode();
				}
				catch (Exception ex)
				{
					logger.LogWarning(
						ex,
						"Unable to send remote abort to {nodeOrController}!",
						swarmController
							? $"node {swarmServer.Identifier}"
							: "controller");
				}
			}

			if (!swarmController)
				return SendRemoteAbort(new SwarmServerInformation
				{
					Address = swarmConfiguration.ControllerAddress,
				});

			lock (swarmServers!)
				return ValueTaskExtensions.WhenAll(
					swarmServers
						.Where(x => !x.Controller)
						.Select(SendRemoteAbort)
						.ToList());
		}

		/// <summary>
		/// Create the <see cref="RequestFileStreamProvider"/> for an update package retrieval from a given <paramref name="sourceNode"/>.
		/// </summary>
		/// <param name="sourceNode">The <see cref="SwarmServerInformation"/> to download the update package from.</param>
		/// <param name="ticket">The <see cref="FileTicketResponse"/> to use for the download.</param>
		/// <returns>A new <see cref="RequestFileStreamProvider"/> for the update package.</returns>
		RequestFileStreamProvider CreateUpdateStreamProvider(SwarmServerInformation sourceNode, FileTicketResponse ticket)
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
		/// <param name="updateRequest">The <see cref="SwarmUpdateRequest"/>. Must always have <see cref="SwarmUpdateRequest.UpdateVersion"/> populated. If <paramref name="initiatorProvider"/> is <see langword="null"/>, it must be fully populated.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="SwarmPrepareResult"/>.</returns>
		async ValueTask<SwarmPrepareResult> PrepareUpdateImpl(ISeekableFileStreamProvider? initiatorProvider, SwarmUpdateRequest updateRequest, CancellationToken cancellationToken)
		{
			var version = updateRequest.UpdateVersion!;
			if (!SwarmMode)
			{
				// we still need an active update operation for the TargetVersion
				updateOperation = new SwarmUpdateOperation(version);
				return SwarmPrepareResult.SuccessProviderNotRequired;
			}

			var initiator = initiatorProvider != null;
			logger.LogTrace("PrepareUpdateImpl {version}...", version);

			var shouldAbort = false;
			SwarmUpdateOperation localUpdateOperation;
			try
			{
				SwarmServerInformation? sourceNode = null;
				List<SwarmServerInformation> currentNodes;
				lock (swarmServers)
				{
					currentNodes = swarmServers
						.Select(node =>
						{
							if (node.Identifier == updateRequest.SourceNode)
								sourceNode = node;

							return node;
						})
						.ToList();
					if (swarmController)
						localUpdateOperation = new SwarmUpdateOperation(
							version,
							currentNodes);
					else
						localUpdateOperation = new SwarmUpdateOperation(version);
				}

				var existingUpdateOperation = Interlocked.CompareExchange(ref updateOperation, localUpdateOperation, null);
				if (existingUpdateOperation != null && existingUpdateOperation.TargetVersion != version)
				{
					logger.LogWarning("Aborting update preparation, version {targetUpdateVersion} already prepared!", existingUpdateOperation.TargetVersion);
					shouldAbort = true;
					return SwarmPrepareResult.Failure;
				}

				if (existingUpdateOperation?.TargetVersion == version)
				{
					logger.LogTrace("PrepareUpdateImpl early out, already prepared!");
					return SwarmPrepareResult.SuccessProviderNotRequired;
				}

				if (!swarmController && initiator)
				{
					var downloadTickets = await CreateDownloadTickets(initiatorProvider!, currentNodes, cancellationToken); // condition of initiator

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
						return SwarmPrepareResult.SuccessHoldProviderUntilCommit;

					shouldAbort = true;
					return SwarmPrepareResult.Failure;
				}

				if (!initiator)
				{
					logger.LogTrace("Beginning local update process...");

					if (sourceNode == null)
					{
						logger.Log(
							swarmController
								? LogLevel.Error
								: LogLevel.Warning,
							"Missing local node entry for update source node: {sourceNode}",
							updateRequest.SourceNode);
						shouldAbort = true;
						return SwarmPrepareResult.Failure;
					}

					if (updateRequest.DownloadTickets == null)
					{
						logger.LogError("Missing download tickets in update request!");
						return SwarmPrepareResult.Failure;
					}

					if (!updateRequest.DownloadTickets.TryGetValue(swarmConfiguration.Identifier!, out var ticket))
					{
						logger.Log(
							swarmController
								? LogLevel.Error
								: LogLevel.Warning,
							"Missing node entry for download ticket in update request!");
						shouldAbort = true;
						return SwarmPrepareResult.Failure;
					}

					ServerUpdateResult updateApplyResult;
					var downloaderStream = CreateUpdateStreamProvider(sourceNode, ticket);
					try
					{
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
					logger.LogTrace("No need to re-initiate update as it originated here on the swarm controller");

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
					await AbortUpdate();
			}

			if (!swarmController)
				return SwarmPrepareResult.SuccessProviderNotRequired;

			return await ControllerDistributedPrepareUpdate(
				initiatorProvider,
				updateRequest,
				localUpdateOperation,
				cancellationToken);
		}

		/// <summary>
		/// Send a given <paramref name="updateRequest"/> out to nodes from the swarm controller.
		/// </summary>
		/// <param name="initiatorProvider">The <see cref="ISeekableFileStreamProvider"/> containing the update package if this is the initiating server, <see langword="null"/> otherwise.</param>
		/// <param name="updateRequest">The <see cref="SwarmUpdateRequest"/>. Must always have <see cref="SwarmUpdateRequest.UpdateVersion"/> populated. If <paramref name="initiatorProvider"/> is <see langword="null"/>, it must be fully populated.</param>
		/// <param name="currentUpdateOperation">The current <see cref="SwarmUpdateOperation"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="SwarmPrepareResult"/>.</returns>
		async ValueTask<SwarmPrepareResult> ControllerDistributedPrepareUpdate(
			ISeekableFileStreamProvider? initiatorProvider,
			SwarmUpdateRequest updateRequest,
			SwarmUpdateOperation currentUpdateOperation,
			CancellationToken cancellationToken)
		{
			bool abortUpdate = false;
			try
			{
				logger.LogInformation("Sending remote prepare to nodes...");

				if (currentUpdateOperation.InvolvedServers.Count - 1 < swarmConfiguration.UpdateRequiredNodeCount)
				{
					logger.LogWarning(
						"Aborting update, controller expects to be in sync with {requiredNodeCount} nodes but currently only has {currentNodeCount}!",
						swarmConfiguration.UpdateRequiredNodeCount,
						currentUpdateOperation.InvolvedServers.Count - 1);
					abortUpdate = true;
					return SwarmPrepareResult.Failure;
				}

				var weAreInitiator = initiatorProvider != null;
				if (currentUpdateOperation.InvolvedServers.Count == 1)
				{
					logger.LogDebug("Controller has no nodes, setting commit-ready.");
					if (updateOperation?.Commit() == true)
						return SwarmPrepareResult.SuccessProviderNotRequired;

					logger.LogDebug("Update appears to have been aborted");
					return SwarmPrepareResult.Failure;
				}

				// The initiator node obviously doesn't create a ticket for itself
				else if (!weAreInitiator && updateRequest.DownloadTickets!.Count != currentUpdateOperation.InvolvedServers.Count - 1)
				{
					logger.LogWarning(
						"Aborting update, {receivedTickets} download tickets were provided but there are {nodesToUpdate} nodes in the swarm that require the package!",
						updateRequest.DownloadTickets.Count,
						currentUpdateOperation.InvolvedServers.Count);
					abortUpdate = true;
					return SwarmPrepareResult.Failure;
				}

				var downloadTicketDictionary = weAreInitiator
					? await CreateDownloadTickets(initiatorProvider!, currentUpdateOperation.InvolvedServers, cancellationToken)
					: updateRequest.DownloadTickets!;

				var sourceNode = weAreInitiator
					? swarmConfiguration.Identifier
					: updateRequest.SourceNode;

				using var httpClient = httpClientFactory.CreateClient();
				using var transferSemaphore = new SemaphoreSlim(1);

				bool anyFailed = false;
				var updateRequests = currentUpdateOperation
					.InvolvedServers
					.Where(node => !node.Controller)
					.Select(node =>
					{
						// only send the necessary ticket to each node from the controller
						Dictionary<string, FileTicketResponse>? localTicketDictionary;
						var nodeId = node.Identifier!;
						if (nodeId == sourceNode)
							localTicketDictionary = null;
						else if (!downloadTicketDictionary.TryGetValue(nodeId, out var ticket))
						{
							logger.LogError("Missing download ticket for node {missingNodeId}!", nodeId);
							anyFailed = true;
							return null;
						}
						else
							localTicketDictionary = new Dictionary<string, FileTicketResponse>
							{
								{ nodeId, ticket },
							};

						var request = new SwarmUpdateRequest
						{
							UpdateVersion = updateRequest.UpdateVersion,
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
						var node = tuple!.Item1;
						var body = tuple.Item2;

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
					logger.LogInformation("Distributed prepare for update to version {version} complete.", updateRequest.UpdateVersion);
					return weAreInitiator
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
					await AbortUpdate();
			}

			return SwarmPrepareResult.Failure;
		}

		/// <summary>
		/// Create a <see cref="FileTicketResponse"/> for downloading the content of a given <paramref name="initiatorProvider"/> for the rest of the swarm nodes.
		/// </summary>
		/// <param name="initiatorProvider">The <see cref="ISeekableFileStreamProvider"/> containing the server update package.</param>
		/// <param name="involvedServers">An <see cref="IEnumerable{T}"/> of the involved <see cref="SwarmServerInformation"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="Dictionary{TKey, TValue}"/> of unique <see cref="FileTicketResponse"/>s keyed by their <see cref="SwarmServer.Identifier"/>.</returns>
		async ValueTask<Dictionary<string, FileTicketResponse>> CreateDownloadTickets(
			ISeekableFileStreamProvider initiatorProvider,
			IReadOnlyCollection<SwarmServerInformation> involvedServers,
			CancellationToken cancellationToken)
		{
			// we need to ensure this thing is loaded before we start providing downloads or it'll create unnecessary delays
			var streamRetrievalTask = initiatorProvider.GetResult(cancellationToken);

			var downloadProvider = new FileDownloadProvider(
				() => initiatorProvider.Disposed
					? Api.Models.ErrorCode.ResourceNotPresent
					: null,
				async downloadToken => await initiatorProvider.GetOwnedResult(downloadToken), // let it throw if disposed, shouldn't happen regardless
				"<Swarm Update Package Provider>",
				false);

			var serversRequiringTickets = involvedServers
				.Where(node => node.Identifier != swarmConfiguration.Identifier)
				.ToList();

			logger.LogTrace("Creating {n} download tickets for other nodes...", serversRequiringTickets.Count);

			var downloadTickets = new Dictionary<string, FileTicketResponse>(serversRequiringTickets.Count);
			foreach (var node in serversRequiringTickets)
				downloadTickets.Add(
					node.Identifier!,
					transferService.CreateDownload(downloadProvider));

			await streamRetrievalTask;
			return downloadTickets;
		}

		/// <summary>
		/// Ping each node to see that they are still running.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask HealthCheckNodes(CancellationToken cancellationToken)
		{
			using var httpClient = httpClientFactory.CreateClient();

			List<SwarmServerInformation> currentSwarmServers;
			lock (swarmServers!)
				currentSwarmServers = swarmServers.ToList();

			var registrationIdsAndTimes = this.registrationIdsAndTimes!;
			async ValueTask HealthRequestForServer(SwarmServerInformation swarmServer)
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
					registrationIdsAndTimes.Remove(swarmServer.Identifier!);
				}
			}

			await ValueTaskExtensions.WhenAll(
				currentSwarmServers
					.Where(node => !node.Controller
						&& registrationIdsAndTimes.TryGetValue(node.Identifier!, out var registrationAndTime)
						&& registrationAndTime.RegisteredAt.AddMinutes(SwarmConstants.ControllerHealthCheckIntervalMinutes) < DateTimeOffset.UtcNow)
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
			var currentTcs = Interlocked.Exchange(ref forceHealthCheckTcs, new TaskCompletionSource());
			return currentTcs!.TrySetResult();
		}

		/// <summary>
		/// Ping the swarm controller to see that it is still running. If need be, reregister.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask HealthCheckController(CancellationToken cancellationToken)
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
					await AbortUpdate();
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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="SwarmRegistrationResult"/>.</returns>
		async ValueTask<SwarmRegistrationResult> RegisterWithController(CancellationToken cancellationToken)
		{
			logger.LogInformation("Attempting to register with swarm controller at {controllerAddress}...", swarmConfiguration.ControllerAddress);
			var requestedRegistrationId = Guid.NewGuid();

			using var httpClient = httpClientFactory.CreateClient();
			using var registrationRequest = PrepareSwarmRequest(
				null,
				HttpMethod.Post,
				SwarmConstants.RegisterRoute,
				new SwarmRegistrationRequest(Version.Parse(MasterVersionsAttribute.Instance.RawSwarmProtocolVersion))
				{
					Identifier = swarmConfiguration.Identifier,
					Address = swarmConfiguration.Address,
					PublicAddress = swarmConfiguration.PublicAddress,
				},
				requestedRegistrationId);

			try
			{
				using var response = await httpClient.SendAsync(registrationRequest, HttpCompletionOption.ResponseContentRead, cancellationToken);
				if (response.IsSuccessStatusCode)
				{
					try
					{
						var json = await response.Content.ReadAsStringAsync(cancellationToken);
						if (json == null)
						{
							logger.LogDebug("Error reading registration response content stream! Text was null!");
							return SwarmRegistrationResult.PayloadFailure;
						}

						var registrationResponse = JsonConvert.DeserializeObject<SwarmRegistrationResponse>(json);
						if (registrationResponse == null)
						{
							logger.LogDebug("Error reading registration response content stream! Payload was null!");
							return SwarmRegistrationResult.PayloadFailure;
						}

						if (registrationResponse.TokenSigningKeyBase64 == null)
						{
							logger.LogDebug("Error reading registration response content stream! SigningKey was null!");
							return SwarmRegistrationResult.PayloadFailure;
						}

						tokenFactory.SigningKeyBytes = Convert.FromBase64String(registrationResponse.TokenSigningKeyBase64);
					}
					catch (Exception ex)
					{
						logger.LogDebug(ex, "Error reading registration response content stream!");
						return SwarmRegistrationResult.PayloadFailure;
					}

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
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask SendUpdatedServerListToNodes(CancellationToken cancellationToken)
		{
			List<SwarmServerInformation> currentSwarmServers;
			lock (swarmServers!)
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
			async ValueTask UpdateRequestForServer(SwarmServerInformation swarmServer)
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
						registrationIdsAndTimes!.Remove(swarmServer.Identifier!);
					}
				}
			}

			await ValueTaskExtensions.WhenAll(
				currentSwarmServers
					.Where(x => !x.Controller)
					.Select(UpdateRequestForServer)
					.ToList());
		}

		/// <summary>
		/// Prepares a <see cref="HttpRequestMessage"/> for swarm communication.
		/// </summary>
		/// <param name="swarmServer">The <see cref="SwarmServerInformation"/> the message is for. Must have <see cref="SwarmServer.Address"/> and <see cref="SwarmServer.Identifier"/> set. If <see langword="null"/>, will be sent to swarm controller.</param>
		/// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
		/// <param name="route">The route on <see cref="SwarmConstants.ControllerRoute"/> to use.</param>
		/// <param name="body">The body <see cref="object"/> if any.</param>
		/// <param name="registrationIdOverride">An optional override to the <see cref="SwarmConstants.RegistrationIdHeader"/>.</param>
		/// <returns>A new <see cref="HttpRequestMessage"/>.</returns>
		HttpRequestMessage PrepareSwarmRequest(
			SwarmServerInformation? swarmServer,
			HttpMethod httpMethod,
			string route,
			object? body,
			Guid? registrationIdOverride = null)
		{
			swarmServer ??= new SwarmServerInformation
			{
				Address = swarmConfiguration.ControllerAddress,
			};

			var fullRoute = $"{SwarmConstants.ControllerRoute}/{route}";
			logger.LogTrace(
				"{method} {route} to swarm server {nodeIdOrAddress}",
				httpMethod,
				fullRoute,
				swarmServer.Identifier ?? swarmServer.Address!.ToString());

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
					lock (swarmServers!)
						if (registrationIdsAndTimes!.TryGetValue(swarmServer.Identifier!, out var registrationIdAndTime))
							request.Headers.Add(SwarmConstants.RegistrationIdHeader, registrationIdAndTime.RegistrationId.ToString());
				}
				else if (controllerRegistration.HasValue)
					request.Headers.Add(SwarmConstants.RegistrationIdHeader, controllerRegistration.Value.ToString());

				if (body != null)
					request.Content = new StringContent(
						JsonConvert.SerializeObject(body, SwarmConstants.SerializerSettings),
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
				var nextForceHealthCheckTask = forceHealthCheckTcs!.Task;
				while (!cancellationToken.IsCancellationRequested)
				{
					TimeSpan delay;
					if (swarmController)
						delay = TimeSpan.FromMinutes(SwarmConstants.ControllerHealthCheckIntervalMinutes);
					else
					{
						delay = TimeSpan.FromMinutes(SwarmConstants.NodeHealthCheckIntervalMinutes);
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
						logger.LogTrace("Next health check triggering in {delaySeconds}s...", SwarmConstants.SecondsToDelayForcedHealthChecks);
						await asyncDelayer.Delay(TimeSpan.FromSeconds(SwarmConstants.SecondsToDelayForcedHealthChecks), cancellationToken);
					}
					else if (!swarmController && !nextForceHealthCheckTask.IsCompleted)
					{
						if (!lastControllerHealthCheck.HasValue)
						{
							logger.LogTrace("Not initially registered with controller, skipping health check.");
							continue; // unregistered
						}

						if ((DateTimeOffset.UtcNow - lastControllerHealthCheck.Value).TotalMinutes < SwarmConstants.NodeHealthCheckIntervalMinutes)
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
		/// Gets the <see cref="SwarmServer.Identifier"/> from a given <paramref name="registrationId"/>.
		/// </summary>
		/// <param name="registrationId">The registration <see cref="Guid"/>.</param>
		/// <returns>The registered <see cref="SwarmServer.Identifier"/> or <see langword="null"/> if it does not exist.</returns>
		string? NodeIdentifierFromRegistration(Guid registrationId)
		{
			if (!swarmController)
				throw new InvalidOperationException("NodeIdentifierFromRegistration on node!");

			lock (swarmServers!)
			{
				var registrationIdsAndTimes = this.registrationIdsAndTimes!;
				var exists = registrationIdsAndTimes.Any(x => x.Value.RegistrationId == registrationId);
				if (!exists)
				{
					logger.LogWarning("A node that was to be looked up ({registrationId}) disappeared from our records!", registrationId);
					return null;
				}

				return registrationIdsAndTimes.First(x => x.Value.RegistrationId == registrationId).Key;
			}
		}
	}
}
