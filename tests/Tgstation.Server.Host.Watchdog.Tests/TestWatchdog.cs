using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Startup;

namespace Tgstation.Server.Host.Watchdog.Tests
{
	[TestClass]
	public sealed class TestWatchdog
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new Watchdog(null, null, null));
			var mockActiveAssemblyDeleter = new Mock<IActiveLibraryDeleter>();
			Assert.ThrowsException<ArgumentNullException>(() => new Watchdog(mockActiveAssemblyDeleter.Object, null, null));
			var mockIsolatedServerContextFactory = new Mock<IIsolatedAssemblyContextFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => new Watchdog(mockActiveAssemblyDeleter.Object, mockIsolatedServerContextFactory.Object, null));
			var mockLogger = new LoggerFactory().CreateLogger<Watchdog>();
			var wd = new Watchdog(mockActiveAssemblyDeleter.Object, mockIsolatedServerContextFactory.Object, mockLogger);
		}
	}
}
