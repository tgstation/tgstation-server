using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Grpc.Core;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;
using Moq.Language.Flow;

using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Swarm.Grpc;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Swarm.Tests
{
	sealed class TestableSwarmNode : IAsyncDisposable
	{
		const string HttpClientTokenExceptionMessage = "hds7fh7HDUYIHFSG7dsgy7hsufidhdsf";

		public SwarmService Service { get; private set; }

		public SwarmConfiguration Config { get; }

		public SwarmControllerService ControllerService { get; private set; }

		public SwarmNodeService NodeService { get; private set; }

		public SwarmSharedService SharedService { get; private set; }

		public FileTransferService TransferService { get; }

		public bool UpdateCommits { get; set; }

		public CancellationTokenSource CriticalCancellationTokenSource { get; private set; }

		public ServerUpdateResult UpdateResult { get; set; }
		public Task<SwarmCommitResult?> UpdateTask { get; private set; }

		public bool WebServerOpen { get; private set; }

		public bool Shutdown { get; private set; }

		readonly Mock<IHttpClient> mockHttpClient;
		readonly Mock<IDatabaseContextFactory> mockDBContextFactory;
		readonly Mock<IDatabaseSeeder> mockDatabaseSeeder;
		readonly ISetup<IDatabaseSeeder, ValueTask> mockDatabaseSeederInitialize;
		readonly Mock<ICallInvokerFactory> mockCallInvokerFactory;

		readonly Dictionary<(Uri, string), CallInvoker> callInvokerMappings;

		readonly Action recreateControllerAndService;

		readonly ILogger logger;

		public static void Link(params TestableSwarmNode[] nodes)
		{
			var configControllerSet = nodes.Select(x => (x.Config, x.TransferService, TestableNode: x)).ToList();

			var controller = configControllerSet.Single(x => x.Config.ControllerAddress == null);

			foreach (var node in nodes)
			{
				node.Config.UpdateRequiredNodeCount = (uint)nodes.Length - 1;
				if (node != controller.TestableNode)
					node.RegisterWithController(controller.TestableNode);
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

			var mockOptions = new Mock<IOptionsMonitor<SwarmConfiguration>>();
			mockOptions.SetupGet(x => x.CurrentValue).Returns(swarmConfiguration);

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

			var mockHttpClientFactory = new Mock<IAbstractHttpClientFactory>();
			mockHttpClient = new Mock<IHttpClient>();
			mockHttpClientFactory.Setup(x => x.CreateClient()).Returns(mockHttpClient.Object);

			mockHttpClient.Setup(x => x.SendAsync(It.IsNotNull<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception(HttpClientTokenExceptionMessage));

			mockCallInvokerFactory = new Mock<ICallInvokerFactory>();
			mockCallInvokerFactory.Setup(x => x.CreateCallInvoker(It.IsAny<Uri>(), It.IsAny<Func<string>>())).Returns<Uri, Func<string>>(
				(uri, authHeader) =>
				{
					if (!callInvokerMappings.TryGetValue((uri, authHeader()), out var invoker))
						invoker = new UnavailableCallInvoker();

					return invoker;
				});

			callInvokerMappings = new Dictionary<(Uri, string), CallInvoker>();

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
					Mock.Of<IPasswordHasher<User>>()),
				Mock.Of<IIOManager>(),
				new AsyncDelayer(Mock.Of<ILogger<AsyncDelayer>>()), // use a real one here because otherwise tickets expire too fast
				CreateLoggerFactoryForLogger(loggerFactory.CreateLogger($"FileTransferService-{swarmConfiguration.Identifier}"), out var mockLoggerFactory).CreateLogger<FileTransferService>());

			mockServerUpdater
				.Setup(x => x.BeginUpdate(It.IsNotNull<ISwarmService>(), It.IsAny<IFileStreamProvider>(), It.IsNotNull<Version>(), It.IsAny<CancellationToken>()))
				.Returns(BeginUpdate);

			UpdateResult = ServerUpdateResult.Started;
			UpdateCommits = true;

			logger = loggerFactory.CreateLogger($"TestableSwarmNode-{swarmConfiguration.Identifier}");

			var mockTokenFactory = new MockTokenFactory();

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
					mockCallInvokerFactory.Object,
					mockOptions.Object,
					serviceLogger);

				SharedService = new SwarmSharedService(
					Service);

				if (Config.ControllerAddress == null)
					ControllerService = new SwarmControllerService(
						Service,
						loggerFactory.CreateLogger<SwarmControllerService>());
				else
					NodeService = new SwarmNodeService(
						Service);
			}

			RecreateControllerAndService();
			recreateControllerAndService = RecreateControllerAndService;
		}

		private void RegisterWithController(TestableSwarmNode controllerNode)
		{
			var controllerCallInvoker = new SwarmMockCallInvoker(
				() => controllerNode.ControllerService,
				() => controllerNode.SharedService,
				() => !controllerNode.WebServerOpen,
				logger);

			callInvokerMappings.Add((controllerNode.Config.Address, $"{SwarmConstants.AuthenticationSchemeAndPolicy} {controllerNode.Config.PrivateKey}"), controllerCallInvoker);

			var nodeCallInvoker = new SwarmMockCallInvoker(
				() => NodeService,
				() => SharedService,
				() => !WebServerOpen,
				controllerNode.logger);

			controllerNode.callInvokerMappings.Add((Config.Address, $"{SwarmConstants.AuthenticationSchemeAndPolicy} {Config.PrivateKey}"), nodeCallInvoker);
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

			// prevent inline re-register
			WebServerOpen = false;
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

			Stream stream;
			try
			{
				stream = await fileStreamProvider.GetResult(cancellationToken);
			}
			catch (Exception ex) when (ex.Message == HttpClientTokenExceptionMessage)
			{
				// content of the update stream really doesn't matter
				stream = new MemoryStream([1, 2, 3, 4]);
			}

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
