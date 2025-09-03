using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <inheritdoc />
	sealed class RepostoryManagerFactory : IRepositoryManagerFactory
	{
		/// <summary>
		/// The <see cref="ILibGit2RepositoryFactory"/> for the <see cref="RepostoryManagerFactory"/>.
		/// </summary>
		readonly ILibGit2RepositoryFactory repositoryFactory;

		/// <summary>
		/// The <see cref="ILibGit2Commands"/> for the <see cref="RepostoryManagerFactory"/>.
		/// </summary>
		readonly ILibGit2Commands repositoryCommands;

		/// <summary>
		/// The <see cref="IPostWriteHandler"/> for the <see cref="RepostoryManagerFactory"/>.
		/// </summary>
		readonly IPostWriteHandler postWriteHandler;

		/// <summary>
		/// The <see cref="IGitRemoteFeaturesFactory"/> for the <see cref="RepostoryManagerFactory"/>.
		/// </summary>
		readonly IGitRemoteFeaturesFactory gitRemoteFeaturesFactory;

		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> of <see cref="GeneralConfiguration"/> for the <see cref="RepostoryManagerFactory"/>.
		/// </summary>
		readonly IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="RepostoryManagerFactory"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="RepostoryManagerFactory"/> class.
		/// </summary>
		/// <param name="repositoryFactory">The value of <see cref="repositoryFactory"/>.</param>
		/// <param name="repositoryCommands">The value of <see cref="repositoryCommands"/>.</param>
		/// <param name="postWriteHandler">The value of <see cref="postWriteHandler"/>.</param>
		/// <param name="gitRemoteFeaturesFactory">The value of <see cref="gitRemoteFeaturesFactory"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="generalConfigurationOptions">The value of <see cref="generalConfigurationOptions"/>.</param>
		public RepostoryManagerFactory(
			ILibGit2RepositoryFactory repositoryFactory,
			ILibGit2Commands repositoryCommands,
			IPostWriteHandler postWriteHandler,
			IGitRemoteFeaturesFactory gitRemoteFeaturesFactory,
			ILoggerFactory loggerFactory,
			IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions)
		{
			this.repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
			this.repositoryCommands = repositoryCommands ?? throw new ArgumentNullException(nameof(repositoryCommands));
			this.postWriteHandler = postWriteHandler ?? throw new ArgumentNullException(nameof(postWriteHandler));
			this.gitRemoteFeaturesFactory = gitRemoteFeaturesFactory ?? throw new ArgumentNullException(nameof(gitRemoteFeaturesFactory));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <inheritdoc />
		public IRepositoryManager CreateRepositoryManager(IIOManager ioManager, IEventConsumer eventConsumer)
			=> new RepositoryManager(
				repositoryFactory,
				repositoryCommands,
				ioManager,
				eventConsumer,
				postWriteHandler,
				gitRemoteFeaturesFactory,
				generalConfigurationOptions,
				loggerFactory.CreateLogger<Repository>(),
				loggerFactory.CreateLogger<RepositoryManager>());

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken)
		{
			CheckSystemCompatibility();
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <summary>
		/// Test that the <see cref="repositoryFactory"/> is functional.
		/// </summary>
		void CheckSystemCompatibility() => repositoryFactory.CreateInMemory();
	}
}
