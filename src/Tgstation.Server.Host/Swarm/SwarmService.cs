using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Google.Protobuf;

using Grpc.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
using Tgstation.Server.Host.Swarm.Grpc;
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
					return swarmServers.Count - 1 >= swarmConfigurationOptions.CurrentValue.UpdateRequiredNodeCount;
			}
		}

		/// <summary>
		/// If the swarm system is enabled.
		/// </summary>
		[MemberNotNullWhen(true, nameof(serverHealthCheckTask), nameof(forceHealthCheckTcs), nameof(serverHealthCheckCancellationTokenSource), nameof(swarmServers))]
		bool SwarmMode => swarmConfigurationOptions.CurrentValue.PrivateKey != null;

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
		/// The <see cref="ICallInvokerlFactory"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly ICallInvokerlFactory grpcChannelFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly ILogger<SwarmService> logger;

		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> for the <see cref="SwarmConfiguration"/>.
		/// </summary>
		readonly IOptionsMonitor<SwarmConfiguration> swarmConfigurationOptions;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="serverHealthCheckTask"/>.
		/// </summary>
		readonly CancellationTokenSource? serverHealthCheckCancellationTokenSource;

		/// <summary>
		/// <see cref="List{T}"/> of connected <see cref="SwarmServerInformation"/>s.
		/// </summary>
		readonly List<SwarmServerInformation>? swarmServers;

		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="Api.Models.Internal.SwarmServer.Identifier"/>s to registration <see cref="Guid"/>s and when they were created.
		/// </summary>
		readonly Dictionary<string, (Guid RegistrationId, DateTimeOffset RegisteredAt)>? registrationIdsAndTimes;

		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of non-controller <see cref="Api.Models.Internal.SwarmServer.Identifier"/>s to <see cref="CallInvoker"/>s connecting to them.
		/// </summary>
		readonly Dictionary<string, CallInvoker> nodeCallInvokers;

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
		/// The <see cref="CallInvoker"/> for communication with the swarm controller.
		/// </summary>
		CallInvoker? controllerCallInvoker;

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
		/// <param name="grpcChannelFactory">The value of <see cref="grpcChannelFactory"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfigurationOptions"/>.</param>
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
			ICallInvokerlFactory grpcChannelFactory,
			IOptionsMonitor<SwarmConfiguration> swarmConfigurationOptions,
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
			this.grpcChannelFactory = grpcChannelFactory ?? throw new ArgumentNullException(nameof(grpcChannelFactory));
			this.swarmConfigurationOptions = swarmConfigurationOptions ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			nodeCallInvokers = new Dictionary<string, CallInvoker>();
			if (SwarmMode)
			{
				var currentSwarmOptions = swarmConfigurationOptions.CurrentValue;
				if (currentSwarmOptions.Address == null)
					throw new InvalidOperationException("Swarm configuration missing Address!");

				if (string.IsNullOrWhiteSpace(currentSwarmOptions.Identifier))
					throw new InvalidOperationException("Swarm configuration missing Identifier!");

				swarmController = currentSwarmOptions.ControllerAddress == null;
				if (swarmController)
					registrationIdsAndTimes = new();

				serverHealthCheckCancellationTokenSource = new CancellationTokenSource();
				forceHealthCheckTcs = new TaskCompletionSource();

				swarmServers = new List<SwarmServerInformation>
				{
					new()
					{
						Address = currentSwarmOptions.Address,
						PublicAddress = currentSwarmOptions.PublicAddress,
						Controller = swarmController,
						Identifier = currentSwarmOptions.Identifier,
					},
				};
			}
			else
				swarmController = true;
		}

		/// <inheritdoc />
		public void Dispose()
			=> serverHealthCheckCancellationTokenSource?.Dispose();

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

				var client = new GrpcSwarmSharedService.GrpcSwarmSharedServiceClient(
					GetCallInvokerForNode(null, out var registration));

				try
				{
					await client.CommitUpdateAsync(
						new CommitUpdateRequest
						{
							Registration = registration,
						},
						cancellationToken: cancellationToken);
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
					cancellationToken).AsTask()
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
				var client = new GrpcSwarmSharedService.GrpcSwarmSharedServiceClient(
					GetCallInvokerForNode(swarmServer, out var registration));

				try
				{
					await client.CommitUpdateAsync(
						new CommitUpdateRequest
						{
							Registration = registration,
						},
						cancellationToken: cancellationToken);
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
				new PrepareUpdateRequest
				{
					UpdateVersion = new GrpcVersion(version),
				},
				cancellationToken);
		}

		/// <inheritdoc />
		public async ValueTask<bool> PrepareUpdateFromController(PrepareUpdateRequest updateRequest, CancellationToken cancellationToken)
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
			var currentSwarmConfiguration = swarmConfigurationOptions.CurrentValue;
			if (SwarmMode)
				logger.LogInformation(
					"Swarm mode enabled: {nodeType} {nodeId}",
					swarmController
						? "Controller"
						: "Node",
					currentSwarmConfiguration.Identifier);
			else
				logger.LogTrace("Swarm mode disabled");

			SwarmRegistrationResult result;
			if (swarmController)
			{
				if (currentSwarmConfiguration.UpdateRequiredNodeCount > 0)
					logger.LogInformation("Expecting connections from {expectedNodeCount} nodes", currentSwarmConfiguration.UpdateRequiredNodeCount);

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
				var callInvoker = GetCallInvokerForNode(swarmServer, out var registration);
				var client = new GrpcSwarmControllerService.GrpcSwarmControllerServiceClient(callInvoker);

				try
				{
					await client.UnregisterNodeAsync(
						new UnregisterNodeRequest
						{
							Registration = registration,
						},
						cancellationToken: cancellationToken);
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
		public bool ValidateRegistration(SwarmRegistration registration)
		{
			if (!SwarmMode)
				throw new InvalidOperationException("Swarm mode not enabled!");

			var registrationId = registration.ToGuid();
			if (swarmController)
				lock (swarmServers)
					return registrationIdsAndTimes!.Values.Any(x => x.RegistrationId == registrationId);

			if (registrationId != controllerRegistration)
				return false;

			lastControllerHealthCheck = DateTimeOffset.UtcNow;
			return true;
		}

		/// <inheritdoc />
		public async ValueTask<RegisterNodeResponse> RegisterNode(Api.Models.Internal.SwarmServer node, CancellationToken cancellationToken)
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

			var registrationIdsAndTimes = this.registrationIdsAndTimes!;
			Guid registrationId;
			lock (swarmServers)
			{
				do
				{
					registrationId = Guid.NewGuid();
				}
				while (registrationIdsAndTimes.Any(x => x.Value.RegistrationId == registrationId));

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
			return new()
			{
				Registration = new SwarmRegistration
				{
					Id = registrationId.ToString(),
				},
				TokenSigningKey = ByteString.CopyFrom(tokenFactory.SigningKeyBytes),
			};
		}

		/// <inheritdoc />
		public async ValueTask<bool> RemoteCommitReceived(SwarmRegistration registration, CancellationToken cancellationToken)
		{
			var registrationId = registration.ToGuid();
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
		public async ValueTask UnregisterNode(SwarmRegistration registration, CancellationToken cancellationToken)
		{
			if (!SwarmMode)
				throw new InvalidOperationException("Swarm mode not enabled!");

			var registrationId = registration.ToGuid();
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
				var callInvoker = GetCallInvokerForNode(swarmServer, out var registration);
				var client = new GrpcSwarmSharedService.GrpcSwarmSharedServiceClient(callInvoker);

				try
				{
					// DCT: Intentionally should not be cancelled
					await client.AbortUpdateAsync(
						new AbortUpdateRequest
						{
							Registration = registration,
						},
						cancellationToken: CancellationToken.None);
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
					Address = swarmConfigurationOptions.CurrentValue.ControllerAddress,
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
				var request = new HttpRequestMessage(
					HttpMethod.Get,
					$"{Api.Routes.Transfer}?ticket={HttpUtility.UrlEncode(ticket.FileTicket)}");

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
		/// <param name="updateRequest">The <see cref="PrepareUpdateRequest"/>. Must always have <see cref="PrepareUpdateRequest.UpdateVersion"/> populated. If <paramref name="initiatorProvider"/> is <see langword="null"/>, it must be fully populated.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="SwarmPrepareResult"/>.</returns>
#pragma warning disable CA1506 // TODO: Decomplexify
		async ValueTask<SwarmPrepareResult> PrepareUpdateImpl(ISeekableFileStreamProvider? initiatorProvider, PrepareUpdateRequest updateRequest, CancellationToken cancellationToken)
#pragma warning restore CA1506
		{
			var version = updateRequest.UpdateVersion.ToVersion();
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
							if (node.Identifier == updateRequest.SourceNodeIdentifier)
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

					var callInvoker = GetCallInvokerForNode(null, out var registration);
					var client = new GrpcSwarmSharedService.GrpcSwarmSharedServiceClient(callInvoker);

					// File transfer service will hold the necessary streams
					var request = new PrepareUpdateRequest
					{
						Registration = registration,
						UpdateVersion = new GrpcVersion(version),
						SourceNodeIdentifier = swarmConfigurationOptions.CurrentValue.Identifier,
					};

					request.DownloadTicketsByNodeIdentifier.Add(downloadTickets.ToDictionary(x => x.Key, x => new DownloadTicket(x.Value)));
					var response = await client.PrepareUpdateAsync(
						request,
						cancellationToken: cancellationToken);

					return SwarmPrepareResult.SuccessHoldProviderUntilCommit;
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
							updateRequest.SourceNodeIdentifier);
						shouldAbort = true;
						return SwarmPrepareResult.Failure;
					}

					if (updateRequest.DownloadTicketsByNodeIdentifier == null)
					{
						logger.LogError("Missing download tickets in update request!");
						return SwarmPrepareResult.Failure;
					}

					if (!updateRequest.DownloadTicketsByNodeIdentifier.TryGetValue(swarmConfigurationOptions.CurrentValue.Identifier!, out var ticket))
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
					var downloaderStream = CreateUpdateStreamProvider(sourceNode, ticket.ToFileTicketResponse());
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
		/// <param name="updateRequest">The <see cref="PrepareUpdateRequest"/>. Must always have <see cref="PrepareUpdateRequest.UpdateVersion"/> populated. If <paramref name="initiatorProvider"/> is <see langword="null"/>, it must be fully populated.</param>
		/// <param name="currentUpdateOperation">The current <see cref="SwarmUpdateOperation"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="SwarmPrepareResult"/>.</returns>
#pragma warning disable CA1506 // TODO: Decomplexify
		async ValueTask<SwarmPrepareResult> ControllerDistributedPrepareUpdate(
			ISeekableFileStreamProvider? initiatorProvider,
			PrepareUpdateRequest updateRequest,
			SwarmUpdateOperation currentUpdateOperation,
			CancellationToken cancellationToken)
#pragma warning restore CA1506
		{
			bool abortUpdate = false;
			try
			{
				logger.LogInformation("Sending remote prepare to nodes...");

				var currentSwarmConfiguration = swarmConfigurationOptions.CurrentValue;
				if (currentUpdateOperation.InvolvedServers.Count - 1 < currentSwarmConfiguration.UpdateRequiredNodeCount)
				{
					logger.LogWarning(
						"Aborting update, controller expects to be in sync with {requiredNodeCount} nodes but currently only has {currentNodeCount}!",
						currentSwarmConfiguration.UpdateRequiredNodeCount,
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
				else if (!weAreInitiator && updateRequest.DownloadTicketsByNodeIdentifier!.Count != currentUpdateOperation.InvolvedServers.Count - 1)
				{
					logger.LogWarning(
						"Aborting update, {receivedTickets} download tickets were provided but there are {nodesToUpdate} nodes in the swarm that require the package!",
						updateRequest.DownloadTicketsByNodeIdentifier.Count,
						currentUpdateOperation.InvolvedServers.Count);
					abortUpdate = true;
					return SwarmPrepareResult.Failure;
				}

				var downloadTicketDictionary = weAreInitiator
					? await CreateDownloadTickets(initiatorProvider!, currentUpdateOperation.InvolvedServers, cancellationToken)
					: updateRequest.DownloadTicketsByNodeIdentifier.ToDictionary(x => x.Key, x => x.Value.ToFileTicketResponse());

				var sourceNode = weAreInitiator
					? currentSwarmConfiguration.Identifier
					: updateRequest.SourceNodeIdentifier;

				using var httpClient = httpClientFactory.CreateClient();
				using var transferSemaphore = new SemaphoreSlim(1);

				bool anyFailed = false;
				var updateRequests = currentUpdateOperation
					.InvolvedServers
					.Where(node => !node.Controller)
					.Select(node =>
					{
						// only send the necessary ticket to each node from the controller
						Dictionary<string, DownloadTicket>? localTicketDictionary;
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
							localTicketDictionary = new Dictionary<string, DownloadTicket>
							{
								{ nodeId, new DownloadTicket(ticket) },
							};

						var request = new PrepareUpdateRequest
						{
							UpdateVersion = updateRequest.UpdateVersion,
							SourceNodeIdentifier = sourceNode,
						};

						request.DownloadTicketsByNodeIdentifier.Add(localTicketDictionary);

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

						var callInvoker = GetCallInvokerForNode(node, out var registration);
						var client = new GrpcSwarmSharedService.GrpcSwarmSharedServiceClient(callInvoker);

						try
						{
							await client.PrepareUpdateAsync(
								body,
								cancellationToken: cancellationToken);
							return true;
						}
						catch (Exception ex)
						{
							logger.LogWarning(ex, "Prepare update call for node {node} failed", node.Identifier);
							return false;
						}
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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="Dictionary{TKey, TValue}"/> of unique <see cref="FileTicketResponse"/>s keyed by their <see cref="Api.Models.Internal.SwarmServer.Identifier"/>.</returns>
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
				.Where(node => node.Identifier != swarmConfigurationOptions.CurrentValue.Identifier)
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
				var callInvoker = GetCallInvokerForNode(swarmServer, out var registration);
				var client = new GrpcSwarmNodeService.GrpcSwarmNodeServiceClient(callInvoker);

				try
				{
					await client.HealthCheckAsync(
						new HealthCheckRequest
						{
							Registration = registration,
						},
						cancellationToken: cancellationToken);
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
					var callInvoker = GetCallInvokerForNode(null, out var registration);
					var client = new GrpcSwarmNodeService.GrpcSwarmNodeServiceClient(callInvoker);

					await client.HealthCheckAsync(
						new HealthCheckRequest
						{
							Registration = registration,
						},
						cancellationToken: cancellationToken);
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
			var currentSwarmConfiguration = swarmConfigurationOptions.CurrentValue;
			logger.LogInformation("Attempting to register with swarm controller at {controllerAddress}...", currentSwarmConfiguration.ControllerAddress);

			var callInvoker = GetCallInvokerForNode(null, out _);
			var client = new GrpcSwarmControllerService.GrpcSwarmControllerServiceClient(callInvoker);
			try
			{
				try
				{
					var response = await client.RegisterNodeAsync(
						new RegisterNodeRequest
						{
							RegisteringNode = new Grpc.SwarmServer
							{
								Address = currentSwarmConfiguration.Address!.ToString(),
								PublicAddress = currentSwarmConfiguration.PublicAddress?.ToString(),
								Identifier = currentSwarmConfiguration.Identifier,
							},
							SwarmProtocolVersion = new GrpcVersion(
								Version.Parse(
									MasterVersionsAttribute.Instance.RawSwarmProtocolVersion)),
						},
						cancellationToken: cancellationToken);

					var registrationId = response.Registration.ToGuid();
					tokenFactory.SigningKeyBytes = response.TokenSigningKey.ToByteArray();

					logger.LogInformation("Sucessfully registered with ID {registrationId}", registrationId);
					controllerRegistration = registrationId;
					lastControllerHealthCheck = DateTimeOffset.UtcNow;
					return SwarmRegistrationResult.Success;
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Unable to register with swarm!");
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
				var callInvoker = GetCallInvokerForNode(swarmServer, out var registration);
				var client = new GrpcSwarmNodeService.GrpcSwarmNodeServiceClient(callInvoker);
				try
				{
					var request = new UpdateNodeListRequest
					{
						Registration = registration,
					};
					request.NodeList.AddRange(currentSwarmServers.Select(swarmServer => new NodeInformation
					{
						Controller = swarmServer.Controller,
						SwarmServer = new Grpc.SwarmServer
						{
							Address = swarmServer.Address!.ToString(),
							PublicAddress = swarmServer.PublicAddress?.ToString(),
							Identifier = swarmServer.Identifier,
						},
					}));

					await client.UpdateNodeListAsync(
						request,
						cancellationToken: cancellationToken);
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
						cancellationToken)
						.AsTask();

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
		/// Gets the <see cref="Api.Models.Internal.SwarmServer.Identifier"/> from a given <paramref name="registrationId"/>.
		/// </summary>
		/// <param name="registrationId">The registration <see cref="Guid"/>.</param>
		/// <returns>The registered <see cref="Api.Models.Internal.SwarmServer.Identifier"/> or <see langword="null"/> if it does not exist.</returns>
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

		/// <summary>
		/// Get the <see cref="CallInvoker"/> and <see cref="SwarmRegistration"/> for a given <paramref name="swarmServer"/>.
		/// </summary>
		/// <param name="swarmServer">The optional <see cref="Api.Models.Internal.SwarmServer"/> to connect to. If <see langword="null"/> the <see cref="SwarmConfiguration.ControllerAddress"/> will be used.</param>
		/// <param name="swarmRegistration">The <see cref="SwarmRegistration"/> for the <paramref name="swarmServer"/>, if any.</param>
		/// <returns>The <see cref="CallInvoker"/> to use for calling the target <paramref name="swarmServer"/>.</returns>
		private CallInvoker GetCallInvokerForNode(Api.Models.Internal.SwarmServer? swarmServer, out SwarmRegistration? swarmRegistration)
		{
			string CreateSwarmAuthorizationHeader() => $"{SwarmConstants.AuthenticationSchemeAndPolicy} {swarmConfigurationOptions.CurrentValue.PrivateKey}";

			lock (nodeCallInvokers)
			{
				CallInvoker? callInvoker;
				if (swarmServer == null)
				{
					if (controllerCallInvoker == null)
					{
						var controllerAddress = swarmConfigurationOptions.CurrentValue.ControllerAddress;
						if (controllerAddress == null)
							throw new InvalidOperationException("Controller address was null!");

						controllerCallInvoker = grpcChannelFactory.CreateCallInvoker(controllerAddress, CreateSwarmAuthorizationHeader);
					}

					if (controllerRegistration.HasValue)
						swarmRegistration = new SwarmRegistration
						{
							Id = controllerRegistration.Value.ToString(),
						};
					else
						swarmRegistration = null;

					callInvoker = controllerCallInvoker;
				}
				else
				{
					lock (swarmServers!)
						if (registrationIdsAndTimes!.TryGetValue(swarmServer.Identifier!, out var registrationIdAndTime))
							swarmRegistration = new SwarmRegistration
							{
								Id = registrationIdAndTime.RegistrationId.ToString(),
							};
						else
							swarmRegistration = null;

					if (!nodeCallInvokers.TryGetValue(swarmServer.Identifier!, out callInvoker))
					{
						callInvoker = grpcChannelFactory.CreateCallInvoker(swarmServer.Address!, CreateSwarmAuthorizationHeader);
						nodeCallInvokers.Add(swarmServer.Identifier!, callInvoker);
					}
				}

				return callInvoker;
			}
		}
	}
}
