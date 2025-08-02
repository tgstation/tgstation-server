using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Common.Extensions;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class ChatTest : JobsRequiredTest
	{
		readonly IChatBotsClient chatClient;
		readonly IInstanceManagerClient instanceClient;
		readonly Api.Models.Instance metadata;

		public ChatTest(IChatBotsClient chatClient, IInstanceManagerClient instanceClient, IJobsClient jobsClient, Api.Models.Instance metadata)
			: base(jobsClient)
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

			var listTest = RunListTest(cancellationToken);
			await RunLimitTests(cancellationToken);

			await listTest;
		}

		async Task RunIrc(CancellationToken cancellationToken)
		{
			var connectionString = Environment.GetEnvironmentVariable("TGS_TEST_IRC_CONNECTION_STRING");
			if (String.IsNullOrWhiteSpace(connectionString))
				// needs to just be valid
				connectionString = new IrcConnectionStringBuilder
				{
					Address = "irc.fake.com",
					Nickname = "irc_nick",
					Password = "some_pw",
					PasswordType = IrcPasswordType.Server,
					Port = 6668,
					UseSsl = true,
				}.ToString();
			else
				// standardize
				connectionString = new IrcConnectionStringBuilder(connectionString).ToString();

			var firstBotReq = new ChatBotCreateRequest
			{
				ConnectionString = connectionString,
				Enabled = false,
				Name = "tgs_integration_test",
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

			var beforeChatBotEnabled = DateTimeOffset.UtcNow;

			var updatedBot = await chatClient.Update(new ChatBotUpdateRequest
			{
				Id = firstBot.Id,
				Enabled = true
			}, cancellationToken);

			Assert.AreEqual(true, updatedBot.Enabled);

			await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

			var jobs = await JobsClient.List(null, cancellationToken);
			var reconnectJob = jobs
				.Where(x => x.StartedAt >= beforeChatBotEnabled && x.Description.Contains(updatedBot.Name))
				.OrderBy(x => x.StartedAt)
				.FirstOrDefault();

			Assert.IsNotNull(reconnectJob);
			await WaitForJob(reconnectJob, 60, false, null, cancellationToken);

			var channelId = Environment.GetEnvironmentVariable("TGS_TEST_IRC_CHANNEL");
			if (String.IsNullOrWhiteSpace(channelId))
				channelId = "#botbus";

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
						IsSystemChannel = true,
						Tag = "butt2",
						ChannelData = channelId,
					}
				}
			}, cancellationToken);

			Assert.AreEqual(true, updatedBot.Enabled);
			Assert.IsNotNull(updatedBot.Channels);
			Assert.AreEqual(1, updatedBot.Channels.Count);
			Assert.AreEqual(false, updatedBot.Channels.First().IsAdminChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsSystemChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsUpdatesChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsWatchdogChannel);
			Assert.AreEqual("butt2", updatedBot.Channels.First().Tag);
			Assert.AreEqual(channelId, updatedBot.Channels.First().ChannelData);
		}

		async Task RunDiscord(CancellationToken cancellationToken)
		{
			var connectionString = Environment.GetEnvironmentVariable("TGS_TEST_DISCORD_TOKEN");
			if (String.IsNullOrWhiteSpace(connectionString))
				// needs to just be valid
				connectionString = new DiscordConnectionStringBuilder
				{
					BotToken = "some_token",
					DeploymentBranding = true,
					DMOutputDisplay = DiscordDMOutputDisplayType.Never,
				}.ToString();
			else
				// standardize
				connectionString = new DiscordConnectionStringBuilder(connectionString).ToString();

			var firstBotReq = new ChatBotCreateRequest
			{
				ConnectionString = connectionString,
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

			var beforeChatBotEnabled = DateTimeOffset.UtcNow;

			var updatedBot = await chatClient.Update(new ChatBotUpdateRequest
			{
				Id = firstBot.Id,
				Enabled = true
			}, cancellationToken);

			Assert.AreEqual(true, updatedBot.Enabled);

			await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

			var jobs = await JobsClient.List(null, cancellationToken);
			var reconnectJob = jobs
				.Where(x => x.StartedAt >= beforeChatBotEnabled && x.Description.Contains(updatedBot.Name))
				.OrderBy(x => x.StartedAt)
				.FirstOrDefault();

			Assert.IsNotNull(reconnectJob, $"Jobs: {JsonConvert.SerializeObject(jobs)}");
			await WaitForJob(reconnectJob, 60, false, null, cancellationToken);

			var channelIdStr = Environment.GetEnvironmentVariable("TGS_TEST_DISCORD_CHANNEL");
			if (String.IsNullOrWhiteSpace(channelIdStr))
				channelIdStr = "487268744419344384";

			var channelId = ulong.Parse(channelIdStr);

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
						IsSystemChannel = true,
						Tag = "butt",
						ChannelData = channelId.ToString(),
					}
				}
			}, cancellationToken);

			Assert.AreEqual(true, updatedBot.Enabled);
			Assert.IsNotNull(updatedBot.Channels);
			Assert.AreEqual(1, updatedBot.Channels.Count);
			Assert.AreEqual(true, updatedBot.Channels.First().IsSystemChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsAdminChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsUpdatesChannel);
			Assert.AreEqual(true, updatedBot.Channels.First().IsWatchdogChannel);
			Assert.AreEqual("butt", updatedBot.Channels.First().Tag);
			Assert.AreEqual(channelId.ToString(), updatedBot.Channels.First().ChannelData);
		}

		public async Task RunPostTest(CancellationToken cancellationToken)
		{
			var activeBots = await chatClient.List(null, cancellationToken);

			Assert.AreEqual(2, activeBots.Count);

			await ValueTaskExtensions.WhenAll(activeBots.Select(bot => chatClient.Delete(bot, cancellationToken)));

			var nowBots = await chatClient.List(null, cancellationToken);
			Assert.AreEqual(0, nowBots.Count);
		}

		async Task RunListTest(CancellationToken cancellationToken)
		{
			// regression test for GHSA-rv76-495p-g7cp
			// test starts with all perms
			var permsClient = instanceClient.CreateClient(metadata).PermissionSets;
			var ourInstancePermissionSetTask = permsClient.Read(cancellationToken);

			var ourIPS = await ourInstancePermissionSetTask;
			Assert.IsTrue(ourIPS.ChatBotRights.Value.HasFlag(ChatBotRights.ReadConnectionString));

			var results = await chatClient.List(null, cancellationToken);

			Assert.IsTrue(results.Count > 0);
			Assert.IsTrue(results.All(chatBot => chatBot.ConnectionString != null));

			var result = await chatClient.GetId(results[0], cancellationToken);

			Assert.IsNotNull(result.ConnectionString);

			await permsClient.Update(new InstancePermissionSetRequest
			{
				PermissionSetId = ourIPS.PermissionSetId,
				ChatBotRights = ourIPS.ChatBotRights.Value & (~ChatBotRights.ReadConnectionString),
			}, cancellationToken);

			results = await chatClient.List(null, cancellationToken);

			Assert.IsTrue(results.Count > 0);
			Assert.IsTrue(results.All(chatBot => chatBot.ConnectionString == null));

			result = await chatClient.GetId(results[0], cancellationToken);

			Assert.IsNull(result.ConnectionString);
		}

		async Task RunLimitTests(CancellationToken cancellationToken)
		{
			await ApiAssert.ThrowsExactly<ConflictException, ChatBotResponse>(() => chatClient.Create(new ChatBotCreateRequest
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
					ChannelData = discordBotReq.Channels.First().ChannelData
				});

			await ApiAssert.ThrowsExactly<ApiConflictException, ChatBotResponse>(() => chatClient.Update(discordBotReq, cancellationToken), ErrorCode.ChatBotMaxChannels);

			var oldChannels = discordBotReq.Channels;
			discordBotReq.Channels = null;
			discordBotReq.ChannelLimit = 0;
			await ApiAssert.ThrowsExactly<ConflictException, ChatBotResponse>(() => chatClient.Update(discordBotReq, cancellationToken), ErrorCode.ChatBotMaxChannels);

			discordBotReq.Channels = oldChannels;
			discordBotReq.ChannelLimit = null;
			await ApiAssert.ThrowsExactly<ConflictException, ChatBotResponse>(() => chatClient.Update(discordBotReq, cancellationToken), ErrorCode.ChatBotMaxChannels);

			await ApiAssert.ThrowsExactly<ConflictException, InstanceResponse>(() => instanceClient.Update(new InstanceUpdateRequest
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
