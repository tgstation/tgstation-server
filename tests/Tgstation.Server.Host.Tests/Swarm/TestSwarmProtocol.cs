using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Swarm.Tests
{
	[TestClass]
	public sealed class TestSwarmProtocol
	{
		static readonly HashSet<ushort> usedPorts = new ();
		static ILoggerFactory loggerFactory;
		static ILogger logger;

		static ISeekableFileStreamProvider updateFileStreamProvider;

		[TestInitialize]
		public async Task Initialize()
		{
			loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.SetMinimumLevel(LogLevel.Trace);
				builder.AddConsole();
			});

			logger = loggerFactory.CreateLogger<TestSwarmProtocol>();

			var ms = new MemoryStream(new byte[] { 1, 2, 3, 4 });

			updateFileStreamProvider = new BufferedFileStreamProvider(ms);
			await updateFileStreamProvider.GetResult(default);
		}

		[TestCleanup]
		public async Task Shutdown()
		{
			usedPorts.Clear();
			loggerFactory.Dispose();
			await updateFileStreamProvider.DisposeAsync();
		}

		[TestMethod]
		public async Task TestInitHappensInstantlyWhenControllerIsInitialized()
		{
			await using var controller = GenNode();
			await using var node = GenNode(controller);

			TestableSwarmNode.Link(controller, node);

			var controllerInit = controller.TryInit();
			Assert.IsTrue(controllerInit.IsCompleted);
			Assert.AreEqual(SwarmRegistrationResult.Success, await controllerInit);

			var nodeInit = node.TryInit();
			Assert.IsTrue(nodeInit.IsCompleted);
			Assert.AreEqual(SwarmRegistrationResult.Success, await nodeInit);
		}

		[TestMethod]
		public async Task TestNodeInitializeDoesNotWorkWithoutController()
		{
			await using var controller = GenNode();
			await using var node1 = GenNode(controller);
			await using var node2 = GenNode(controller);

			TestableSwarmNode.Link(controller, node1, node2);

			Assert.AreEqual(SwarmRegistrationResult.CommunicationFailure, await node1.TryInit());

			Assert.AreEqual(SwarmRegistrationResult.Success, await controller.TryInit());

			Assert.AreEqual(SwarmRegistrationResult.Success, await node2.TryInit());
		}

		[TestMethod]
		public async Task TestUpdateCantProceedWithoutAllNodes()
		{
			await using var controller = GenNode();
			await using var node1 = GenNode(controller);
			await using var node2 = GenNode(controller);

			TestableSwarmNode.Link(controller, node1, node2);

			Assert.AreEqual(SwarmRegistrationResult.Success, await controller.TryInit());
			Assert.AreEqual(SwarmRegistrationResult.Success, await node2.TryInit());

			logger.LogTrace("Marker 1");

			await DelayMax(() =>
			{
				Assert.AreEqual(2, controller.Service.GetSwarmServers().Count);
				Assert.AreEqual(2, node2.Service.GetSwarmServers().Count);
			}, 5);

			logger.LogTrace("Marker 2");

			Assert.AreEqual(SwarmPrepareResult.Failure, await controller.Service.PrepareUpdate(updateFileStreamProvider, new Version(4, 3, 2), default));


			logger.LogTrace("Marker 3");

			Assert.AreEqual(SwarmRegistrationResult.Success, await node1.TryInit());

			logger.LogTrace("Marker 4");

			await DelayMax(() =>
			{
				Assert.AreEqual(3, controller.Service.GetSwarmServers().Count);
				Assert.AreEqual(3, node2.Service.GetSwarmServers().Count);
				Assert.AreEqual(3, node1.Service.GetSwarmServers().Count);
			}, 5);

			Assert.AreEqual(SwarmPrepareResult.SuccessHoldProviderUntilCommit, await controller.Service.PrepareUpdate(updateFileStreamProvider, new Version(4, 3, 2), default));
		}

		[TestMethod]
		public async Task TestServersReconnectAfterReboot()
		{
			await using var controller = GenNode();
			await using var node1 = GenNode(controller);

			TestableSwarmNode.Link(controller, node1);
			Assert.AreEqual(SwarmRegistrationResult.Success, await controller.TryInit());

			Assert.AreEqual(SwarmRegistrationResult.Success, await node1.TryInit());

			Assert.AreEqual(2, controller.Service.GetSwarmServers().Count);
			Assert.AreEqual(1, node1.Service.GetSwarmServers().Count);

			await DelayMax(() => Assert.AreEqual(2, node1.Service.GetSwarmServers().Count));

			await controller.SimulateReboot(default);
			Assert.AreEqual(SwarmRegistrationResult.Success, await controller.TryInit());
			Assert.AreEqual(1, controller.Service.GetSwarmServers().Count);

			// Remember node's async delayer fires every millisecond
			// server list is updated in health check loop
			await DelayMax(() =>
			{
				Assert.AreEqual(2, controller.Service.GetSwarmServers().Count);
				Assert.AreEqual(2, node1.Service.GetSwarmServers().Count);
			}, 5);

			await node1.SimulateReboot(default);
			// health check timeout
			await DelayMax(() => Assert.AreEqual(1, controller.Service.GetSwarmServers().Count), 5);

			Assert.AreEqual(SwarmRegistrationResult.Success, await node1.TryInit());

			Assert.AreEqual(2, controller.Service.GetSwarmServers().Count, "thing 1");
			await DelayMax(() => Assert.AreEqual(2, node1.Service.GetSwarmServers().Count), 5);
		}

		[TestMethod]
		public async Task TestCommitFailsIfNotPrepared()
		{
			await using var controller = GenNode();
			await using var node1 = GenNode(controller);

			TestableSwarmNode.Link(controller, node1);

			Assert.AreEqual(SwarmRegistrationResult.Success, await controller.TryInit());
			Assert.AreEqual(SwarmRegistrationResult.Success, await node1.TryInit());

			Assert.AreEqual(SwarmCommitResult.AbortUpdate, await node1.Service.CommitUpdate(default));
		}

		[TestMethod]
		public async Task TestPrepareDifferentVersionsFails()
		{
			await using var controller = GenNode();
			await using var node1 = GenNode(controller);

			TestableSwarmNode.Link(controller, node1);

			Assert.AreEqual(SwarmRegistrationResult.Success, await controller.TryInit());
			Assert.AreEqual(SwarmRegistrationResult.Success, await node1.TryInit());

			await DelayMax(() =>
			{
				Assert.AreEqual(2, controller.Service.GetSwarmServers().Count);
				Assert.AreEqual(2, node1.Service.GetSwarmServers().Count);
			}, 5);

			Assert.AreEqual(SwarmPrepareResult.SuccessHoldProviderUntilCommit, await node1.Service.PrepareUpdate(updateFileStreamProvider, new Version(4, 3, 2), default));
			Assert.AreEqual(SwarmPrepareResult.Failure, await node1.Service.PrepareUpdate(updateFileStreamProvider, new Version(4, 3, 3), default));
		}

		[TestMethod]
		public async Task TestSimultaneousPrepareDifferentVersionsFailsControllerFirst()
		{
			await TestSimultaneousPrepareDifferentVersionsFails(true);
		}

		[TestMethod]
		public async Task TestSimultaneousPrepareDifferentVersionsFailsNodeFirst()
		{
			await TestSimultaneousPrepareDifferentVersionsFails(false);
		}

		static async ValueTask TestSimultaneousPrepareDifferentVersionsFails(bool prepControllerFirst)
		{
			await using var controller = GenNode();
			await using var node1 = GenNode(controller);

			TestableSwarmNode.Link(controller, node1);

			controller.UpdateResult = ServerUpdateResult.UpdateInProgress;
			node1.UpdateResult = ServerUpdateResult.UpdateInProgress;

			Assert.AreEqual(SwarmRegistrationResult.Success, await controller.TryInit());
			Assert.AreEqual(SwarmRegistrationResult.Success, await node1.TryInit());

			ValueTask<SwarmPrepareResult> controllerPrepareTask = ValueTask.FromResult(SwarmPrepareResult.SuccessProviderNotRequired), nodePrepareTask;
			if (prepControllerFirst)
				controllerPrepareTask = controller.Service.PrepareUpdate(updateFileStreamProvider, new Version(4, 3, 2), default);

			nodePrepareTask = node1.Service.PrepareUpdate(updateFileStreamProvider, new Version(4, 3, 3), default);

			if (!prepControllerFirst)
				controllerPrepareTask = controller.Service.PrepareUpdate(updateFileStreamProvider, new Version(4, 3, 2), default);

			await Task.Yield();

			await ValueTaskExtensions.WhenAll(controllerPrepareTask, nodePrepareTask);

			Task<SwarmCommitResult> nodeCommitTask = null, controllerCommitTask = null;

			var controllerPrepped = await controllerPrepareTask;
			var nodePrepped = await nodePrepareTask;
			if (controllerPrepped == SwarmPrepareResult.Failure && nodePrepped == SwarmPrepareResult.Failure)
				return; // all is good with the world

			// We have to be fair to the system here...
			// Requests could still be in flight, things could get aborted
			// Each server STILL has to download the update package which takes time
			// Give it 1 second to get its shit together
			await Task.Delay(TimeSpan.FromSeconds(1));

			if (controllerPrepped != SwarmPrepareResult.Failure)
			{
				Assert.AreEqual(SwarmPrepareResult.SuccessHoldProviderUntilCommit, controllerPrepped);
				controllerCommitTask = controller.Service.CommitUpdate(default).AsTask();
			}

			if (nodePrepped != SwarmPrepareResult.Failure)
			{
				Assert.AreEqual(SwarmPrepareResult.SuccessHoldProviderUntilCommit, nodePrepped);
				nodeCommitTask = node1.Service.CommitUpdate(default).AsTask();
			}

			await Task.Yield();

			await Task.WhenAll(controllerCommitTask ?? Task.CompletedTask, nodeCommitTask ?? Task.CompletedTask);

			if (controllerPrepped != SwarmPrepareResult.Failure)
				Assert.AreEqual(SwarmCommitResult.AbortUpdate, await controllerCommitTask);

			if (nodePrepped != SwarmPrepareResult.Failure)
				Assert.AreEqual(SwarmCommitResult.AbortUpdate, await nodeCommitTask);
		}

		static TestableSwarmNode GenNode(TestableSwarmNode controller = null, Version version = null)
		{
			ushort randPort;
			do
			{
				randPort = usedPorts.LastOrDefault() == 0 ? (ushort)16129U: (ushort)(usedPorts.Last() + 1);
			}
			while (randPort == 0 || !usedPorts.Add(randPort));

			var config = new SwarmConfiguration
			{
				Address = new Uri($"http://127.0.0.1:{randPort}"),
				ControllerAddress = controller?.Config.Address,
				Identifier = $"{(controller == null ? "Controller" : "Node")}{usedPorts.Count}",
				PrivateKey = "asdf",
				UpdateRequiredNodeCount = (uint)usedPorts.Count - 1,
			};

			return new TestableSwarmNode(loggerFactory, config, version);
		}

		static async Task DelayMax(Action assertion, long seconds = 1)
		{
			var id = Guid.NewGuid();
			logger.LogInformation("Begin DelayMax {id}: {seconds}", id, seconds);
			var stopwatch = Stopwatch.StartNew();
			do
			{
				try
				{
					assertion();
					logger.LogInformation("End DelayMax {id} after {milliseconds}ms", id, stopwatch.ElapsedMilliseconds);
					return;
				}
				catch (AssertFailedException)
				{
				}

				await Task.Delay(TimeSpan.FromMilliseconds(100));
			}
			while (stopwatch.ElapsedMilliseconds < (seconds * 1000));

			try
			{
				assertion();
			}
			catch
			{
				logger.LogError("Fail DelayMax {id} after {milliseconds}ms", id, stopwatch.ElapsedMilliseconds);
				throw;
			}

			logger.LogInformation("End DelayMax {id} after {milliseconds}ms", id, stopwatch.ElapsedMilliseconds);
		}
	}
}
