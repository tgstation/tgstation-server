using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
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

		public async Task RunPreWatchdog(CancellationToken cancellationToken)
		{
			var ircTask = RunIrc(cancellationToken);
			await RunDiscord(cancellationToken);
			await ircTask;
			await RunLimitTests(cancellationToken);
		}

		async Task RunIrc(CancellationToken cancellationToken)
		{
			var firstBotReq = new ChatBotCreateRequest
			{
				ConnectionString = Environment.GetEnvironmentVariable("TGS4_TEST_IRC_CONNECTION_STRING"),
				Enabled = false,
				Name = "tgs4_integration_test",
				Provider = ChatProvider.Irc,
				ReconnectionInterval = 1,
				ChannelLimit = 1
			};

			var csb = new IrcConnectionStringBuilder(firstBotReq.ConnectionString);

			Assert.IsTrue(csb.Valid, $"Invalid IRC connection string: {firstBotReq.ConnectionString}");

			var firstBot = await chatClient.Create(firstBotReq, cancellationToken);

			Assert.AreEqual(csb.ToString(), firstBot.ConnectionString);
			Assert.AreNotEqual(0, firstBot.Id);

			var bots = await chatClient.List(null, cancellationToken);
			Assert.AreEqual(firstBot.Id, bots.First(x => x.Provider.Value == ChatProvider.Irc).Id);

			var retrievedBot = await chatClient.GetId(firstBot, cancellationToken);
			Assert.AreEqual(firstBot.Id, retrievedBot.Id);

			var updatedBot = await chatClient.Update(new ChatBotUpdateRequest
			{
				Id = firstBot.Id,
				Enabled = true
			}, cancellationToken);

			Assert.AreEqual(true, updatedBot.Enabled);

			var channelId = Environment.GetEnvironmentVariable("TGS4_TEST_IRC_CHANNEL");;

			updatedBot = await chatClient.Update(new ChatBotUpdateRequest
			{
				Id = firstBot.Id,
				Channels = new List<ChatChannel>
				{
					new ChatChannel
					{
						IsAdminChannel = false,
						IsUpdatesChannel = true,
						IsWatchdogChannel = true,
						Tag = "butt2",
						IrcChannel = channelId
					}
				}
			}, cancellationToken);

			Assert.AreEqual(true, updatedBot.Enabled);
			Assert.IsNotNull(updatedBot.Channels);
			Assert.AreEqual(1, updatedBot.Channels.Count);
			Assert.AreEqual(false, updatedBot.Channels.First().IsAdminChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsUpdatesChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsWatchdogChannel);
			Assert.AreEqual("butt2", updatedBot.Channels.First().Tag);
			Assert.AreEqual(channelId, updatedBot.Channels.First().IrcChannel);
			Assert.IsNull(updatedBot.Channels.First().DiscordChannelId);
		}

		async Task RunDiscord(CancellationToken cancellationToken)
		{
			var firstBotReq = new ChatBotCreateRequest
			{
				ConnectionString =
					new DiscordConnectionStringBuilder
					{
						BotToken = Environment.GetEnvironmentVariable("TGS4_TEST_DISCORD_TOKEN"),
						DMOutputDisplay = DiscordDMOutputDisplayType.OnError
					}.ToString(),
				Enabled = false,
				Name = "r4407",
				Provider = ChatProvider.Discord,
				ReconnectionInterval = 1,
				ChannelLimit = 1
			};

			var csb = new DiscordConnectionStringBuilder(firstBotReq.ConnectionString);

			Assert.IsTrue(csb.Valid, $"Invalid Discord connection string: {firstBotReq.ConnectionString}");

			var firstBot = await chatClient.Create(firstBotReq, cancellationToken);

			Assert.AreEqual(csb.ToString(), firstBot.ConnectionString);
			Assert.AreNotEqual(0, firstBot.Id);

			var bots = await chatClient.List(null, cancellationToken);
			Assert.AreEqual(firstBot.Id, bots.First(x => x.Provider.Value == ChatProvider.Discord).Id);

			var retrievedBot = await chatClient.GetId(firstBot, cancellationToken);
			Assert.AreEqual(firstBot.Id, retrievedBot.Id);

			var updatedBot = await chatClient.Update(new ChatBotUpdateRequest
			{
				Id = firstBot.Id,
				Enabled = true
			}, cancellationToken);

			Assert.AreEqual(true, updatedBot.Enabled);

			var channelId = UInt64.Parse(Environment.GetEnvironmentVariable("TGS4_TEST_DISCORD_CHANNEL"));
			firstBot.Channels = new List<ChatChannel>
			{
				new ChatChannel
				{
					IsAdminChannel = true,
					IsUpdatesChannel = true,
					IsWatchdogChannel = true,
					Tag = "butt",
					DiscordChannelId = channelId
				}
			};

			updatedBot = await chatClient.Update(new ChatBotUpdateRequest
			{
				Id = firstBot.Id,
				Channels = new List<ChatChannel>
				{
					new ChatChannel
					{
						IsAdminChannel = true,
						IsUpdatesChannel = true,
						IsWatchdogChannel = true,
						Tag = "butt",
						DiscordChannelId = channelId
					}
				}
			}, cancellationToken);

			Assert.AreEqual(true, updatedBot.Enabled);
			Assert.IsNotNull(updatedBot.Channels);
			Assert.AreEqual(1, updatedBot.Channels.Count);
			Assert.AreEqual(true, updatedBot.Channels.First().IsAdminChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsUpdatesChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsWatchdogChannel);
			Assert.AreEqual("butt", updatedBot.Channels.First().Tag);
			Assert.AreEqual(channelId, updatedBot.Channels.First().DiscordChannelId);
			Assert.IsNull(updatedBot.Channels.First().IrcChannel);
		}

		public async Task RunPostTest(CancellationToken cancellationToken)
		{
			var activeBots = await chatClient.List(null, cancellationToken);

			Assert.AreEqual(2, activeBots.Count);

			await Task.WhenAll(activeBots.Select(bot => chatClient.Delete(bot, cancellationToken)));

			var nowBots = await chatClient.List(null, cancellationToken);
			Assert.AreEqual(0, nowBots.Count);
		}

		async Task RunLimitTests(CancellationToken cancellationToken)
		{
			await ApiAssert.ThrowsException<ConflictException>(() => chatClient.Create(new ChatBotCreateRequest
			{
				Name = "asdf",
				ConnectionString = "asdf",
				Provider = ChatProvider.Irc
			}, cancellationToken), ErrorCode.ChatBotMax);

			var bots = await chatClient.List(null, cancellationToken);
			var ogDiscordBot = bots.First(bot => bot.Provider.Value == ChatProvider.Discord); ;
			var discordBotReq = new ChatBotUpdateRequest
			{
				Id = ogDiscordBot.Id,
				Channels = ogDiscordBot.Channels.ToList(),
				ChannelLimit = 1
			};

			// We limited chat bots and channels to 1 and 2 respectively, try violating them
			discordBotReq.Channels.Add(
				new ChatChannel
				{
					IsAdminChannel = true,
					IsUpdatesChannel = false,
					IsWatchdogChannel = true,
					Tag = "butt",
					DiscordChannelId = discordBotReq.Channels.First().DiscordChannelId
				});

			await ApiAssert.ThrowsException<ApiConflictException>(() => chatClient.Update(discordBotReq, cancellationToken), ErrorCode.ChatBotMaxChannels);

			var oldChannels = discordBotReq.Channels;
			discordBotReq.Channels = null;
			discordBotReq.ChannelLimit = 0;
			await ApiAssert.ThrowsException<ConflictException>(() => chatClient.Update(discordBotReq, cancellationToken), ErrorCode.ChatBotMaxChannels);

			discordBotReq.Channels = oldChannels;
			discordBotReq.ChannelLimit = null;
			await ApiAssert.ThrowsException<ConflictException>(() => chatClient.Update(discordBotReq, cancellationToken), ErrorCode.ChatBotMaxChannels);

			await ApiAssert.ThrowsException<ConflictException>(() => instanceClient.Update(new InstanceUpdateRequest
			{
				Id = metadata.Id,
				ChatBotLimit = 0
			}, cancellationToken), ErrorCode.ChatBotMax);

			discordBotReq.ChannelLimit = 20;
			discordBotReq.Channels = null;
			await chatClient.Update(discordBotReq, cancellationToken);
		}
	}
}
