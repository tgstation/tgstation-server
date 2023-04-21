using System;
using System.Linq;
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
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Tests.Swarm
{
	sealed class TestableSwarmNode : IDisposable, IServerUpdateExecutor
	{
		public SwarmController Controller { get; }

		public SwarmService Service { get; }

		public SwarmConfiguration Config { get; }

		public SwarmRpcMapper RpcMapper { get; }

		public bool Initialized { get; private set; }

		readonly Mock<IHttpClient> mockHttpClient;
		readonly Mock<IServerControl> mockServerControl;
		readonly Mock<IDatabaseContextFactory> mockDBContextFactory;
		readonly Mock<IDatabaseSeeder> mockDatabaseSeeder;
		readonly ISetup<IDatabaseSeeder, Task> mockDatabaseSeederInitialize;

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

			mockServerControl = new Mock<IServerControl>();
			mockServerControl.Setup(x => x.TryStartUpdate(this, It.IsNotNull<Version>())).Returns(TryStartUpdate);
			mockServerControl.Setup(x => x.RegisterForRestart(It.IsNotNull<IRestartHandler>())).Returns(Mock.Of<IRestartRegistration>());

			var mockHttpClientFactory = new Mock<IAbstractHttpClientFactory>();
			mockHttpClient = new Mock<IHttpClient>();
			mockHttpClientFactory.Setup(x => x.CreateClient()).Returns(mockHttpClient.Object);

			var mockAsyncDelayer = new Mock<IAsyncDelayer>();
			mockAsyncDelayer.Setup(
				x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
				.Returns<TimeSpan, CancellationToken>(
					(delay, ct) => Task.Delay(TimeSpan.FromSeconds(1), ct));

			var mockServerUpdater = new Mock<IServerUpdater>();

			RpcMapper = new SwarmRpcMapper(mockHttpClient);

			Service = new SwarmService(
				mockDBContextFactory.Object,
				mockDatabaseSeeder.Object,
				mockAssemblyInformationProvider.Object,
				mockHttpClientFactory.Object,
				mockServerControl.Object,
				mockServerUpdater.Object,
				mockAsyncDelayer.Object,
				mockOptions.Object,
				loggerFactory.CreateLogger<SwarmService>());

			Controller = new SwarmController(
				Service,
				RpcMapper,
				mockAssemblyInformationProvider.Object,
				mockOptions.Object,
				loggerFactory.CreateLogger<SwarmController>());
		}

		public void Dispose() => Service.Dispose();

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

		bool TryStartUpdate(IServerUpdateExecutor updateExecutor, Version newVersion)
		{
			throw new NotImplementedException();
		}

		public Task<bool> ExecuteUpdate(string updatePath, CancellationToken cancellationToken, CancellationToken criticalCancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
