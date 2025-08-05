using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.IO.Tests
{
	[TestClass]
	public sealed class TestConsole
	{
		[TestMethod]
		public void TestContructionThrows()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new Console(null));
		}

		[TestMethod]
		public async Task TestWriteLine()
		{
			var console = new Console(new PlatformIdentifier());
			await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => console.WriteAsync(null, false, default));
			try
			{
				await console.WriteAsync(null, true, default);
				await console.WriteAsync(String.Empty, false, default).ConfigureAwait(true);
			}
			catch (InvalidOperationException)
			{
				Assert.IsFalse(console.Available);
			}
		}

		[TestMethod]
		public void TestUserInteractive()
		{
			var console = new Console(new PlatformIdentifier());
			Assert.AreEqual(Environment.UserInteractive, console.Available);
		}
	}
}
