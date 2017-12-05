using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TGS.Server.IO;
using TGS.Server.Logging;

namespace TGS.Server.Tests
{
	/// <summary>
	/// Tests for <see cref="ServerFactory"/>
	/// </summary>
	[TestClass]
	public sealed class TestServerFactory
	{
		[TestMethod]
		public void TestConstruction()
		{
			var mockIO = new Mock<IIOManager>();
			var si = new ServerFactory(mockIO.Object);
		}

		[TestMethod]
		public void TestCreation()
		{
			var mockIO = new Mock<IIOManager>();
			var mockLogger = new Mock<ILogger>();
			var si = new ServerFactory(mockIO.Object);
			var s = si.CreateServer(mockLogger.Object);
		}
	}
}
