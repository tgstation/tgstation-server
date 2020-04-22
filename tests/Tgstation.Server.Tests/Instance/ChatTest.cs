using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Instance
{
	sealed class ChatTest
	{
		readonly IChatBotsClient chatClient;
		readonly IInstanceManagerClient instanceClient;
		readonly Api.Models.Instance metadata;

		public ChatTest(IChatBotsClient chatClient, IInstanceManagerClient instanceClient, Api.Models.Instance metadata)
		{
			this.chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			var firstBot = new ChatBot
			{
				ConnectionString = Environment.GetEnvironmentVariable("TGS4_TEST_DISCORD_TOKEN"),
				Enabled = false,
				Name = "r4407",
				Provider = ChatProvider.Discord,
				ReconnectionInterval = 1,
				ChannelLimit = 1
			};

			firstBot = await chatClient.Create(firstBot, cancellationToken);

			Assert.AreNotEqual(0, firstBot.Id);

			var bots = await chatClient.List(cancellationToken);
			Assert.AreEqual(1, bots.Count);
			Assert.AreEqual(firstBot.Id, bots.First().Id);

			var retrievedBot = await chatClient.GetId(firstBot, cancellationToken);
			Assert.AreEqual(firstBot.Id, retrievedBot.Id);

			firstBot.Enabled = true;
			var updatedBot = await chatClient.Update(firstBot, cancellationToken);

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
			

			updatedBot = await chatClient.Update(firstBot, cancellationToken);

			Assert.AreEqual(true, updatedBot.Enabled);
			Assert.IsNotNull(updatedBot.Channels);
			Assert.AreEqual(1, updatedBot.Channels.Count);
			Assert.AreEqual(true, updatedBot.Channels.First().IsAdminChannel);
			Assert.AreEqual(false, updatedBot.Channels.First().IsUpdatesChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsWatchdogChannel);
			Assert.AreEqual("butt", updatedBot.Channels.First().Tag);
			Assert.AreEqual(channelId, updatedBot.Channels.First().DiscordChannelId);
			Assert.IsNull(updatedBot.Channels.First().IrcChannel);

			await ApiAssert.ThrowsException<ConflictException>(() => chatClient.Create(new ChatBot
			{
				Name = "asdf",
				ConnectionString = "asdf",
				Provider = ChatProvider.Irc
			}, cancellationToken), ErrorCode.ChatBotMax);

			// We limited chat bots and channels to 1, try violating them
			updatedBot.Channels.Add(
				new ChatChannel
				{
					IsAdminChannel = true,
					IsUpdatesChannel = false,
					IsWatchdogChannel = true,
					Tag = "butt",
					DiscordChannelId = channelId
				});

			await ApiAssert.ThrowsException<ApiConflictException>(() => chatClient.Update(updatedBot, cancellationToken), ErrorCode.ChatBotMaxChannels);

			var oldChannels = updatedBot.Channels;
			updatedBot.Channels = null;
			updatedBot.ChannelLimit = 0;
			await ApiAssert.ThrowsException<ConflictException>(() => chatClient.Update(updatedBot, cancellationToken), ErrorCode.ChatBotMaxChannels);

			updatedBot.Channels = oldChannels;
			updatedBot.ChannelLimit = null;
			await ApiAssert.ThrowsException<ConflictException>(() => chatClient.Update(updatedBot, cancellationToken), ErrorCode.ChatBotMaxChannels);

			var instance = metadata.CloneMetadata();
			instance.ChatBotLimit = 0;
			await ApiAssert.ThrowsException<ConflictException>(() => instanceClient.Update(instance, cancellationToken), ErrorCode.ChatBotMax);

			await chatClient.Delete(firstBot, cancellationToken);
			bots = await chatClient.List(cancellationToken);
			Assert.AreEqual(0, bots.Count);
		}
	}
}
