using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Host.Controllers.Tests
{
	[TestClass]
	public sealed class TestRequestSwarmRegistrationParser
	{
		[TestMethod]
		public void TestConstructorThrows()
		{
			var parser = new RequestSwarmRegistrationParser();

			Assert.ThrowsException<ArgumentNullException>(() => parser.GetRequestRegistrationId(null));
		}
	}
}
