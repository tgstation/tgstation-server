using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

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
		public void TestWorksWithoutUpdatePath()
		{
			var factory = ServerFactory.CreateDefault();

			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateServer(null, null));
			factory.CreateServer(Array.Empty<string>(), null);
		}

		[TestMethod]
		public void TestWorksWithUpdatePath()
		{
			var factory = ServerFactory.CreateDefault();
			const string Path = "/test";

			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateServer(null, null));
			Assert.ThrowsException<ArgumentNullException>(() => factory.CreateServer(null, Path));
			factory.CreateServer(Array.Empty<string>(), Path);
		}
	}
}
