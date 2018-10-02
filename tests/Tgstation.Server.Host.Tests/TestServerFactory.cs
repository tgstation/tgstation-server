using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tgstation.Server.Host.Tests
{
	/// <summary>
	/// Tests for <see cref="ServerFactory"/>
	/// </summary>
	[TestClass]
	public sealed class TestServerFactory
	{
		[TestMethod]
		public void TestWorksWithoutUpdatePath()
		{
			var factory = new ServerFactory();

			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateServer(null, null));
			factory.CreateServer(Array.Empty<string>(), null);
		}

		[TestMethod]
		public void TestWorksWithUpdatePath()
		{
			var factory = new ServerFactory();
			const string Path = "/test";

			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateServer(null, null));
			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateServer(null, Path));
			factory.CreateServer(Array.Empty<string>(), Path);
		}
	}
}
