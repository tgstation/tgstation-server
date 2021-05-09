using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Tests
{
	/// <summary>
	/// Tests for <see cref="ServerFactory"/>
	/// </summary>
	[TestClass]
	public sealed class TestServerFactory
	{
		[TestMethod]
		public void TestContructor()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new ServerFactory(null, null));
			IAssemblyInformationProvider assemblyInformationProvider = Mock.Of<IAssemblyInformationProvider>();
			Assert.ThrowsException<ArgumentNullException>(() => new ServerFactory(assemblyInformationProvider, null));
			IIOManager ioManager = Mock.Of<IIOManager>();
			new ServerFactory(assemblyInformationProvider, ioManager);
		}

		[TestMethod]
		public async Task TestWorksWithoutUpdatePath()
		{
			var factory = Application.CreateDefaultServerFactory();

			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => factory.CreateServer(null, null, default));
			var result = await factory.CreateServer(new[] { "General:SetupWizardMode=Never" }, null, default);
			Assert.IsNotNull(result);
		}

		[TestMethod]
		public async Task TestWorksWithUpdatePath()
		{
			var factory = Application.CreateDefaultServerFactory();
			const string Path = "/test";

			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => factory.CreateServer(null, null, default));
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => factory.CreateServer(null, Path, default));
			var result = await factory.CreateServer(new[] { "General:SetupWizardMode=Never" }, Path, default);
			Assert.IsNotNull(result);
		}
	}
}
