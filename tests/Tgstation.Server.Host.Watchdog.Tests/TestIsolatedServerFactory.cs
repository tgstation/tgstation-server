using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tgstation.Server.Host.Watchdog.Tests
{
	/// <summary>
	/// Tests for <see cref="IsolatedServerFactory"/>
	/// </summary>
	[TestClass]
	public sealed class TestIsolatedServerFactory
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new IsolatedServerFactory(null));
			var isf = new IsolatedServerFactory(typeof(ServerFactory).Assembly.Location);
		}

		[TestMethod]
		public void TestLoading()
		{
			var isf = new IsolatedServerFactory(typeof(ServerFactory).Assembly.Location);
			Assert.IsNotNull(isf.CreateServer(Array.Empty<string>(), String.Empty));
		}
	}
}
