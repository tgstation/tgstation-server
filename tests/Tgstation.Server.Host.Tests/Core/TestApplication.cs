using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Tgstation.Server.Host.Core.Tests
{
	[TestClass]
	public sealed class TestApplication
	{
		[TestMethod]
		public void TestMethodThrows()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new Application(null, null));
			var mockConfiguration = new Mock<IConfiguration>();
			Assert.ThrowsException<ArgumentNullException>(() => new Application(mockConfiguration.Object, null));

			var mockHostingEnvironment = new Mock<IHostingEnvironment>();

			var app = new Application(mockConfiguration.Object, mockHostingEnvironment.Object);

			Assert.ThrowsException<ArgumentNullException>(() => app.ConfigureServices(null));
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(null, null, null));

			var mockAppBuilder = new Mock<IApplicationBuilder>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, null, null));

			var mockLogger = new Mock<ILogger<Application>>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockLogger.Object, null));
		}
	}
}
