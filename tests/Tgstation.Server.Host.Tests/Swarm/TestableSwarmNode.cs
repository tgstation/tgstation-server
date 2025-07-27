using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;
using Moq.Language.Flow;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Swarm.Tests
{
	sealed class TestableSwarmNode : IAsyncDisposable
	{
		public SwarmService Service { get; private set; }

		public SwarmConfiguration Config { get; }

		public SwarmRpcMapper RpcMapper { get; }

		public FileTransferService TransferService { get; }

		public bool UpdateCommits { get; set; }

		public CancellationTokenSource CriticalCancellationTokenSource { get; private set; }

		public ServerUpdateResult UpdateResult { get; set; }
		public Task<SwarmCommitResult?> UpdateTask { get; private set; }

		public bool WebServerOpen { get; private set; }

		public bool Shutdown { get; private set; }

		readonly Mock<IDatabaseContextFactory> mockDBContextFactory;
		readonly Mock<IDatabaseSeeder> mockDatabaseSeeder;
		readonly ISetup<IDatabaseSeeder, ValueTask> mockDatabaseSeederInitialize;

		readonly Action recreateControllerAndService;

		readonly ILogger logger;

		public static void Link(params TestableSwarmNode[] nodes)
		{
			var configControllerSet = nodes.Select(x => (x.Config, x.TransferService, x)).ToList();

			_ = configControllerSet.Single(x => x.Config.ControllerAddress == null);
			Assert.IsTrue(
				configControllerSet.All(
					tuple1 => !String.IsNullOrWhiteSpace(tuple1.Config.PrivateKey)
					&& configControllerSet.All(tuple2 => tuple1.Config.PrivateKey == tuple2.Config.PrivateKey)),
				"This test doesn't support authentication issues.");

			foreach (var node in nodes)
			{
				node.Config.UpdateRequiredNodeCount = (uint)nodes.Length - 1;
				node.RpcMapper.Register(configControllerSet);
			}
		}

		private class MockTokenFactory : ITokenFactory
		{
			public ReadOnlySpan<byte> SigningKeyBytes
			{
				get => [0, 1, 2, 3, 4];
				set
				{
				}
			}

			public TokenValidationParameters ValidationParameters => throw new NotSupportedException();

			public string CreateToken(User user, bool serviceLogin)
			{
				throw new NotSupportedException();
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
			_ = mockDBContextFactory
				.Setup(x => x.UseContext(It.IsNotNull<Func<IDatabaseContext, ValueTask>>()))
				.Callback<Func<IDatabaseContext, ValueTask>>((func) => func(mockDatabaseContext));
			mockDBContextFactory
				.Setup(x => x.UseContextTaskReturn(It.IsNotNull<Func<IDatabaseContext, Task>>()))
				.Callback<Func<IDatabaseContext, Task>>((func) => func(mockDatabaseContext));

			var mockAsyncDelayer = new Mock<IAsyncDelayer>();
			mockAsyncDelayer.Setup(
				x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
				.Returns<TimeSpan, CancellationToken>(
					async (delay, ct) => await Task.Delay(TimeSpan.FromMilliseconds(100), ct));

			var mockServerUpdater = new Mock<IServerUpdater>();

			static ILoggerFactory CreateLoggerFactoryForLogger(ILogger logger, out Mock<ILoggerFactory> mockLoggerFactory)
			{
				mockLoggerFactory = new Mock<ILoggerFactory>();
				mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() =>
				{
					var temp = logger;
					logger = null;

					Assert.IsNotNull(temp);
					return temp;
				})
				.Verifiable();
				return mockLoggerFactory.Object;
			}

			TransferService = new FileTransferService(
				new CryptographySuite(
					Mock.Of<IPasswordHasher<Models.User>>()),
				Mock.Of<IIOManager>(),
				new AsyncDelayer(Mock.Of<ILogger<AsyncDelayer>>()), // use a real one here because otherwise tickets expire too fast
				CreateLoggerFactoryForLogger(loggerFactory.CreateLogger($"FileTransferService-{swarmConfiguration.Identifier}"), out var mockLoggerFactory).CreateLogger<FileTransferService>());

			RpcMapper = new SwarmRpcMapper(
				(targetService, targetTransfer) => new SwarmController(
					targetService,
					targetTransfer,
					mockOptions.Object,
					loggerFactory.CreateLogger<SwarmController>()),
				loggerFactory.CreateLogger($"SwarmRpcMapper-{swarmConfiguration.Identifier}"),
				out var mockMessageHandler);

			mockServerUpdater
				.Setup(x => x.BeginUpdate(It.IsNotNull<ISwarmService>(), It.IsAny<IFileStreamProvider>(), It.IsNotNull<Version>(), It.IsAny<CancellationToken>()))
				.Returns(BeginUpdate);

			UpdateResult = ServerUpdateResult.Started;
			UpdateCommits = true;

			logger = loggerFactory.CreateLogger($"TestableSwarmNode-{swarmConfiguration.Identifier}");

			var mockTokenFactory = new MockTokenFactory();

			var mockHttpClientFactory = new Mock<IHttpClientFactory>();
			mockHttpClientFactory.Setup(x => x.CreateClient(String.Empty)).Returns(() => new HttpClient(mockMessageHandler));

			var runCount = 0;
			void RecreateControllerAndService()
			{
				logger.LogTrace("RecreateControllerAndService...");
				var run = ++runCount;
				WebServerOpen = false;
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
					mockAsyncDelayer.Object,
					mockServerUpdater.Object,
					TransferService,
					mockTokenFactory,
					mockOptions.Object,
					serviceLogger);
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
			logger.LogTrace("TryInit...");
			if (WebServerOpen)
				Assert.Fail("Initialized twice!");

			if (!cancel)
				mockDatabaseSeederInitialize.Returns(ValueTask.CompletedTask).Verifiable();
			else
				mockDatabaseSeederInitialize.Returns(async () =>
				{
					await Task.Yield();
					throw new TaskCanceledException();
				}).Verifiable();

			Task<SwarmRegistrationResult> Invoke() => Service.Initialize(default).AsTask();

			SwarmRegistrationResult? result;
			if (cancel)
			{
				await Assert.ThrowsExceptionAsync<OperationCanceledException>(Invoke);
				result = null;
			}
			else
			{
				try
				{
					WebServerOpen = true;
					result = await Invoke();
				}
				catch
				{
					WebServerOpen = false;
					throw;
				}
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

		ValueTask<ServerUpdateResult> BeginUpdate(ISwarmService swarmService, IFileStreamProvider fileStreamProvider, Version version, CancellationToken cancellationToken)
		{
			logger.LogTrace("BeginUpdate...");
			if (UpdateTask?.IsCompleted == false)
				return ValueTask.FromResult(ServerUpdateResult.UpdateInProgress);

			if (UpdateResult == ServerUpdateResult.Started)
			{
				UpdateTask = ExecuteUpdate(fileStreamProvider, version, cancellationToken, CriticalCancellationTokenSource.Token);
			}

			return ValueTask.FromResult(UpdateResult);
		}

		async Task<SwarmCommitResult?> ExecuteUpdate(IFileStreamProvider fileStreamProvider, Version version, CancellationToken cancellationToken, CancellationToken criticalCancellationToken)
		{
			logger.LogTrace("ExecuteUpdate...");
			await Task.Yield(); // Important to simulate some actual kind of asynchronicity here

			var stream = await fileStreamProvider.GetResult(cancellationToken);
			await using var buffer = new BufferedFileStreamProvider(stream);
			var result = await Service.PrepareUpdate(buffer, version, cancellationToken);

			if (result == SwarmPrepareResult.SuccessProviderNotRequired)
				await buffer.DisposeAsync();

			if (UpdateCommits && result != SwarmPrepareResult.Failure)
			{
				return await Service.CommitUpdate(criticalCancellationToken);
			}

			return null;
		}
	}
}
