using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;
using Moq.Language.Flow;

using Tgstation.Server.Common;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Swarm.Tests
{
	sealed class TestableSwarmNode : IAsyncDisposable
	{
		public SwarmController Controller { get; private set; }

		public SwarmService Service { get; private set; }

		public SwarmConfiguration Config { get; }

		public SwarmRpcMapper RpcMapper { get; }

		public bool UpdateCommits { get; set; }

		public CancellationTokenSource CriticalCancellationTokenSource { get; private set; }

		public ServerUpdateResult UpdateResult { get; set; }
		public Task<SwarmCommitResult?> UpdateTask { get; private set; }

		public bool Initialized { get; private set; }

		public bool Shutdown { get; private set; }

		readonly Mock<IHttpClient> mockHttpClient;
		readonly Mock<IDatabaseContextFactory> mockDBContextFactory;
		readonly Mock<IDatabaseSeeder> mockDatabaseSeeder;
		readonly ISetup<IDatabaseSeeder, Task> mockDatabaseSeederInitialize;

		readonly Action recreateControllerAndService;

		readonly ILogger logger;

		public static void Link(params TestableSwarmNode[] nodes)
		{
			var configControllerSet = nodes.Select(x => (x.Config, x)).ToList();

			_ = configControllerSet.Single(x => x.Config.ControllerAddress == null);
			Assert.IsTrue(
				configControllerSet.All(
					tuple1 => !String.IsNullOrWhiteSpace(tuple1.Config.PrivateKey)
					&& configControllerSet.All(tuple2 => tuple1.Config.PrivateKey == tuple2.Config.PrivateKey)),
				"This test doesn't support authentication issues.");

			foreach (var node in nodes)
			{
				if (node.Config.ControllerAddress != null)
					node.Config.UpdateRequiredNodeCount = 0;
				else
					node.Config.UpdateRequiredNodeCount = (uint)nodes.Length - 1;
				node.RpcMapper.Register(configControllerSet);
			}
		}

		public TestableSwarmNode(
			ILoggerFactory loggerFactory,
			SwarmConfiguration swarmConfiguration,
			Version mockVersion = null)
		{
			this.Config = swarmConfiguration;

			var mockOptions = new Mock<IOptions<SwarmConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(swarmConfiguration);

			var realVersion = new AssemblyInformationProvider().Version;
			var mockAssemblyInformationProvider = new Mock<IAssemblyInformationProvider>();
			mockAssemblyInformationProvider.SetupGet(x => x.Version).Returns(mockVersion ?? realVersion);

			var mockDatabaseContext = Mock.Of<IDatabaseContext>();

			mockDatabaseSeeder = new Mock<IDatabaseSeeder>();
			mockDatabaseSeederInitialize = new Mock<IDatabaseSeeder>().Setup(x => x.Initialize(mockDatabaseContext, It.IsAny<CancellationToken>()));
			mockDBContextFactory = new Mock<IDatabaseContextFactory>();
			mockDBContextFactory
				.Setup(x => x.UseContext(It.IsNotNull<Func<IDatabaseContext, Task>>()))
				.Callback<Func<IDatabaseContext, Task>>((func) => func(mockDatabaseContext));

			var mockHttpClientFactory = new Mock<IAbstractHttpClientFactory>();
			mockHttpClient = new Mock<IHttpClient>();
			mockHttpClientFactory.Setup(x => x.CreateClient()).Returns(mockHttpClient.Object);

			var mockAsyncDelayer = new Mock<IAsyncDelayer>();
			mockAsyncDelayer.Setup(
				x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
				.Returns<TimeSpan, CancellationToken>(
					(delay, ct) => Task.Delay(TimeSpan.FromMilliseconds(100), ct));

			var mockServerUpdater = new Mock<IServerUpdater>();

			RpcMapper = new SwarmRpcMapper(mockHttpClient, loggerFactory.CreateLogger($"SwarmRpcMapper-{swarmConfiguration.Identifier}"));

			mockServerUpdater
				.Setup(x => x.BeginUpdate(It.IsNotNull<SwarmService>(), It.IsNotNull<Version>(), It.IsAny<CancellationToken>()))
				.Returns(BeginUpdate);

			UpdateResult = ServerUpdateResult.Started;
			UpdateCommits = true;

			logger = loggerFactory.CreateLogger($"TestableSwarmNode-{swarmConfiguration.Identifier}");

			var runCount = 0;
			void RecreateControllerAndService()
			{
				logger.LogTrace("RecreateControllerAndService...");
				var run = ++runCount;
				Initialized = false;
				Shutdown = false;

				CriticalCancellationTokenSource?.Dispose();
				CriticalCancellationTokenSource = new CancellationTokenSource();

				var serviceLogger = new Logger<SwarmService>(loggerFactory);
				// HAX HAX HAX
				serviceLogger
					.GetType()
					.GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance)
					.SetValue(serviceLogger, loggerFactory.CreateLogger($"SwarmService-{swarmConfiguration.Identifier}{(run != 1 ? $"-Run{run}": String.Empty)}"));

				Service = new SwarmService(
					mockDBContextFactory.Object,
					mockDatabaseSeeder.Object,
					mockAssemblyInformationProvider.Object,
					mockHttpClientFactory.Object,
					mockServerUpdater.Object,
					mockAsyncDelayer.Object,
					mockOptions.Object,
					serviceLogger);

				Controller = new SwarmController(
					Service,
					RpcMapper,
					mockAssemblyInformationProvider.Object,
					mockOptions.Object,
					loggerFactory.CreateLogger<SwarmController>());
			}

			RecreateControllerAndService();
			recreateControllerAndService = RecreateControllerAndService;
		}

		public async Task SimulateReboot(CancellationToken cancellationToken)
		{
			logger.LogTrace("SimulateReboot...");
			await ShutdownService(cancellationToken);
			recreateControllerAndService();
		}

		public async ValueTask DisposeAsync()
		{
			logger.LogTrace("DisposeAsync...");
			await ShutdownService(default);
			RpcMapper.Dispose();
			CriticalCancellationTokenSource.Dispose();
		}

		private async Task ShutdownService(CancellationToken cancellationToken)
		{
			logger.LogTrace("ShutdownService...");
			Shutdown = true;
			CriticalCancellationTokenSource.Cancel();
			if (UpdateTask != null)
				try
				{
					await UpdateTask;
				}
				catch (OperationCanceledException)
				{
				}
			UpdateTask = null;

			await Service.Shutdown(cancellationToken);
			Service.Dispose();
		}

		public async Task<SwarmRegistrationResult?> TryInit(bool cancel = false)
		{
			if (Initialized)
				Assert.Fail("Initialized twice!");

			if (!cancel)
				mockDatabaseSeederInitialize.Returns(Task.CompletedTask).Verifiable();
			else
				mockDatabaseSeederInitialize.ThrowsAsync(new TaskCanceledException()).Verifiable();

			Task<SwarmRegistrationResult> Invoke() => Service.Initialize(default);

			SwarmRegistrationResult? result;
			if (cancel)
			{
				await Assert.ThrowsExceptionAsync<OperationCanceledException>(Invoke);
				result = null;
			}
			else
			{
				result = await Invoke();
				Initialized = true;
			}

			if (Config.ControllerAddress == null)
				mockDatabaseSeeder.VerifyAll();
			else
			{
				Assert.IsFalse(mockDatabaseSeeder.Invocations.Any());
				Assert.IsFalse(mockDBContextFactory.Invocations.Any());
			}

			return result;
		}

		Task<ServerUpdateResult> BeginUpdate(ISwarmService swarmService, Version version, CancellationToken cancellationToken)
		{
			if (UpdateResult == ServerUpdateResult.Started)
			{
				UpdateTask = ExecuteUpdate(version, cancellationToken, CriticalCancellationTokenSource.Token);
			}

			return Task.FromResult(UpdateResult);
		}

		async Task<SwarmCommitResult?> ExecuteUpdate(Version version, CancellationToken cancellationToken, CancellationToken criticalCancellationToken)
		{
			await Task.Yield(); // Important to simulate some actual kind of asyncronicity here

			await Service.PrepareUpdate(version, cancellationToken);

			if (UpdateCommits)
			{
				return await Service.CommitUpdate(criticalCancellationToken);
			}

			return null;
		}
	}
}
