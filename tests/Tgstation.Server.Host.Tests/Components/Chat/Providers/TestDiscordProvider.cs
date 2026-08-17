using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using OneOf;

using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Chat.Providers.Tests
{
	[TestClass]
	public sealed class TestDiscordProvider
	{
		static ChatBot testToken1;
		static IJobManager mockJobManager;

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			var actualToken = Environment.GetEnvironmentVariable("TGS_TEST_DISCORD_TOKEN");
			if (!String.IsNullOrWhiteSpace(actualToken))
				testToken1 = new ChatBot
				{
					ConnectionString = actualToken,
					ReconnectionInterval = 1,
					Instance = new Models.Instance()
				};

			var mockSetup = new Mock<IJobManager>();
			mockSetup
				.Setup(x => x.RegisterOperation(It.IsNotNull<Job>(), It.IsNotNull<JobEntrypoint>(), It.IsAny<CancellationToken>()))
				.Callback<Job, JobEntrypoint, CancellationToken>((job, entrypoint, cancellationToken) => job.StartedBy ??= new User { })
				.Returns(ValueTask.CompletedTask);
			mockSetup
				.Setup(x => x.WaitForJobCompletion(It.IsNotNull<Job>(), It.IsAny<User>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
				.Returns(ValueTask.FromResult<bool?>(true));
			mockJobManager = mockSetup.Object;
		}

		[TestMethod]
		public async Task TestConstructionAndDisposal()
		{
			Func<IReadOnlyList<string>> commandNamesFactory = () => Array.Empty<string>();
			var bot = new ChatBot
			{
				ConnectionString = "fake_token",
				ReconnectionInterval = 1,
				Instance = new Models.Instance(),
			};

			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(null, null, null, null, null, null, commandNamesFactory));
			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(mockJobManager, null, null, null, null, null, commandNamesFactory));
			var mockDel = Mock.Of<IAsyncDelayer>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(mockJobManager, mockDel, null, null, null, null, commandNamesFactory));
			var mockLogger = Mock.Of<ILogger<DiscordProvider>>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(mockJobManager, mockDel, mockLogger, null, null, null, commandNamesFactory));
			var mockAss = Mock.Of<IAssemblyInformationProvider>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(mockJobManager, mockDel, mockLogger, mockAss, null, null, commandNamesFactory));
			var mockGen = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			mockGen.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration());
			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(mockJobManager, mockDel, mockLogger, mockAss, mockGen.Object, null, commandNamesFactory));
			await new DiscordProvider(mockJobManager, mockDel, mockLogger, mockAss, mockGen.Object, bot, commandNamesFactory).DisposeAsync();
		}

		[TestMethod]
		public async Task TestSlashCommandInteraction()
		{
			const ulong applicationId = 1;
			const ulong channelId = 2;
			const ulong userId = 3;
			var channelSnowflake = new Snowflake(channelId);

			var interactionApi = new Mock<IDiscordRestInteractionAPI>();
			interactionApi
				.Setup(x => x.CreateInteractionResponseAsync(
					It.IsAny<Snowflake>(),
					It.IsAny<string>(),
					It.IsAny<IInteractionResponse>(),
					It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
					It.IsAny<CancellationToken>()))
				.ReturnsAsync(Result.FromSuccess());
			interactionApi
				.Setup(x => x.EditOriginalInteractionResponseAsync(
					It.IsAny<Snowflake>(),
					It.IsAny<string>(),
					It.IsAny<Optional<string>>(),
					It.IsAny<Optional<IReadOnlyList<IEmbed>>>(),
					It.IsAny<Optional<IAllowedMentions>>(),
					It.IsAny<Optional<IReadOnlyList<IMessageComponent>>>(),
					It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
					It.IsAny<Optional<MessageFlags>>(),
					It.IsAny<CancellationToken>()))
				.ReturnsAsync(Result<IMessage>.FromSuccess(Mock.Of<IMessage>()));

			var channel = new Mock<IChannel>();
			channel.SetupGet(x => x.ID).Returns(new Snowflake(channelId));
			channel.SetupGet(x => x.Type).Returns(ChannelType.DM);
			channel.SetupGet(x => x.Name).Returns(new Optional<string>("test-channel"));
			var channelApi = new Mock<IDiscordRestChannelAPI>();
			channelApi
				.Setup(x => x.GetChannelAsync(channelSnowflake, default(CancellationToken)))
				.ReturnsAsync(Result<IChannel>.FromSuccess(channel.Object));

			var user = new Mock<IUser>();
			user.SetupGet(x => x.ID).Returns(new Snowflake(userId));
			user.SetupGet(x => x.Username).Returns("test-user");

			var option = new Mock<IApplicationCommandInteractionDataOption>();
			option.SetupGet(x => x.Name).Returns("command");
			option.SetupGet(x => x.Value).Returns(
				new Optional<OneOf<string, long, bool, Snowflake, double>>(
					OneOf<string, long, bool, Snowflake, double>.FromT0("info")));
			var argumentsOption = new Mock<IApplicationCommandInteractionDataOption>();
			argumentsOption.SetupGet(x => x.Name).Returns("arguments");
			argumentsOption.SetupGet(x => x.Value).Returns(
				new Optional<OneOf<string, long, bool, Snowflake, double>>(
					OneOf<string, long, bool, Snowflake, double>.FromT0("--verbose")));

			var commandData = new Mock<IApplicationCommandData>();
			commandData.SetupGet(x => x.Name).Returns("tgs");
			commandData.SetupGet(x => x.Options).Returns(
				new Optional<IReadOnlyList<IApplicationCommandInteractionDataOption>>(
					new List<IApplicationCommandInteractionDataOption> { option.Object, argumentsOption.Object }));

			var partialChannel = new Mock<IPartialChannel>();
			partialChannel.SetupGet(x => x.ID).Returns(new Snowflake(channelId));
			var interaction = new Mock<IInteractionCreate>();
			interaction.SetupGet(x => x.ID).Returns(new Snowflake(4));
			interaction.SetupGet(x => x.ApplicationID).Returns(new Snowflake(applicationId));
			interaction.SetupGet(x => x.Type).Returns(InteractionType.ApplicationCommand);
			interaction.SetupGet(x => x.Data).Returns(
				new Optional<OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>>(
					OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>.FromT0(commandData.Object)));
			interaction.SetupGet(x => x.Channel).Returns(new Optional<IPartialChannel>(partialChannel.Object));
			interaction.SetupGet(x => x.User).Returns(new Optional<IUser>(user.Object));
			interaction.SetupGet(x => x.Token).Returns("interaction-token");

			var serviceProvider = new ServiceCollection()
				.AddSingleton<IDiscordRestInteractionAPI>(interactionApi.Object)
				.AddSingleton<IDiscordRestChannelAPI>(channelApi.Object)
				.BuildServiceProvider();
			await using var provider = new DiscordProvider(
				mockJobManager,
				Mock.Of<IAsyncDelayer>(),
				Mock.Of<ILogger<DiscordProvider>>(),
				Mock.Of<IAssemblyInformationProvider>(),
				Mock.Of<IOptionsMonitor<GeneralConfiguration>>(),
				new ChatBot
				{
					ConnectionString = "fake_token",
					Instance = new Models.Instance(),
				},
				() => new List<string> { "info" },
				serviceProvider);

			var nextMessage = provider.NextMessage(CancellationToken.None);
			var result = await provider.RespondAsync(interaction.Object, CancellationToken.None);
			var message = await nextMessage;

			Assert.IsTrue(result.IsSuccess);
			Assert.IsNotNull(message);
			Assert.AreEqual("!tgs info --verbose", message.Content);
			Assert.AreEqual(channelId, message.User.Channel.RealId);
			Assert.AreEqual(userId, message.User.RealId);
			Assert.IsTrue(message.User.Channel.IsPrivateChannel);
			Assert.AreEqual(new Snowflake(applicationId), ((DiscordMessage)message).ApplicationId.Value);
			Assert.AreEqual("interaction-token", ((DiscordMessage)message).InteractionToken);
			await provider.SendMessage(message, new MessageContent { Text = "reply" }, channelId, CancellationToken.None);
			interactionApi.Verify(
					x => x.CreateInteractionResponseAsync(
						It.IsAny<Snowflake>(),
						It.IsAny<string>(),
						It.IsAny<IInteractionResponse>(),
						It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
						It.IsAny<CancellationToken>()),
				Times.Once);
			var editInvocation = interactionApi.Invocations.Single(
				invocation => invocation.Method.Name == nameof(IDiscordRestInteractionAPI.EditOriginalInteractionResponseAsync));
			Assert.AreEqual(new Snowflake(applicationId), editInvocation.Arguments[0]);
			Assert.AreEqual("interaction-token", editInvocation.Arguments[1]);
			Assert.AreEqual("reply", ((Optional<string>)editInvocation.Arguments[2]).Value);
		}

		[TestMethod]
		public async Task TestSlashCommandAutocomplete()
		{
			var interactionApi = new Mock<IDiscordRestInteractionAPI>();
			interactionApi
				.Setup(x => x.CreateInteractionResponseAsync(
					It.IsAny<Snowflake>(),
					It.IsAny<string>(),
					It.IsAny<IInteractionResponse>(),
					It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
					It.IsAny<CancellationToken>()))
				.ReturnsAsync(Result.FromSuccess());

			var option = new Mock<IApplicationCommandInteractionDataOption>();
			option.SetupGet(x => x.Name).Returns("command");
			option.SetupGet(x => x.IsFocused).Returns(new Optional<bool>(true));
			option.SetupGet(x => x.Value).Returns(
				new Optional<OneOf<string, long, bool, Snowflake, double>>(
					OneOf<string, long, bool, Snowflake, double>.FromT0("re")));

			var commandData = new Mock<IApplicationCommandData>();
			commandData.SetupGet(x => x.Name).Returns("tgs");
			commandData.SetupGet(x => x.Options).Returns(
				new Optional<IReadOnlyList<IApplicationCommandInteractionDataOption>>(
					new List<IApplicationCommandInteractionDataOption> { option.Object }));

			var interaction = new Mock<IInteractionCreate>();
			interaction.SetupGet(x => x.ID).Returns(new Snowflake(1));
			interaction.SetupGet(x => x.Type).Returns(InteractionType.ApplicationCommandAutocomplete);
			interaction.SetupGet(x => x.Data).Returns(
				new Optional<OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>>(
					OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>.FromT0(commandData.Object)));
			interaction.SetupGet(x => x.Token).Returns("interaction-token");

			var serviceProvider = new ServiceCollection()
				.AddSingleton<IDiscordRestInteractionAPI>(interactionApi.Object)
				.BuildServiceProvider();
			await using var provider = new DiscordProvider(
				mockJobManager,
				Mock.Of<IAsyncDelayer>(),
				Mock.Of<ILogger<DiscordProvider>>(),
				Mock.Of<IAssemblyInformationProvider>(),
				Mock.Of<IOptionsMonitor<GeneralConfiguration>>(),
				new ChatBot
				{
					ConnectionString = "fake_token",
					Instance = new Models.Instance(),
				},
				() => new List<string> { "info", "restart", "revision" },
				serviceProvider);

			var result = await provider.RespondAsync(interaction.Object, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			var response = (IInteractionResponse)interactionApi.Invocations.Single().Arguments[2];
			Assert.AreEqual(InteractionCallbackType.ApplicationCommandAutocompleteResult, response.Type);
			CollectionAssert.AreEqual(
				new[] { "restart", "revision" },
				response.Data.Value.AsT1.Choices.Select(choice => choice.Name).ToArray());
			Assert.AreEqual("tgs", DiscordProvider.NormalizeSlashCommandName("TGS"));
		}

		[TestMethod]
		public void TestSlashCommandOptionEnablesAutocomplete()
		{
			var options = (IReadOnlyList<IApplicationCommandOption>)typeof(DiscordProvider)
				.GetField("SlashCommandOptions", BindingFlags.Static | BindingFlags.NonPublic)!
				.GetValue(null)!;

			Assert.IsTrue(options[0].IsRequired.HasValue && options[0].IsRequired.Value);
			Assert.IsTrue(options[0].EnableAutocomplete.HasValue && options[0].EnableAutocomplete.Value);
			Assert.IsFalse(options[0].IsDefault.HasValue);
		}

		[TestMethod]
		public void TestSlashCommandNameMustNotContainSlash()
		{
			Assert.AreEqual("tgs", DiscordProvider.NormalizeSlashCommandName("TGS"));
			Assert.AreEqual("тгс", DiscordProvider.NormalizeSlashCommandName("ТГС"));
			Assert.ThrowsExactly<ArgumentException>(() => DiscordProvider.NormalizeSlashCommandName("/tgs"));
		}

		[TestMethod]
		public void TestRegisteredTgsSlashCommandsAreStale()
		{
			static IApplicationCommand CreateCommand(string name, string description)
			{
				var command = new Mock<IApplicationCommand>();
				command.SetupGet(x => x.Name).Returns(name);
				command.SetupGet(x => x.Type).Returns(ApplicationCommandType.ChatInput);
				command.SetupGet(x => x.Description).Returns(description);
				return command.Object;
			}

			Assert.IsTrue(DiscordProvider.IsTgsSlashCommand(CreateCommand("custom-a", "Run a TGS chat command."), "custom-b"));
			Assert.IsFalse(DiscordProvider.IsTgsSlashCommand(CreateCommand("custom-b", "Run a TGS chat command."), "custom-b"));
			Assert.IsFalse(DiscordProvider.IsTgsSlashCommand(CreateCommand("custom-a", "Unrelated command."), "custom-b"));
		}

		static ValueTask InvokeConnect(IProvider provider, CancellationToken cancellationToken = default) => (ValueTask)provider.GetType().GetMethod("Connect", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(provider, new object[] { cancellationToken });

		[TestMethod]
		public async Task TestConnectWithFakeTokenFails()
		{
			var mockLogger = new Mock<ILogger<DiscordProvider>>();
			await using var provider = new DiscordProvider(mockJobManager, Mock.Of<IAsyncDelayer>(), mockLogger.Object, Mock.Of<IAssemblyInformationProvider>(), Mock.Of<IOptionsMonitor<GeneralConfiguration>>(), new ChatBot
			{
				ReconnectionInterval = 1,
				ConnectionString = "asdf",
				Instance = new Models.Instance(),
			}, () => Array.Empty<string>());
			await Assert.ThrowsExactlyAsync<JobException>(async () => await InvokeConnect(provider));
			Assert.IsFalse(provider.Connected);
		}

		[TestMethod]
		public async Task TestConnectAndDisconnect()
		{
			if (testToken1 == null)
				Assert.Inconclusive("Required environment variable TGS_TEST_DISCORD_TOKEN isn't set!");

			if (!new DiscordConnectionStringBuilder(testToken1.ConnectionString).Valid)
				Assert.Fail("TGS_TEST_DISCORD_TOKEN is not a valid Discord connection string!");

			var mockLogger = new Mock<ILogger<DiscordProvider>>();
			await using var provider = new DiscordProvider(mockJobManager, Mock.Of<IAsyncDelayer>(), mockLogger.Object, Mock.Of<IAssemblyInformationProvider>(), Mock.Of<IOptionsMonitor<GeneralConfiguration>>(), testToken1, () => Array.Empty<string>());
			Assert.IsFalse(provider.Connected);
			await InvokeConnect(provider);
			Assert.IsTrue(provider.Connected);

			await provider.Disconnect(default);
			Assert.IsFalse(provider.Connected);
		}
	}
}
