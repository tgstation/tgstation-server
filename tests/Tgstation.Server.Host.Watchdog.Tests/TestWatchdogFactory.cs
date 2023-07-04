using Microsoft.Extensions.Logging;
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
		public void TestCreateWatchdog()
		{
			var factory = new WatchdogFactory();
			Assert.IsNotNull(
				factory.CreateWatchdog(
					Mock.Of<ISignalChecker>(),
					Mock.Of<ILoggerFactory>()));
		}
	}
}
