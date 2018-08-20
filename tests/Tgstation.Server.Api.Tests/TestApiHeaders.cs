using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http.Headers;

namespace Tgstation.Server.Api.Tests
{
	/// <summary>
	/// Tests for <see cref="ApiHeaders"/>
	/// </summary>
	[TestClass]
	public sealed class TestApiHeaders
	{
		readonly ProductHeaderValue productHeaderValue = new ProductHeaderValue("Tgstation.Server.Api.Tests", "1.0.0");

		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new ApiHeaders(null, null));
			Assert.ThrowsException<ArgumentNullException>(() => new ApiHeaders(productHeaderValue, null));
			var headers = new ApiHeaders(productHeaderValue, String.Empty);
		}
	}
}
