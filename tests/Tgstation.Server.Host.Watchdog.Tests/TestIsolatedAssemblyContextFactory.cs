using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Host.Watchdog.Tests
{
	/// <summary>
	/// Tests for <see cref="IsolatedAssemblyContextFactory"/>
	/// </summary>
	[TestClass]
	public sealed class TestIsolatedAssemblyContextFactory
	{
		[TestMethod]
		public void TestServerFactoryCreation()
		{
			var contextFactory = new IsolatedAssemblyContextFactory();
			Assert.IsNotNull(contextFactory.CreateIsolatedServerFactory(typeof(ServerFactory).Assembly.Location));
		}
	}
}
