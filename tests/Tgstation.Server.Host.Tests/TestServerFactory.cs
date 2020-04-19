using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
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
			Assert.ThrowsException<ArgumentNullException>(() => new ServerFactory<Application>(null, null));
			IAssemblyInformationProvider assemblyInformationProvider = Mock.Of<IAssemblyInformationProvider>();
			Assert.ThrowsException<ArgumentNullException>(() => new ServerFactory<Application>(assemblyInformationProvider, null));
			IIOManager ioManager = Mock.Of<IIOManager>();
			new ServerFactory<Application>(assemblyInformationProvider, ioManager);
		}

		[TestMethod]
		public void TestWorksWithoutUpdatePath()
		{
			var factory = Application.CreateDefaultServerFactory();

			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateServer(null, null));
			factory.CreateServer(Array.Empty<string>(), null);
		}

		[TestMethod]
		public void TestWorksWithUpdatePath()
		{
			var factory = Application.CreateDefaultServerFactory();
			const string Path = "/test";

			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateServer(null, null));
			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateServer(null, Path));
			factory.CreateServer(Array.Empty<string>(), Path);
		}
	}
}
