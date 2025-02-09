using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;

using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Core.Tests
{
	/// <summary>
	/// Tests for <see cref="Application"/>
	/// </summary>
	[TestClass]
	public sealed class TestApplication
	{
		[TestMethod]
		public void TestMethodThrows()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new Application(null, null));
			var mockConfiguration = new Mock<IConfiguration>();
			Assert.ThrowsException<ArgumentNullException>(() => new Application(mockConfiguration.Object, null));

			var mockHostingEnvironment = new Mock<IWebHostEnvironment>();
			var app = new Application(mockConfiguration.Object, mockHostingEnvironment.Object);

			Assert.ThrowsException<ArgumentNullException>(() => app.ConfigureServices(null, null, null));

			var mockServiceCollection = Mock.Of<IServiceCollection>();
			Assert.ThrowsException<ArgumentNullException>(() => app.ConfigureServices(mockServiceCollection, null, null));

			Assert.ThrowsException<ArgumentNullException>(() => app.ConfigureServices(mockServiceCollection, null, null));

			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(null, null, null, null, null, null, null, null, null, null, null));

			var mockAppBuilder = new Mock<IApplicationBuilder>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, null, null, null, null, null, null, null, null, null, null));

			var mockServerControl = new Mock<IServerControl>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, null, null, null, null, null, null, null, null, null));

			var mockTokenFactory = new Mock<ITokenFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, null, null, null, null, null, null, null, null));

			var mockServerPortProvider = new Mock<IServerPortProvider>();
			mockServerPortProvider.SetupGet(x => x.HttpApiPort).Returns(5345);
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, mockServerPortProvider.Object, null, null, null, null, null, null, null));

			var mockAssemblyInformationProvider = Mock.Of<IAssemblyInformationProvider>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, mockServerPortProvider.Object, mockAssemblyInformationProvider, null, null, null, null, null, null));

			var mockControlPanelOptions = new Mock<IOptions<ControlPanelConfiguration>>();
			mockControlPanelOptions.SetupGet(x => x.Value).Returns(new ControlPanelConfiguration()).Verifiable();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, mockServerPortProvider.Object, mockAssemblyInformationProvider, mockControlPanelOptions.Object, null, null, null, null, null));

			var mockGeneralOptions = new Mock<IOptions<GeneralConfiguration>>();
			mockGeneralOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration()).Verifiable();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, mockServerPortProvider.Object, mockAssemblyInformationProvider, mockControlPanelOptions.Object, mockGeneralOptions.Object, null, null, null, null));

			var mockDatabaseOptions = new Mock<IOptions<DatabaseConfiguration>>();
			mockDatabaseOptions.SetupGet(x => x.Value).Returns(new DatabaseConfiguration()).Verifiable();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, mockServerPortProvider.Object, mockAssemblyInformationProvider, mockControlPanelOptions.Object, mockGeneralOptions.Object, mockDatabaseOptions.Object, null, null, null));

			var mockSwarmOptions = new Mock<IOptions<SwarmConfiguration>>();
			mockSwarmOptions.SetupGet(x => x.Value).Returns(new SwarmConfiguration()).Verifiable();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, mockServerPortProvider.Object, mockAssemblyInformationProvider, mockControlPanelOptions.Object, mockGeneralOptions.Object, mockDatabaseOptions.Object, mockSwarmOptions.Object, null, null));

			var mockInternalOptions = new Mock<IOptions<InternalConfiguration>>();
			mockInternalOptions.SetupGet(x => x.Value).Returns(new InternalConfiguration()).Verifiable();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, mockServerPortProvider.Object, mockAssemblyInformationProvider, mockControlPanelOptions.Object, mockGeneralOptions.Object, mockDatabaseOptions.Object, mockSwarmOptions.Object, mockInternalOptions.Object, null));

			mockControlPanelOptions.VerifyAll();
			mockInternalOptions.VerifyAll();
			mockSwarmOptions.VerifyAll();
			mockGeneralOptions.VerifyAll();
		}

		class MockHostApplicationLifetime : IHostApplicationLifetime
		{
			public CancellationToken ApplicationStarted => default;

			public CancellationToken ApplicationStopping => default;

			public CancellationToken ApplicationStopped => default;

			public void StopApplication() { }
		}
	}
}
