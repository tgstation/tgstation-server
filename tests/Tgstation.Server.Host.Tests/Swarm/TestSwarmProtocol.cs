using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
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
			using var controller = new TestableSwarmNode(loggerFactory, GenConfig());
			using var node = new TestableSwarmNode(loggerFactory, GenConfig(controller.Config));

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
			using var controller = new TestableSwarmNode(loggerFactory, GenConfig());
			using var node1 = new TestableSwarmNode(loggerFactory, GenConfig(controller.Config));
			using var node2 = new TestableSwarmNode(loggerFactory, GenConfig(controller.Config));

			TestableSwarmNode.Link(controller, node1, node2);

			Assert.AreEqual(SwarmRegistrationResult.CommunicationFailure, await node1.TryInit());

			Assert.AreEqual(SwarmRegistrationResult.Success, await controller.TryInit());

			Assert.AreEqual(SwarmRegistrationResult.Success, await node2.TryInit());
		}

		SwarmConfiguration GenConfig(SwarmConfiguration controllerConfig = null)
		{
			ushort randPort;
			do
			{
				randPort = (ushort)(Random.Shared.Next() % UInt16.MaxValue);
			}
			while (randPort == 0 || !usedPorts.Add(randPort));

			return new SwarmConfiguration
			{
				Address = new Uri($"http://127.0.0.1:{randPort}"),
				ControllerAddress = controllerConfig?.Address,
				Identifier = $"{(controllerConfig == null ? "Controller" : "Node")}{usedPorts.Count}",
				PrivateKey = "asdf",
				UpdateRequiredNodeCount = (uint)usedPorts.Count - 1,
			};
		}
	}
}
