using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http.Headers;

namespace Tgstation.Server.Client.Tests
{
	[TestClass]
	public sealed class TestServerClientFactory
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new RestServerClientFactory(null));
			new RestServerClientFactory(new ProductHeaderValue("Tgstation.Server.Client.Tests", GetType().Assembly.GetName().Version.ToString()));
		}
	}
}
