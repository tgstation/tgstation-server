using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Core.Tests
{
	[TestClass]
	public sealed class TestSetupWizard
	{
		[TestMethod]
		public void TestConstructionThrows()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(null, null, null, null, null, null, null));
			var mockIOManager = new Mock<IIOManager>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, null, null, null, null, null, null));
			var mockConsole = new Mock<IConsole>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, null, null, null, null, null));
			var mockHostingEnvironment = new Mock<IHostingEnvironment>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, null, null, null, null));
			var mockApplication = new Mock<IApplication>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockApplication.Object, null, null, null));
			var mockDBConnectionFactory = new Mock<IDBConnectionFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockApplication.Object, mockDBConnectionFactory.Object, null, null));
			var mockLogger = new Mock<ILogger<SetupWizard>>();
			Assert.ThrowsException<ArgumentNullException>(() => new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockApplication.Object, mockDBConnectionFactory.Object, mockLogger.Object, null));
		}

		//TODO
		[TestMethod]
		public async Task WIPTestWithUserStupiditiy()
		{
			Assert.Inconclusive();

			var mockIOManager = new Mock<IIOManager>();
			var mockConsole = new Mock<IConsole>();
			var mockHostingEnvironment = new Mock<IHostingEnvironment>();
			var mockApplication = new Mock<IApplication>();
			var mockDBConnectionFactory = new Mock<IDBConnectionFactory>();
			var mockLogger = new Mock<ILogger<SetupWizard>>();
			var mockGeneralConfigurationOptions = new Mock<IOptions<GeneralConfiguration>>();

			var testGeneralConfig = new GeneralConfiguration
			{
				SetupWizardMode = SetupWizardMode.Never
			};
			mockGeneralConfigurationOptions.SetupGet(x => x.Value).Returns(testGeneralConfig).Verifiable();

			var wizard = new SetupWizard(mockIOManager.Object, mockConsole.Object, mockHostingEnvironment.Object, mockApplication.Object, mockDBConnectionFactory.Object, mockLogger.Object, mockGeneralConfigurationOptions.Object);

			Assert.IsFalse(await wizard.CheckRunWizard(default).ConfigureAwait(false));

			testGeneralConfig.SetupWizardMode = SetupWizardMode.Force;
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => wizard.CheckRunWizard(default)).ConfigureAwait(false);

			testGeneralConfig.SetupWizardMode = SetupWizardMode.Only;
			mockConsole.SetupGet(x => x.Available).Returns(true).Verifiable();
			Assert.IsFalse(await wizard.CheckRunWizard(default).ConfigureAwait(false));

			mockIOManager.Setup(x => x.FileExists(It.IsNotNull<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true)).Verifiable();
			mockIOManager.Setup(x => x.ReadAllBytes(It.IsNotNull<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Encoding.UTF8.GetBytes("cucked"))).Verifiable();
			mockIOManager.Setup(x => x.WriteAllBytes(It.IsNotNull<string>(), It.IsNotNull<byte[]>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
			
			var mockGoodDbConnection = new Mock<DbConnection>();
			mockGoodDbConnection.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

			var mockBadDbConnection = new Mock<DbConnection>();
			mockGoodDbConnection.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Throws(new Exception()).Verifiable();

			void AddVersionReturn(Mock<DbCommand> mock) => mock.Setup(x => x.ExecuteScalarAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult<object>("1.2.3")).Verifiable();
			var mockSuccessCommand = new Mock<DbCommand>();
			mockSuccessCommand.Setup(x => x.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(0)).Verifiable();
			AddVersionReturn(mockSuccessCommand);
			var mockFailCommand = new Mock<DbCommand>();
			mockFailCommand.Setup(x => x.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).Throws(new Exception()).Verifiable();
			AddVersionReturn(mockFailCommand);
			var secondTime = false;
			var mockUglyDbConnection = new Mock<DbConnection>();
			mockUglyDbConnection.Setup(x => x.CreateCommand()).Returns(() =>
			{
				if (!secondTime)
				{
					secondTime = true;
					return mockSuccessCommand.Object;
				}
				else
					return mockFailCommand.Object;
			}).Verifiable();

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
			};
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				//test winauth
				finalInputSequence.Add("yes");
			else
				finalInputSequence.AddRange(new List<string>
				{
					"username",
					"password"
				});
			finalInputSequence.AddRange(new List<string>
			{
				//sql server will always fail so reconfigure with maria
				nameof(DatabaseType.MariaDB),
				"bleh",
				"blah",
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
				//saved, now for second run
				//this time use defaults amap

				//TODO
			});

			var inputPos = 0;

			mockConsole.Setup(x => x.PressAnyKeyAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
			mockConsole.Setup(x => x.ReadLineAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(() =>
			{
				if (inputPos == finalInputSequence.Count)
					Assert.Fail("Exhausted input sequence!");
				return Task.FromResult(finalInputSequence[inputPos++]);
			}).Verifiable();
			mockConsole.Setup(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

			//first real run
			Assert.IsTrue(await wizard.CheckRunWizard(default).ConfigureAwait(false));

			//second run
			mockIOManager.Setup(x => x.ReadAllBytes(It.IsNotNull<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Encoding.UTF8.GetBytes(String.Empty))).Verifiable();
			Assert.IsTrue(await wizard.CheckRunWizard(default).ConfigureAwait(false));
		
			//third run
			testGeneralConfig.SetupWizardMode = SetupWizardMode.Autodetect;
			mockIOManager.Setup(x => x.WriteAllBytes(It.IsNotNull<string>(), It.IsNotNull<byte[]>(), It.IsAny<CancellationToken>())).Throws(new Exception()).Verifiable();
			await Assert.ThrowsExceptionAsync<Exception>(() => wizard.CheckRunWizard(default)).ConfigureAwait(false);

			mockFailCommand.VerifyAll();
			mockSuccessCommand.VerifyAll();
			mockIOManager.VerifyAll();
			mockGeneralConfigurationOptions.VerifyAll();
			mockConsole.VerifyAll();
			mockGoodDbConnection.VerifyAll();
			mockDBConnectionFactory.VerifyAll();
		}
	}
}
