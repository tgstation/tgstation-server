using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using TGS.Tests;

namespace TGS.Server.Service.Tests
{
	/// <summary>
	/// Tests for <see cref="Program"/>
	/// </summary>
	[TestClass]
	public sealed class TestProgram
	{
		TextReader oldIn;

		[TestInitialize]
		public void Init()
		{
			oldIn = System.Console.In;
			System.Console.SetIn(new FakeTextReader());
		}

		[TestCleanup]
		public void Cleanup()
		{
			System.Console.SetIn(oldIn);
		}

		[TestMethod]
		public void TestMain()
		{
			var mockServerFactory = new Mock<IServerFactory>();
			var mockServiceRunner = new Mock<IServiceRunner>();
			var mockServer = new Mock<IServer>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsAny<ILogger>())).Returns(mockServer.Object);
			var pt = new PrivateType(typeof(Program));
			pt.SetStaticField("ServerFactory", mockServerFactory.Object);
			pt.SetStaticField("ServiceRunner", mockServiceRunner.Object);
			Program.Main();
		}
	}
}
