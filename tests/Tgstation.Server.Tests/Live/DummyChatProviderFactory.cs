using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Moq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Live
{
	sealed class DummyChatProviderFactory : IProviderFactory
	{
		readonly IJobManager jobManager;
		readonly ICryptographySuite cryptographySuite;
		readonly ILoggerFactory loggerFactory;
		readonly ILogger<DummyChatProviderFactory> logger;

		readonly IReadOnlyList<ICommand> commands;

		readonly Dictionary<ChatProvider, Random> seededRng;

		public DummyChatProviderFactory(IJobManager jobManager, ICryptographySuite cryptographySuite, ILoggerFactory loggerFactory, ILogger<DummyChatProviderFactory> logger)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			var commandFactory = new CommandFactory(
				Mock.Of<IAssemblyInformationProvider>(),
				Mock.Of<IEngineManager>(),
				Mock.Of<IRepositoryManager>(),
				Mock.Of<IDatabaseContextFactory>(),
				Mock.Of<ILatestCompileJobProvider>(),
				new Host.Models.Instance());

			commandFactory.SetWatchdog(Mock.Of<IWatchdog>());
			commands = commandFactory.GenerateCommands();

			const bool UseUnseededProviderRng = false;

			var randomSeed = UseUnseededProviderRng
				? new Random().Next()
				: 22475;

			logger.LogInformation("Random seed: {randomSeed}", randomSeed);

			var baseRng = new Random(randomSeed);
			seededRng = new Dictionary<ChatProvider, Random>{
				{ ChatProvider.Irc, new Random(baseRng.Next()) },
				{ ChatProvider.Discord, new Random(baseRng.Next()) },
			}; // hope you get the reference
		}

		public IProvider CreateProvider(ChatBot settings)
		{
			logger.LogTrace("CreateProvider");
			ArgumentNullException.ThrowIfNull(settings);

			var provider = settings.Provider;
			switch (provider)
			{
				case ChatProvider.Irc:
				case ChatProvider.Discord:
					logger.LogTrace("Creating DummyChatProvider in place of requested {providerType}Provider", settings.Provider);

					// for RNG to work, chat bots need to get created in a certain order
					// the ChatTest creates one of each provider type
					return new DummyChatProvider(
						jobManager,
						loggerFactory.CreateLogger($"Dummy{settings.Provider}Provider"),
						settings,
						cryptographySuite,
						commands,
						new Random(seededRng[provider.Value].Next()));
				default:
					throw new InvalidOperationException($"Invalid ChatProvider: {provider}");
			}
		}
	}
}
