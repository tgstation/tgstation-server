using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Setup.Tests
{
	[TestClass]
	public sealed class TestSetupWizard
	{
		[TestMethod]
		public void TestConstructionThrows()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(null, null, null, null, null, null, null, null, null, null));
			var mockIOManager = new Mock<IIOManager>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, null, null, null, null, null, null, null, null, null));
			var mockConsole = new Mock<IConsole>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, null, null, null, null, null, null, null, null));
			var mockHostingEnvironment = new Mock<IWebHostEnvironment>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, null, null, null, null, null, null, null));
			var mockAssemblyInfoProvider = new Mock<IAssemblyInformationProvider>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockAssemblyInfoProvider.Object, null, null, null, null, null, null));
			var mockDBConnectionFactory = new Mock<IDatabaseConnectionFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockAssemblyInfoProvider.Object, mockDBConnectionFactory.Object, null, null, null, null, null));
			var mockPlatformIdentifier = new Mock<IPlatformIdentifier>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockAssemblyInfoProvider.Object, mockDBConnectionFactory.Object, mockPlatformIdentifier.Object, null, null, null, null));
			var mockAsyncDelayer = new Mock<IAsyncDelayer>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockAssemblyInfoProvider.Object, mockDBConnectionFactory.Object, mockPlatformIdentifier.Object, mockAsyncDelayer.Object, null, null, null));
			var mockLifetime = new Mock<IHostApplicationLifetime>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockAssemblyInfoProvider.Object, mockDBConnectionFactory.Object, mockPlatformIdentifier.Object, mockAsyncDelayer.Object, mockLifetime.Object, null, null));
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(x => x.GetReloadToken()).Returns(Mock.Of<IChangeToken>());
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockAssemblyInfoProvider.Object, mockDBConnectionFactory.Object, mockPlatformIdentifier.Object, mockAsyncDelayer.Object, mockLifetime.Object, mockConfiguration.Object, null));
		}
		
		[TestMethod]
		public async Task TestWithUserStupiditiy()
		{
			var mockIOManager = new Mock<IIOManager>();
			var mockConsole = new Mock<IConsole>();
			var mockHostingEnvironment = new Mock<IWebHostEnvironment>();
			var mockAssemblyInfoProvider = new Mock<IAssemblyInformationProvider>();
			var mockDBConnectionFactory = new Mock<IDatabaseConnectionFactory>();
			var mockLifetime = new Mock<IHostApplicationLifetime>();
			var mockGeneralConfigurationOptions = new Mock<IOptions<GeneralConfiguration>>();
			var mockPlatformIdentifier = new Mock<IPlatformIdentifier>();
			var mockAsyncDelayer = new Mock<IAsyncDelayer>();
			var mockConfiguration = new Mock<IConfiguration>();
			var mockChangeToken = new Mock<IChangeToken>();

			object configReloadCallbackState = null;
			Action<object> configReloadCallback = null;
			mockChangeToken
				.Setup(x => x.RegisterChangeCallback(It.IsNotNull<Action<object>>(), null))
				.Callback<Action<object>, object>((callback, state) =>
				{
					configReloadCallback = callback;
					configReloadCallbackState = state;
				});

			mockConfiguration
				.Setup(x => x.GetReloadToken())
				.Returns(mockChangeToken.Object)
				.Verifiable();

			var testGeneralConfig = new GeneralConfiguration
			{
				SetupWizardMode = SetupWizardMode.Never
			};
			mockGeneralConfigurationOptions.SetupGet(x => x.Value).Returns(testGeneralConfig).Verifiable();

			var wizard = new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockAssemblyInfoProvider.Object, mockDBConnectionFactory.Object, mockPlatformIdentifier.Object, mockAsyncDelayer.Object, mockLifetime.Object, mockConfiguration.Object, mockGeneralConfigurationOptions.Object);


			mockPlatformIdentifier.SetupGet(x => x.IsWindows).Returns(true).Verifiable();
			mockAsyncDelayer.Setup(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

			await wizard.StartAsync(default).ConfigureAwait(false);

			testGeneralConfig.SetupWizardMode = SetupWizardMode.Force;
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => wizard.StartAsync(default)).ConfigureAwait(false);

			testGeneralConfig.SetupWizardMode = SetupWizardMode.Only;
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => wizard.StartAsync(default)).ConfigureAwait(false);

			mockConsole.SetupGet(x => x.Available).Returns(true).Verifiable();
			mockIOManager.Setup(x => x.FileExists(It.IsNotNull<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true)).Verifiable();
			mockIOManager.Setup(x => x.ReadAllBytes(It.IsNotNull<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Encoding.UTF8.GetBytes("less profane"))).Verifiable();
			mockIOManager
				.Setup(x => x.WriteAllBytes(It.IsNotNull<string>(), It.IsNotNull<byte[]>(), It.IsAny<CancellationToken>()))
				.Callback(() => configReloadCallback(configReloadCallbackState))
				.Returns(Task.CompletedTask)
				.Verifiable();
			
			var mockSuccessCommand = new Mock<DbCommand>();
			mockSuccessCommand.Setup(x => x.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(0)).Verifiable();
			mockSuccessCommand.Setup(x => x.ExecuteScalarAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult<object>("1.2.3")).Verifiable();
			var mockFailCommand = new Mock<DbCommand>();
			mockFailCommand.Setup(x => x.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).Throws(new Exception()).Verifiable();

			static void SetDbCommandCreator(Mock<DbConnection> mock, Func<DbCommand> creator) => mock.Protected().Setup<DbCommand>("CreateDbCommand").Returns(creator).Verifiable();

			var mockGoodDbConnection = new Mock<DbConnection>();
			mockGoodDbConnection.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
			SetDbCommandCreator(mockGoodDbConnection, () => mockSuccessCommand.Object);

			var mockBadDbConnection = new Mock<DbConnection>();
			mockBadDbConnection.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Throws(new Exception()).Verifiable();
			var invokeTimes = 0;
			var mockUglyDbConnection = new Mock<DbConnection>();
			SetDbCommandCreator(mockUglyDbConnection, () =>
			{
				if (invokeTimes < 2)
				{
					++invokeTimes;
					return mockSuccessCommand.Object;
				}
				else
					return mockFailCommand.Object;
			});

			mockDBConnectionFactory.Setup(x => x.CreateConnection(It.IsAny<string>(), DatabaseType.SqlServer)).Returns(mockBadDbConnection.Object).Verifiable();
			mockDBConnectionFactory.Setup(x => x.CreateConnection(It.IsAny<string>(), DatabaseType.MariaDB)).Returns(mockGoodDbConnection.Object).Verifiable();
			mockDBConnectionFactory.Setup(x => x.CreateConnection(It.IsAny<string>(), DatabaseType.MySql)).Returns(mockUglyDbConnection.Object).Verifiable();

			var finalInputSequence = new List<string>()
			{
				//first run, just say no to the force prompt after testing it
				"fake",
				"n",
				//second run say yes to the force prompt
				"y",
				//first normal run
				"bad port number",
				"0",
				"666",
				"FakeDBType",
				nameof(DatabaseType.SqlServer),
				"this isn't validated",
				"nor is this",
				"no",
				//test winauth
				"yes",
				//sql server will always fail so reconfigure with maria
				nameof(DatabaseType.MariaDB),
				"bleh",
				"blah",
				"NO",
				"user",
				"pass",
				//general config
				"four",
				"-12",
				"16",
				"eight",
				"-27",
				"5000",
				"fake token",
				"y",
				//logging config
				"no",
				//cp config
				"y",
				"y",
				// swarm config
				"n",
				//saved, now for second run
				//this time use defaults amap
				String.Empty,
				//test MySQL errors
				nameof(DatabaseType.MySql),
				String.Empty,
				String.Empty,
				"DbName",
				"n",
				"user",
				"pass",
				//general config
				"y",
				String.Empty,
				String.Empty,
				String.Empty,
				"n",
				"n",
				//logging config
				"y",
				"not actually verified because lol mocks /../!@#$%^&*()/..///.",
				"Warning",
				String.Empty,
				//cp config
				"y",
				"n",
				String.Empty,
				//swarm config
				"y",
				"node1",
				"not a url",
				"net.tcp://notandhttpAddress.com",
				"http://node1:3400",
				"privatekey",
				"n",
				"http://controller.com",
				//third run, we already hit all the code coverage so just get through it
				String.Empty,
				nameof(DatabaseType.MariaDB),
				String.Empty,
				"dbname",
				"y",
				"user",
				"pass",
				//general
				"y",
				String.Empty,
				String.Empty,
				String.Empty,
				"y",
				"y",
				"will faile",
				String.Empty,
				String.Empty,
				"fake",
				"None",
				"Critical",
				//cp config
				"y",
				"n",
				"http://fake.com, https://example.org",
				//swarm config
				"y",
				"controller",
				"https://controller.com",
				"privatekey",
				"y"
			};

			var inputPos = 0;

			mockAssemblyInfoProvider.SetupGet(x => x.VersionPrefix).Returns("sumfuk").Verifiable();


			// This list is here for ease of debugging.
			var consolePlayback = new List<string>();

			mockConsole.Setup(x => x.PressAnyKeyAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
			mockConsole.Setup(x => x.ReadLineAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(() =>
			{
				if (inputPos == finalInputSequence.Count)
					Assert.Fail("Exhausted input sequence!");
				var res = finalInputSequence[inputPos++];
				consolePlayback.Add($"Input: {res}");
				return Task.FromResult(res);
			}).Verifiable();

			mockConsole
				.Setup(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
				.Callback<string, bool, CancellationToken>((message, error, token) => consolePlayback.Add($"Output: {message}"))
				.Returns(Task.CompletedTask)
				.Verifiable();

			await wizard.StartAsync(default).ConfigureAwait(false);
			//first real run
			await wizard.StartAsync(default).ConfigureAwait(false);

			//second run
			mockIOManager.Setup(x => x.ReadAllBytes(It.IsNotNull<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Encoding.UTF8.GetBytes(String.Empty))).Verifiable();
			await wizard.StartAsync(default).ConfigureAwait(false);
		
			//third run
			testGeneralConfig.SetupWizardMode = SetupWizardMode.Autodetect;
			mockIOManager
				.Setup(x => x.WriteAllBytes(It.IsNotNull<string>(), It.IsNotNull<byte[]>(), It.IsAny<CancellationToken>()))
				.Throws(new Exception())
				.Verifiable();
			var firstRun = true;
			mockIOManager.Setup(x => x.CreateDirectory(It.IsNotNull<string>(), It.IsAny<CancellationToken>())).Returns(() =>
			{
				if (firstRun)
				{
					firstRun = false;
					throw new Exception();
				}
				return Task.CompletedTask;
			}).Verifiable();

			await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => wizard.StartAsync(default)).ConfigureAwait(false);

			Assert.AreEqual(finalInputSequence.Count, inputPos);
			mockFailCommand.VerifyAll();
			mockSuccessCommand.VerifyAll();
			mockIOManager.VerifyAll();
			mockGeneralConfigurationOptions.VerifyAll();
			mockConsole.VerifyAll();
			mockGoodDbConnection.VerifyAll();
			mockBadDbConnection.VerifyAll();
			mockUglyDbConnection.VerifyAll();
			mockDBConnectionFactory.VerifyAll();
			mockAssemblyInfoProvider.VerifyAll();
			mockPlatformIdentifier.VerifyAll();
			mockAsyncDelayer.VerifyAll();
			mockConfiguration.VerifyAll();
			mockChangeToken.VerifyAll();
		}
	}
}
