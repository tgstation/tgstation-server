using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Client.Tests
{
	/// <summary>
	/// Tests for <see cref="ServerClientFactory"/>
	/// </summary>
	[TestClass]
	public sealed class TestServerClientFactory
	{
		[TestMethod]
		public async Task TestUserConstruction()
		{
			var factory = new ServerClientFactory();
			await Assert.ThrowsExceptionAsync<NotImplementedException>(() => factory.CreateServerClient(null, null, null)).ConfigureAwait(false);
		}

		[TestMethod]
		public async Task TestTokenConstruction()
		{
			var factory = new ServerClientFactory();
			await Assert.ThrowsExceptionAsync<NotImplementedException>(() => factory.CreateServerClient(null, null)).ConfigureAwait(false);
		}
	}
}
