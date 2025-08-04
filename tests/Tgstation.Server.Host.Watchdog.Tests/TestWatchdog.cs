using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Watchdog.Tests
{
	[TestClass]
	public sealed class TestWatchdog
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new Watchdog(null, null));
			var mockSignalChecker = Mock.Of<ISignalChecker>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new Watchdog(mockSignalChecker, null));
			var mockLogger = Mock.Of<ILogger<Watchdog>>();
			var wd = new Watchdog(mockSignalChecker, mockLogger);
		}
	}
}
