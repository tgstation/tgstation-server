using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Chat.Providers.Tests
{
	[TestClass]
	public sealed class TestIrcProvider
	{
		[TestMethod]
		public async Task TestConstructionAndDisposal()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new IrcProvider(null, null, null, null, null, null));
			var mockJobManager = new Mock<IJobManager>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new IrcProvider(mockJobManager.Object, null, null, null, null, null));
			var mockAsyncDelayer = new Mock<IAsyncDelayer>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new IrcProvider(mockJobManager.Object, mockAsyncDelayer.Object, null, null, null, null));
			var mockLogger = new Mock<ILogger<IrcProvider>>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new IrcProvider(mockJobManager.Object, mockAsyncDelayer.Object, mockLogger.Object, null, null, null));
			var mockAss = new Mock<IAssemblyInformationProvider>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new IrcProvider(mockJobManager.Object, mockAsyncDelayer.Object, mockLogger.Object, mockAss.Object, null, null));

			var mockBot = new ChatBot
			{
				Name = "test",
				Instance = new Models.Instance(),
				Provider = ChatProvider.Irc
			};
			Assert.ThrowsExactly<ArgumentNullException>(() => new IrcProvider(mockJobManager.Object, mockAsyncDelayer.Object, mockLogger.Object, mockAss.Object, mockBot, null));

			var mockLogConf = new FileLoggingConfiguration();
			Assert.ThrowsExactly<InvalidOperationException>(() => new IrcProvider(mockJobManager.Object, mockAsyncDelayer.Object, mockLogger.Object, mockAss.Object, mockBot, mockLogConf));

			mockBot.ConnectionString = new IrcConnectionStringBuilder
			{
				Address = "localhost",
				Nickname = "test",
				UseSsl = true,
				Port = 6667
			}.ToString();

			await new IrcProvider(mockJobManager.Object, mockAsyncDelayer.Object, mockLogger.Object, mockAss.Object, mockBot, mockLogConf).DisposeAsync();
		}

		static ValueTask InvokeConnect(IProvider provider, CancellationToken cancellationToken = default) => (ValueTask)provider.GetType().GetMethod("Connect", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(provider, new object[] { cancellationToken });

		[TestMethod]
		public async Task TestConnectAndDisconnect()
		{
			var actualToken = Environment.GetEnvironmentVariable("TGS_TEST_IRC_CONNECTION_STRING");
			if (String.IsNullOrWhiteSpace(actualToken))
				Assert.Inconclusive("Required environment variable TGS_TEST_IRC_CONNECTION_STRING isn't set!");

			if (!new IrcConnectionStringBuilder(actualToken).Valid)
				Assert.Fail("TGS_TEST_IRC_CONNECTION_STRING is not a valid IRC connection string!");

			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});
			var mockSetup = new Mock<IJobManager>();
			mockSetup
				.Setup(x => x.RegisterOperation(It.IsNotNull<Job>(), It.IsNotNull<JobEntrypoint>(), It.IsAny<CancellationToken>()))
				.Callback<Job, JobEntrypoint, CancellationToken>((job, entrypoint, cancellationToken) => job.StartedBy ??= new User { })
				.Returns(ValueTask.CompletedTask);
			mockSetup
				.Setup(x => x.WaitForJobCompletion(It.IsNotNull<Job>(), It.IsAny<User>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
				.Returns(ValueTask.FromResult<bool?>(true));
			var mockJobManager = mockSetup.Object;

			var chatBot = new ChatBot
			{
				ConnectionString = actualToken,
				Provider = ChatProvider.Irc,
				Instance = new Models.Instance(),
			};

			await using var provider = new IrcProvider(mockJobManager, new AsyncDelayer(loggerFactory.CreateLogger<AsyncDelayer>()), loggerFactory.CreateLogger<IrcProvider>(), Mock.Of<IAssemblyInformationProvider>(), chatBot, new FileLoggingConfiguration());
			Assert.IsFalse(provider.Connected);
			await InvokeConnect(provider);
			Assert.IsTrue(provider.Connected);

			await Task.Delay(2000); // IRC servers do not like it when you connect and disconnect in rapid succession

			await provider.Disconnect(default);
			Assert.IsFalse(provider.Connected);

			await Task.Delay(2000); // same as above

			await InvokeConnect(provider);
			await Task.Delay(2000); // make sure it stays connected after a reconnect attempt
			Assert.IsTrue(provider.Connected);

			await provider.Disconnect(default);
			Assert.IsFalse(provider.Connected);
		}
	}
}
