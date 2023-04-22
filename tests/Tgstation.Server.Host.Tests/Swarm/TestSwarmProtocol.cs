using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host.Tests.Swarm
{
	[TestClass]
	public sealed class TestSwarmProtocol : IDisposable
	{
		readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.SetMinimumLevel(LogLevel.Trace);
			builder.AddConsole();
		});
		readonly HashSet<ushort> usedPorts = new ();

		public void Dispose() => loggerFactory.Dispose();

		[TestInitialize]
		public void Initialize()
		{
			usedPorts.Clear();
		}

		[TestMethod]
		public async Task TestInitHappensInstantlyWhenControllerIsInitialized()
		{
			await using var controller = GenNode();
			await using var node = GenNode(controller);

			TestableSwarmNode.Link(controller, node);

			controller.RpcMapper.AsyncRequests = false;
			node.RpcMapper.AsyncRequests = false;

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

			Assert.IsFalse(await controller.Service.PrepareUpdate(new Version(4, 3, 2), default));

			Assert.AreEqual(SwarmRegistrationResult.Success, await node1.TryInit());

			Assert.IsTrue(await controller.Service.PrepareUpdate(new Version(4, 3, 2), default));
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
			});

			await node1.SimulateReboot(default);
			// health check timeout
			await DelayMax(() => Assert.AreEqual(1, controller.Service.GetSwarmServers().Count));

			Assert.AreEqual(SwarmRegistrationResult.Success, await node1.TryInit());

			Assert.AreEqual(2, controller.Service.GetSwarmServers().Count);
			await DelayMax(() => Assert.AreEqual(2, node1.Service.GetSwarmServers().Count));
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

			Assert.IsTrue(await node1.Service.PrepareUpdate(new Version(4, 3, 2), default));
			Assert.IsFalse(await node1.Service.PrepareUpdate(new Version(4, 3, 3), default));
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

		async Task TestSimultaneousPrepareDifferentVersionsFails(bool prepControllerFirst)
		{
			await using var controller = GenNode();
			await using var node1 = GenNode(controller);

			TestableSwarmNode.Link(controller, node1);

			Assert.AreEqual(SwarmRegistrationResult.Success, await controller.TryInit());
			Assert.AreEqual(SwarmRegistrationResult.Success, await node1.TryInit());

			Task<bool> controllerPrepareTask = null, nodePrepareTask;
			if (prepControllerFirst)
			{
				controllerPrepareTask = controller.Service.PrepareUpdate(new Version(4, 3, 2), default);
				Assert.IsFalse(controllerPrepareTask.IsCompleted);
			}

			nodePrepareTask = node1.Service.PrepareUpdate(new Version(4, 3, 3), default);
			Assert.IsFalse(nodePrepareTask.IsCompleted);

			if (!prepControllerFirst)
			{
				controllerPrepareTask = controller.Service.PrepareUpdate(new Version(4, 3, 2), default);
				Assert.IsFalse(controllerPrepareTask.IsCompleted);
			}

			await Task.Yield();

			var controllerResult = await controllerPrepareTask;
			var nodeResult = await nodePrepareTask;

				Assert.IsFalse(controllerResult && nodeResult);
		}

		static async Task DelayMax(Action assertion, ulong seconds = 1)
		{
			for (var i = 0U; i < (seconds * 10); ++i)
			{
				try
				{
					assertion();
					return;
				}
				catch (AssertFailedException) { }
				await Task.Delay(TimeSpan.FromMilliseconds(100));
			}

			assertion();
		}

		TestableSwarmNode GenNode(TestableSwarmNode controller = null, Version version = null)
		{
			ushort randPort;
			do
			{
				randPort = (ushort)(Random.Shared.Next() % UInt16.MaxValue);
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
	}
}
