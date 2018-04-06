using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Tgstation.Server.Host.Watchdog.Tests
{
	/// <summary>
	/// Tests for <see cref="WatchdogFactory"/>
	/// </summary>
	[TestClass]
	public sealed class TestWatchdogFactory
	{
		[TestMethod]
		public void TestCreateWatchdogWindows()
		{
			var factory = new WatchdogFactory();
			var mockOSIdentifier = new Mock<IOSIdentifier>();
			mockOSIdentifier.SetupGet(x => x.IsWindows).Returns(true).Verifiable();
			WatchdogFactory.osIdentifier = mockOSIdentifier.Object;
			Assert.IsNotNull(factory.CreateWatchdog());
			mockOSIdentifier.VerifyAll();
		}

		[TestMethod]
		public void TestCreateWatchdogNonWindows()
		{
			var factory = new WatchdogFactory();
			var mockOSIdentifier = new Mock<IOSIdentifier>();
			mockOSIdentifier.SetupGet(x => x.IsWindows).Returns(false).Verifiable();
			WatchdogFactory.osIdentifier = mockOSIdentifier.Object;
			Assert.IsNotNull(factory.CreateWatchdog());
			mockOSIdentifier.VerifyAll();
		}
	}
}
