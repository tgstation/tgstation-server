using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Instance
{
	sealed class ChatTest
	{
		readonly IChatBotsClient chatClient;

		public ChatTest(IChatBotsClient chatBotsClient)
		{
			chatClient = chatBotsClient ?? throw new ArgumentNullException(nameof(chatBotsClient));
		}

		public async Task Run()
		{
			var firstBot = new ChatBot
			{
				ConnectionString = Environment.GetEnvironmentVariable("TGS4_TEST_DISCORD_TOKEN"),
				Enabled = false,
				Name = "r4407",
				Provider = ChatProvider.Discord,
				ReconnectionInterval = 1
			};

			firstBot = await chatClient.Create(firstBot, default);

			Assert.AreNotEqual(0, firstBot.Id);

			var bots = await chatClient.List(default);
			Assert.AreEqual(1, bots.Count);
			Assert.AreEqual(firstBot.Id, bots.First().Id);

			var retrievedBot = await chatClient.GetId(firstBot, default);
			Assert.AreEqual(firstBot.Id, retrievedBot);

			firstBot.Enabled = true;
			var updatedBot = await chatClient.Update(firstBot, default);

			Assert.AreEqual(true, updatedBot.Enabled);

			var channelId = UInt64.Parse(Environment.GetEnvironmentVariable("TGS4_TEST_DISCORD_CHANNEL"));
			firstBot.Channels = new List<ChatChannel>
			{
				new ChatChannel
				{
					IsAdminChannel = true,
					IsUpdatesChannel = false,
					IsWatchdogChannel = true,
					Tag = "butt",
					DiscordChannelId = channelId
				}
			};

			updatedBot = await chatClient.Update(firstBot, default);

			Assert.AreEqual(true, updatedBot.Enabled);
			Assert.IsNotNull(updatedBot.Channels);
			Assert.AreEqual(1, updatedBot.Channels.Count);
			Assert.AreEqual(true, updatedBot.Channels.First().IsAdminChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsUpdatesChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsWatchdogChannel);
			Assert.AreEqual("butt", updatedBot.Channels.First().Tag);
			Assert.AreEqual(channelId, updatedBot.Channels.First().DiscordChannelId);
			Assert.IsNull(updatedBot.Channels.First().IrcChannel);

			await chatClient.Delete(firstBot, default);
			bots = await chatClient.List(default);
			Assert.AreEqual(0, bots.Count);
		}
	}
}
