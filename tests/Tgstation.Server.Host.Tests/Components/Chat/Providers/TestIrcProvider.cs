using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
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
			Assert.ThrowsException<ArgumentNullException>(() => new IrcProvider(null, null, null, null, null));
			var mockJobManager = new Mock<IJobManager>();
			Assert.ThrowsException<ArgumentNullException>(() => new IrcProvider(mockJobManager.Object, null, null, null, null));
			var mockAss = new Mock<IAssemblyInformationProvider>();
			Assert.ThrowsException<ArgumentNullException>(() => new IrcProvider(mockJobManager.Object, mockAss.Object, null, null, null));
			var mockAsyncDelayer = new Mock<IAsyncDelayer>();
			Assert.ThrowsException<ArgumentNullException>(() => new IrcProvider(mockJobManager.Object, mockAss.Object, mockAsyncDelayer.Object, null, null));
			var mockLogger = new Mock<ILogger<IrcProvider>>();
			Assert.ThrowsException<ArgumentNullException>(() => new IrcProvider(mockJobManager.Object, mockAss.Object, mockAsyncDelayer.Object, mockLogger.Object, null));

			var mockBot = new ChatBot
			{
				Name = "test",
				Provider = ChatProvider.Irc
			};

			Assert.ThrowsException<InvalidOperationException>(() => new IrcProvider(mockJobManager.Object, mockAss.Object, mockAsyncDelayer.Object, mockLogger.Object, mockBot));

			mockBot.ConnectionString = new IrcConnectionStringBuilder
			{
				Address = "localhost",
				Nickname = "test",
				UseSsl = true,
				Port = 6667
			}.ToString();

			await new IrcProvider(mockJobManager.Object, mockAss.Object, mockAsyncDelayer.Object, mockLogger.Object, mockBot).DisposeAsync();
		}
	}
}
