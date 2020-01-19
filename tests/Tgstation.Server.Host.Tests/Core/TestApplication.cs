using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Security;

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

			var mockHostingEnvironment = new Mock<IHostingEnvironment>();
			Assert.ThrowsException<ArgumentNullException>(() => new Application(mockConfiguration.Object, mockAssemblyInfo.Object, mockHostingEnvironment.Object, null));

			var app = new Application(mockConfiguration.Object, mockAssemblyInfo.Object, mockHostingEnvironment.Object, Mock.Of<IIOManager>());

			Assert.ThrowsException<ArgumentNullException>(() => app.ConfigureServices(null));
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(null, null, null, null, null));

			var mockAppBuilder = new Mock<IApplicationBuilder>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, null, null, null, null));

			var mockServerControl = new Mock<IServerControl>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, null, null, null));

			var mockTokenFactory = new Mock<ITokenFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, null, null));

			var mockControlPanelOptions = new Mock<IOptions<ControlPanelConfiguration>>();
			mockControlPanelOptions.SetupGet(x => x.Value).Returns(new ControlPanelConfiguration()).Verifiable();
			Assert.ThrowsException<ArgumentNullException>(() => app.Configure(mockAppBuilder.Object, mockServerControl.Object, mockTokenFactory.Object, mockControlPanelOptions.Object, null));
			mockControlPanelOptions.VerifyAll();
		}

		class MockSetupWizard : ISetupWizard
		{
			public Task<bool> CheckRunWizard(CancellationToken cancellationToken) => Task.FromResult(true);
		}

		class MockApplicationLifetime : IApplicationLifetime
		{
			public CancellationToken ApplicationStarted => default;

			public CancellationToken ApplicationStopping => default;

			public CancellationToken ApplicationStopped => default;

			public void StopApplication() { }
		}

		[TestMethod]
		public void TestConfigureServicesThrowsWhenSetupWizardConfigurationDemands()
		{
			var mockConfiguration = new Mock<IConfiguration>();
			var mockAssemblyInfo = new Mock<IAssemblyInformationProvider>();
			mockAssemblyInfo.SetupGet(x => x.Name).Returns(typeof(Application).Assembly.GetName());
			var mockHostingEnvironment = new Mock<IHostingEnvironment>();

			var app = new Application(mockConfiguration.Object, mockAssemblyInfo.Object, mockHostingEnvironment.Object, Mock.Of<IIOManager>());

			var mockOptions = new Mock<IOptions<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration
			{
				SetupWizardMode = SetupWizardMode.Only
			}).Verifiable();

			var fakeServiceDescriptor = new List<ServiceDescriptor>()
			{
				new ServiceDescriptor(typeof(IApplicationLifetime), typeof(MockApplicationLifetime), ServiceLifetime.Singleton),
				new ServiceDescriptor(typeof(ISetupWizard), typeof(MockSetupWizard), ServiceLifetime.Singleton),
				new ServiceDescriptor(typeof(IOptions<GeneralConfiguration>), mockOptions.Object)
			};

			var mockServiceCollection = new Mock<IServiceCollection>();

			var mockConfigSection = new Mock<IConfigurationSection>();

			mockConfiguration.Setup(x => x.GetSection(It.IsNotNull<string>())).Returns(mockConfigSection.Object).Verifiable();
			mockServiceCollection.Setup(x => x.GetEnumerator()).Returns(() => fakeServiceDescriptor.GetEnumerator()).Verifiable();

			Assert.ThrowsException<OperationCanceledException>(() => app.ConfigureServices(mockServiceCollection.Object));

			mockOptions.VerifyAll();
			mockConfiguration.VerifyAll();
			mockServiceCollection.VerifyAll();
		}
	}
}
