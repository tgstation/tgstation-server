using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions.Converters;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Helps keep servers connected to the same database in sync by coordinating updates.
	/// </summary>
	sealed class SwarmService : ISwarmService, ISwarmOperations, IRestartHandler, IDisposable
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
		/// See <see cref="JsonSerializerSettings"/> for the swarm system.
		/// </summary>
		static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Converters = new JsonConverter[]
			{
				new VersionConverter(),
				new BoolConverter()
			},
			DefaultValueHandling = DefaultValueHandling.Ignore,
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore
		};

		/// <summary>
		/// If the swarm system is enabled.
		/// </summary>
		bool SwarmMode => swarmConfiguration.PrivateKey != null;

		/// <summary>
		/// Lazily constructed <see cref="IRestartRegistration"/>.
		/// </summary>
		readonly Lazy<IRestartRegistration> lazyRestartRegistration;

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
		/// The <see cref="IHttpClientFactory"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly IHttpClientFactory httpClientFactory;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IServerUpdateInitiator"/> for the <see cref="SwarmService"/>.
		/// </summary>
		readonly IServerUpdateInitiator serverUpdater;

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
		/// <see cref="List{T}"/> of connected <see cref="SwarmServer"/>s.
		/// </summary>
		readonly List<SwarmServer> swarmServers;

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
		/// If <see cref="IRestartHandler.HandleRestart(Version, CancellationToken)"/> was called.
		/// </summary>
		bool restarting;

		/// <summary>
		/// If the <see cref="swarmServers"/> list has been updated and needs to be resent to clients.
		/// </summary>
		bool serversDirty;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmService"/> <see langword="class"/>.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="databaseSeeder">The value of <see cref="databaseSeeder"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> to register ourselves as a <see cref="IRestartHandler"/> with.</param>
		/// <param name="serverUpdateInitiator">The value of <see cref="serverUpdater"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public SwarmService(
			IDatabaseContextFactory databaseContextFactory,
			IDatabaseSeeder databaseSeeder,
			IAssemblyInformationProvider assemblyInformationProvider,
			IHttpClientFactory httpClientFactory,
			IServerControl serverControl,
			IServerUpdateInitiator serverUpdateInitiator,
			IAsyncDelayer asyncDelayer,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<SwarmService> logger)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			if (serverControl == null)
				throw new ArgumentNullException(nameof(serverControl));

			serverUpdater = serverUpdateInitiator ?? throw new ArgumentNullException(nameof(serverUpdateInitiator));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
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
				if (swarmController)
					registrationIds = new Dictionary<string, Guid>();

				swarmServers = new List<SwarmServer>
				{
					new SwarmServer
					{
						Address = swarmConfiguration.Address,
						Controller = swarmController,
						Identifier = swarmConfiguration.Identifier
					}
				};

				updateSynchronizationLock = new object();
			}

			lazyRestartRegistration = new Lazy<IRestartRegistration>(() => serverControl.RegisterForRestart(this));
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

			await AbortUpdate(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task AbortUpdate(CancellationToken cancellationToken)
		{
			if (!SwarmMode)
				return;

			logger.LogInformation("Aborting swarm update!");
			updateCommitTcs?.TrySetResult(false);
			updateCommitTcs = null;
			nodesThatNeedToBeReadyToCommit = null;
			targetUpdateVersion = null;

			using var httpClient = httpClientFactory.CreateClient();
			async Task SendRemoteAbort(SwarmServer swarmServer)
			{
				using var request = PrepareSwarmRequest(
					swarmServer,
					HttpMethod.Delete,
					SwarmConstants.UpdateRoute,
					null);

				using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();
			}

			Task task;
			if (!swarmController)
				task = SendRemoteAbort(new SwarmServer
				{
					Address = swarmConfiguration.ControllerAddress,
				});
			else
			{
				lock (swarmServers)
					task = Task.WhenAll(
						swarmServers
							.Where(x => !x.Controller)
							.Select(
								x => SendRemoteAbort(x)));
			}

			await task.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<bool> CommitUpdate(CancellationToken cancellationToken)
		{
			if (!SwarmMode)
				return true;

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
					using var commitReadyResponse = await httpClient.SendAsync(commitReadyRequest, cancellationToken).ConfigureAwait(false);
					commitReadyResponse.EnsureSuccessStatusCode();
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Unable to send ready-commit to swarm controller!");
					await AbortUpdate(cancellationToken).ConfigureAwait(false);
					return false;
				}
			}

			// wait for the update commit TCS
			var commitTcsTask = updateCommitTcs?.Task;
			if (commitTcsTask == null)
			{
				logger.LogDebug("Update commit failed, no pending task completion source!");
				await AbortUpdate(cancellationToken).ConfigureAwait(false);
				return false;
			}

			var commitGoAhead = await commitTcsTask.ConfigureAwait(false) && updateCommitTcs?.Task == commitTcsTask;
			if (!commitGoAhead)
			{
				logger.LogDebug("Update commit failed!");
				await AbortUpdate(cancellationToken).ConfigureAwait(false);
				return false;
			}

			logger.LogTrace("Update commit task complete");

			// on nodes, it means we can go straight ahead
			if (!swarmController)
				return true;

			// on the controller, we first need to signal for nodes to go ahead
			// if anything fails at this point, there's nothing we can do
			logger.LogDebug("Sending remote commit message to nodes...");
			async Task SendRemoteCommitUpdate(SwarmServer swarmServer)
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
					using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
					response.EnsureSuccessStatusCode();
				}
				catch (Exception ex)
				{
					logger.LogCritical(ex, "Failed to send update commit request to node {0}!", swarmServer.Identifier);
				}
			}

			Task task;
			lock (swarmServers)
				task = Task.WhenAll(
					swarmServers
						.Where(x => !x.Controller)
						.Select(
							x => SendRemoteCommitUpdate(x)));

			await task.ConfigureAwait(false);
			return true;
		}

		/// <inheritdoc />
		public ICollection<SwarmServer> GetSwarmServers()
		{
			if (!SwarmMode)
				return null;

			lock (swarmServers)
				return swarmServers.ToList();
		}

		/// <inheritdoc />
		public async Task<bool> PrepareUpdate(Version version, CancellationToken cancellationToken)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			if (!SwarmMode)
				return true;

			logger.LogTrace("Begin PrepareUpdate...");
			if (version == targetUpdateVersion)
			{
				logger.LogDebug("Prepare update short circuit!");
				return true;
			}

			using var httpClient = httpClientFactory.CreateClient();
			async Task<bool> RemotePrepareUpdate(SwarmServer swarmServer)
			{
				using var request = PrepareSwarmRequest(
					swarmServer,
					HttpMethod.Put,
					SwarmConstants.UpdateRoute,
					new SwarmUpdateRequest
					{
						UpdateVersion = version
					});

				using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
				return response.IsSuccessStatusCode;
			}

			if (!swarmController)
			{
				logger.LogDebug("Forwarding update request to swarm controller...");

				return await RemotePrepareUpdate(null).ConfigureAwait(false);
			}

			var selfPrepare = await PrepareUpdateImpl(version, true, cancellationToken).ConfigureAwait(false);
			if (!selfPrepare)
				return false;

			try
			{
				logger.LogTrace("Sending remote prepare nodes...");
				List<Task<bool>> tasks;
				lock (swarmServers)
					tasks = swarmServers
						.Where(x => !x.Controller)
						.Select(x => RemotePrepareUpdate(x))
						.ToList();

				await Task.WhenAll(tasks);

				// if all succeeds...
				if (tasks.All(x => x.Result))
				{
					logger.LogDebug("Distributed prepare for update to version {0} complete.", version);
					return true;
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error remotely preparing updates!");
			}

			logger.LogDebug("Distrubuted prepare failed!");
			await AbortUpdate(cancellationToken).ConfigureAwait(false);
			return false;
		}

		/// <inheritdoc />
		public Task<bool> PrepareUpdateFromController(Version version, CancellationToken cancellationToken)
			=> PrepareUpdateImpl(version, false, cancellationToken);

		/// <summary>
		/// Implementation of <see cref="PrepareUpdate(Version, CancellationToken)"/>,
		/// </summary>
		/// <param name="version">The <see cref="Version"/> being updated to.</param>
		/// <param name="initiator">Whether or not the update request originated on this server.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in whether or not the update should proceed.</returns>
		async Task<bool> PrepareUpdateImpl(Version version, bool initiator, CancellationToken cancellationToken)
		{
			logger.LogTrace("PrepareUpdateImpl {0}...", version);
			var shouldAbort = false;
			try
			{
				lock (updateSynchronizationLock)
				{
					if (targetUpdateVersion == version)
					{
						logger.LogTrace("PrepareUpdateFromController early out, already prepared!");
						return true;
					}

					if (targetUpdateVersion != null)
					{
						logger.LogDebug("Aborting update preparation, version {0} already prepared!", targetUpdateVersion);
						shouldAbort = true;
						return false;
					}

					targetUpdateVersion = version;
				}

				updateCommitTcs = new TaskCompletionSource<bool>();
				if (!initiator)
				{ 
					var updateApplyResult = await serverUpdater.BeginUpdate(
						version,
						cancellationToken)
						.ConfigureAwait(false);
					if (updateApplyResult != ServerUpdateResult.Started)
					{
						logger.LogWarning("Failed to prepare update! Result: {0}", updateApplyResult);
						shouldAbort = true;
						return false;
					}

					lock (swarmServers)
						nodesThatNeedToBeReadyToCommit = new List<string>(
							swarmServers
								.Where(x => !x.Controller)
								.Select(x => x.Identifier));
				}

				logger.LogDebug("Prepared for update to version {0}", version);
				return true;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to prepare update!");
				shouldAbort = true;
				return false;
			}
			finally
			{
				if (shouldAbort)
					await AbortUpdate(cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task<SwarmRegistrationResult> Initialize(CancellationToken cancellationToken)
		{
			if (SwarmMode)
				logger.LogInformation(
					"Swarm mode enabled ({0})",
					swarmController
						? "controller"
						: "node");
			else
				logger.LogTrace("Swarm mode disabled");

			var _ = lazyRestartRegistration.Value;

			if (swarmController)
			{
				await databaseContextFactory.UseContext(
					databaseContext => databaseSeeder.Initialize(databaseContext, cancellationToken))
					.ConfigureAwait(false);
				if (SwarmMode)
					serverHealthCheckTask = HealthCheckLoop(serverHealthCheckCancellationTokenSource.Token);

				return SwarmRegistrationResult.Success;
			}

			return await RegisterWithController(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task Shutdown(CancellationToken cancellationToken)
		{
			// downgrade the db if necessary
			if (swarmController)
			{
				serverHealthCheckCancellationTokenSource?.Cancel();
				if (serverHealthCheckTask != null)
					await serverHealthCheckTask.ConfigureAwait(false);

				if (targetUpdateVersion != null
					&& targetUpdateVersion < assemblyInformationProvider.Version)
					await databaseContextFactory.UseContext(
						db => databaseSeeder.Downgrade(db, targetUpdateVersion, cancellationToken))
						.ConfigureAwait(false);

				// we don't tell nodes about us unregistering, they'll try to reconnect eventually.
				if (SwarmMode)
					logger.LogTrace("Swarm controller shutdown");

				return;
			}

			// if we restart a node, we don't want to unregister it so the controller doesn't try to update without it
			// if we're shutting it down, though we should unregister it
			if (restarting)
			{
				logger.LogTrace("Not unregistering from swarm controller as we are restarting");
				return;
			}

			logger.LogInformation("Unregistering from swarm controller...");
			using var httpClient = httpClientFactory.CreateClient();
			using var request = PrepareSwarmRequest(
				null,
				HttpMethod.Delete,
				SwarmConstants.RegisterRoute,
				null);

			using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();
		}

		/// <summary>
		/// Ping each node to see that they are still running.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task HealthCheckNodes(CancellationToken cancellationToken)
		{
			using var httpClient = httpClientFactory.CreateClient();

			List<SwarmServer> currentSwarmServers;
			lock (swarmServers)
				currentSwarmServers = swarmServers.ToList();

			async Task HealthRequestForServer(SwarmServer swarmServer)
			{
				using var request = PrepareSwarmRequest(
					swarmServer,
					HttpMethod.Get,
					String.Empty,
					null);

				try
				{
					using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
					response.EnsureSuccessStatusCode();
					return;
				}
				catch (Exception ex)
				{
					logger.LogWarning(
						ex,
						"Error during swarm server health check on node '{0}'! Unregistering...",
						swarmServer.Identifier);
				}

				lock (swarmServers)
				{
					swarmServers.Remove(swarmServer);
					registrationIds.Remove(swarmServer.Identifier);
				};
			}

			await Task.WhenAll(
				currentSwarmServers
					.Where(x => !x.Controller)
					.Select(
						x => HealthRequestForServer(x)))
					.ConfigureAwait(false);

			lock (swarmServers)
				if (swarmServers.Count != currentSwarmServers.Count)
					serversDirty = true;

			if (serversDirty)
				await SendUpdatedServerListToNodes(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Ping the swarm controller to see that it is still running. If need be, reregister.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task HealthCheckController(CancellationToken cancellationToken)
		{
			using var request = PrepareSwarmRequest(
				null,
				HttpMethod.Get,
				String.Empty,
				null);
			using var httpClient = httpClientFactory.CreateClient();

			try
			{
				using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();
				logger.LogTrace("Health check successful");
				return;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error during swarm controller health check! Attempting to re-register...");
				controllerRegistration = null;
				lastControllerHealthCheck = null;
			}

			SwarmRegistrationResult registrationResult;
			for (var I = 1; ; ++I)
			{
				logger.LogInformation("Swarm re-registration attempt {0}...");
				registrationResult = await RegisterWithController(cancellationToken).ConfigureAwait(false);

				if (registrationResult == SwarmRegistrationResult.Success)
					return;

				if (registrationResult == SwarmRegistrationResult.Unauthorized)
				{
					logger.LogError("Swarm re-registration failed, controller's private key has changed!");
					break;
				}

				if (registrationResult == SwarmRegistrationResult.VersionMismatch)
				{
					logger.LogError("Swarm Re-registration failed, controller's TGS version has changed!");
					break;
				}
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
			logger.LogInformation("Attempting to register with swarm controller at {0}...", swarmConfiguration.ControllerAddress);
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
					Address = swarmConfiguration.Address
				},
				requestedRegistrationId);

			try
			{
				using var response = await httpClient.SendAsync(registrationRequest, cancellationToken).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					logger.LogInformation("Sucessfully registered with ID {0}", requestedRegistrationId);
					controllerRegistration = requestedRegistrationId;
					lastControllerHealthCheck = DateTimeOffset.UtcNow;
					return SwarmRegistrationResult.Success;
				}

				logger.LogWarning("Unable to register with swarm: HTTP {0}!", response.StatusCode);

				if (response.StatusCode == HttpStatusCode.Unauthorized)
					return SwarmRegistrationResult.Unauthorized;

				if (response.StatusCode == HttpStatusCode.UpgradeRequired)
					return SwarmRegistrationResult.VersionMismatch;

				logger.LogWarning("Error registering with swarm controller: HTTP {0}", response.StatusCode);
				try
				{
					var responseData = await response.Content.ReadAsStringAsync();
					if (!String.IsNullOrWhiteSpace(responseData))
						logger.LogDebug("Response:{0}{1}", Environment.NewLine, responseData);
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
			logger.LogDebug("Sending updated server list to all nodes...");
			List<SwarmServer> currentSwarmServers;
			lock (swarmServers)
				currentSwarmServers = swarmServers.ToList();

			using var httpClient = httpClientFactory.CreateClient();
			async Task UpdateRequestForServer(SwarmServer swarmServer)
			{
				using var request = PrepareSwarmRequest(
					swarmServer,
					HttpMethod.Post,
					String.Empty,
					new SwarmServersUpdateRequest
					{
						SwarmServers = currentSwarmServers
					});

				try
				{
					using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
					response.EnsureSuccessStatusCode();
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Error during swarm server list update for node '{0}'! Unregistering...", swarmServer.Identifier);

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
					.Select(x => UpdateRequestForServer(x)))
				.ConfigureAwait(false);
			serversDirty = false;
		}

		/// <summary>
		/// Prepares a <see cref="HttpRequestMessage"/> for swarm communication.
		/// </summary>
		/// <param name="swarmServer">The <see cref="SwarmServer"/> the message is for, if null will be sent to swarm controller.</param>
		/// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
		/// <param name="subroute">The route on <see cref="SwarmConstants.ControllerRoute"/> to use.</param>
		/// <param name="body">The body <see cref="object"/> if any.</param>
		/// <param name="registrationIdOverride">An optional override to the <see cref="SwarmConstants.RegistrationIdHeader"/>.</param>
		/// <returns>A new <see cref="HttpRequestMessage"/>.</returns>
		HttpRequestMessage PrepareSwarmRequest(
			SwarmServer swarmServer,
			HttpMethod httpMethod,
			string subroute,
			object body,
			Guid? registrationIdOverride = null)
		{
			swarmServer ??= new SwarmServer
			{
				Address = swarmConfiguration.ControllerAddress,
			};

			subroute = $"{SwarmConstants.ControllerRoute}/{subroute}";
			logger.LogTrace(
				"{0} {1} to swarm server {2}",
				httpMethod,
				subroute,
				swarmServer.Identifier ?? swarmServer.Address.ToString());

			var request = new HttpRequestMessage(
				httpMethod,
				swarmServer.Address + subroute.Substring(1));

			request.Headers.Add(SwarmConstants.ApiKeyHeader, swarmConfiguration.PrivateKey);
			request.Headers.UserAgent.Clear();
			request.Headers.UserAgent.Add(assemblyInformationProvider.ProductInfoHeaderValue);
			request.Headers.Accept.Clear();
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
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

			try
			{
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

		/// <inheritdoc />
		public void UpdateSwarmServersList(IEnumerable<SwarmServer> swarmServers)
		{
			if (swarmServers == null)
				throw new ArgumentNullException(nameof(swarmServers));

			if (swarmController)
				throw new InvalidOperationException("Cannot UpdateSwarmServersList on swarm controller!");

			lock (this.swarmServers)
			{
				this.swarmServers.Clear();
				this.swarmServers.AddRange(swarmServers);
				logger.LogDebug("Updated swarm server list with {0} total nodes", this.swarmServers.Count);
			}
		}

		/// <summary>
		/// Timed loop for calling <see cref="HealthCheckNodes(CancellationToken)"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the rinning operation</returns>
		async Task HealthCheckLoop(CancellationToken cancellationToken)
		{
			logger.LogTrace("Starting HealthCheckLoop...");
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					var delay = swarmController
						? TimeSpan.FromMinutes(ControllerHealthCheckIntervalMinutes)
						: lastControllerHealthCheck.HasValue
							? (lastControllerHealthCheck.Value.AddMinutes(NodeHealthCheckIntervalMinutes) - DateTimeOffset.UtcNow)
							: TimeSpan.FromMinutes(NodeHealthCheckIntervalMinutes);
					await asyncDelayer.Delay(
						delay,
						cancellationToken)
						.ConfigureAwait(false);

					if (!swarmController)
					{
						if (!lastControllerHealthCheck.HasValue)
						{
							logger.LogTrace("Not registered with controller, skipping health check.");
							continue; // unregistered
						}

						if ((DateTimeOffset.UtcNow - lastControllerHealthCheck.Value).TotalMinutes < NodeHealthCheckIntervalMinutes)
						{
							logger.LogTrace("Controller seems to be active, skipping health check.");
							continue;
						}
					}

					logger.LogDebug("Performing swarm health check...");
					try
					{
						if (swarmController)
							await HealthCheckNodes(cancellationToken).ConfigureAwait(false);
						else
							await HealthCheckController(cancellationToken).ConfigureAwait(false);
					}
					catch (Exception ex) when (!(ex is OperationCanceledException))
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
					logger.LogInformation("Not registering node {0} as a distributed update is in progress.", node.Identifier);
					return false;
				}

				lock (swarmServers)
				{
					if (registrationIds.Any(x => x.Value == registrationId))
					{
						var preExistingRegistrationKvp = registrationIds.FirstOrDefault(x => x.Value == registrationId);
						if (preExistingRegistrationKvp.Key == node.Identifier)
						{
							logger.LogWarning("Node {0} has already registered!", node.Identifier);
							return true;
						}

						logger.LogWarning(
							"Registration ID collision! Node {0} tried to register with {1}'s registration ID: {2}",
							node.Identifier,
							preExistingRegistrationKvp.Key,
							registrationId);
						return false;
					}

					if (registrationIds.TryGetValue(node.Identifier, out var oldRegistration))
					{
						logger.LogInformation("Node {0} is re-registering without first unregistering. Indicative of restart.", node.Identifier);
						swarmServers.RemoveAll(x => x.Identifier == node.Identifier);
						registrationIds.Remove(node.Identifier);
					}

					swarmServers.Add(new SwarmServer
					{
						Address = node.Address,
						Identifier = node.Identifier,
						Controller = false,
					});
					registrationIds.Add(node.Identifier, registrationId);
				}
			}

			logger.LogInformation("Registered node {0} with ID {1}", node.Identifier, registrationId);
			serversDirty = true;
			return true;
		}

		/// <inheritdoc />
		public Task HandleRestart(Version updateVersion, CancellationToken cancellationToken)
		{
			restarting = true;
			return Task.CompletedTask;
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
				await AbortUpdate(cancellationToken).ConfigureAwait(false);
				return false;
			}

			var nodeList = nodesThatNeedToBeReadyToCommit;
			if (nodeList == null)
			{
				logger.LogDebug("Ignoring ready-commit from node {0} as the update appears to have been aborted.", nodeIdentifier);
				return false;
			}

			logger.LogDebug("Node {0} is ready to commit.", nodeIdentifier);
			lock (nodeList)
			{
				nodeList.Remove(nodeIdentifier);
				if (nodeList.Count == 0)
				{
					logger.LogTrace("All nodes ready, update commit is a go once controller is ready");
					var commitTcs = updateCommitTcs;
					commitTcs?.TrySetResult(true);
					return commitTcs != null;
				}
			}

			return true;
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
					logger.LogDebug("A node that was to be looked up ({0}) disappeared from our records!", registrationId);
					return null;
				}

				return registrationIds.First(x => x.Value == registrationId).Key;
			}
		}

		/// <inheritdoc />
		public async Task UnregisterNode(Guid registrationId, CancellationToken cancellationToken)
		{
			if (!swarmController)
				throw new InvalidOperationException("Cannot UnregisterNode on swarm node!");

			logger.LogTrace("UnregisterNode {0}", registrationId);
			var nodeIdentifier = NodeIdentifierFromRegistration(registrationId);
			if (nodeIdentifier == null)
				return;

			logger.LogInformation("Unregistering node {0}...", nodeIdentifier);
			await AbortUpdate(cancellationToken).ConfigureAwait(false);
			lock (swarmServers)
			{
				swarmServers.RemoveAll(x => x.Identifier == nodeIdentifier);
				registrationIds.Remove(nodeIdentifier);
			}

			serversDirty = true;
		}
	}
}
