using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO.Abstractions.TestingHelpers;
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
		static readonly string[] cliArgs = ["General:SetupWizardMode=Never"];

		[TestMethod]
		public void TestConstructor()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new ServerFactory(null, null, null));
			IAssemblyInformationProvider assemblyInformationProvider = Mock.Of<IAssemblyInformationProvider>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new ServerFactory(assemblyInformationProvider, null, null));
			IIOManager ioManager = Mock.Of<IIOManager>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new ServerFactory(assemblyInformationProvider, ioManager, null));
			var mockFileSystem = new MockFileSystem();
			_ = new ServerFactory(assemblyInformationProvider, ioManager, mockFileSystem);
		}

		[TestMethod]
		public async Task TestWorksWithoutUpdatePath()
		{
			var factory = Application.CreateDefaultServerFactory();

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => factory.CreateServer(null, null, default).AsTask());
			var result = await factory.CreateServer(cliArgs, null, default);
			Assert.IsNotNull(result);
		}

		[TestMethod]
		public async Task TestWorksWithUpdatePath()
		{
			var factory = Application.CreateDefaultServerFactory();
			const string Path = "/test";

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => factory.CreateServer(null, null, default).AsTask());
			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => factory.CreateServer(null, Path, default).AsTask());
			var result = await factory.CreateServer(cliArgs, Path, default);
			Assert.IsNotNull(result);
		}
	}
}
