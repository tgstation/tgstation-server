using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestConsole
	{
		[TestMethod]
		public async Task TestWriteLine()
		{
			var console = new Console();
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => console.WriteAsync(null, false, default)).ConfigureAwait(false);
			await console.WriteAsync(null, true, default).ConfigureAwait(false);
			await console.WriteAsync(String.Empty, false, default).ConfigureAwait(true);
		}

		[TestMethod]
		public void TestUserInteractive()
		{
			var console = new Console();
			Assert.AreEqual(Environment.UserInteractive, console.Available);
		}
	}
}
