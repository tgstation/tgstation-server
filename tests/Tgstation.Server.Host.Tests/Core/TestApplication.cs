using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
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
			Assert.ThrowsException<ArgumentNullException>(() => new Application(null, null, null, null));
			var mockConfiguration = new Mock<IConfiguration>();
			Assert.ThrowsException<ArgumentNullException>(() => new Application(mockConfiguration.Object, null, null, null));

			var mockAssemblyInfo = new Mock<IAssemblyInformationProvider>();
			mockAssemblyInfo.SetupGet(x => x.Name).Returns(typeof(Application).Assembly.GetName());

			Assert.ThrowsException<ArgumentNullException>(() => new Application(mockConfiguration.Object, mockAssemblyInfo.Object, null, null));

			var mockHostingEnvironment = new Mock<IWebHostEnvironment>();
			Assert.ThrowsException<ArgumentNullException>(() => new Application(mockConfiguration.Object, mockAssemblyInfo.Object, mockHostingEnvironment.Object, null));

			var app = new Application(mockConfiguration.Object, mockAssemblyInfo.Object, mockHostingEnvironment.Object, Mock.Of<IIOManager>());

			Assert.ThrowsException<ArgumentNullException>(() => app.ConfigureServices(null));
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(null, null, null, null, null, null));

			var mockAppBuilder = new Mock<IApplicationBuilder>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, null, null, null, null, null));

			var mockServerControl = new Mock<IServerControl>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, null, null, null, null));

			var mockTokenFactory = new Mock<ITokenFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, null, null, null));

			var mockControlPanelOptions = new Mock<IOptions<ControlPanelConfiguration>>();
			mockControlPanelOptions.SetupGet(x => x.Value).Returns(new ControlPanelConfiguration()).Verifiable();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, mockControlPanelOptions.Object, null, null));

			var mockGeneralOptions = new Mock<IOptions<GeneralConfiguration>>();
			mockGeneralOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration()).Verifiable();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, mockControlPanelOptions.Object, mockGeneralOptions.Object, null));
			mockControlPanelOptions.VerifyAll();
			mockGeneralOptions.VerifyAll();
		}

		class MockSetupWizard : ISetupWizard
		{
			public Task<bool> CheckRunWizard(CancellationToken cancellationToken) => Task.FromResult(true);
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
