using System;
using System.Threading;
using System.Threading.Tasks;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <inheritdoc />
	sealed class RepositoryManager : IRepositoryManager
	{
		/// <inheritdoc />
		public bool InUse => semaphore.CurrentCount == 0;

		/// <inheritdoc />
		public bool CloneInProgress { get; private set; }

		/// <summary>
		/// The <see cref="ILibGit2RepositoryFactory"/> for the <see cref="RepositoryManager"/>.
		/// </summary>
		readonly ILibGit2RepositoryFactory repositoryFactory;

		/// <summary>
		/// The <see cref="ILibGit2Commands"/> for the <see cref="RepositoryManager"/>.
		/// </summary>
		readonly ILibGit2Commands commands;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="RepositoryManager"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="RepositoryManager"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IPostWriteHandler"/> for the <see cref="RepositoryManager"/>.
		/// </summary>
		readonly IPostWriteHandler postWriteHandler;

		/// <summary>
		/// The <see cref="IGitRemoteFeaturesFactory"/> for the <see cref="RepositoryManager"/>.
		/// </summary>
		readonly IGitRemoteFeaturesFactory gitRemoteFeaturesFactory;

		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> of <see cref="GeneralConfiguration"/> for the <see cref="RepositoryManager"/>.
		/// </summary>
		readonly IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions;

		/// <summary>
		/// The <see cref="ILogger"/> created <see cref="Repository"/>s.
		/// </summary>
		readonly ILogger<Repository> repositoryLogger;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="RepositoryManager"/>.
		/// </summary>
		readonly ILogger<RepositoryManager> logger;

		/// <summary>
		/// Used for controlling single access to the <see cref="IRepository"/>.
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryManager"/> class.
		/// </summary>
		/// <param name="repositoryFactory">The value of <see cref="repositoryFactory"/>.</param>
		/// <param name="commands">The value of <see cref="commands"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="postWriteHandler">The value of <see cref="postWriteHandler"/>.</param>
		/// <param name="gitRemoteFeaturesFactory">The value of <see cref="gitRemoteFeaturesFactory"/>.</param>
		/// <param name="repositoryLogger">The value of <see cref="repositoryLogger"/>.</param>
		/// <param name="generalConfigurationOptions">The value of <see cref="generalConfigurationOptions"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public RepositoryManager(
			ILibGit2RepositoryFactory repositoryFactory,
			ILibGit2Commands commands,
			IIOManager ioManager,
			IEventConsumer eventConsumer,
			IPostWriteHandler postWriteHandler,
			IGitRemoteFeaturesFactory gitRemoteFeaturesFactory,
			IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions,
			ILogger<Repository> repositoryLogger,
			ILogger<RepositoryManager> logger)
		{
			this.repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
			this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.postWriteHandler = postWriteHandler ?? throw new ArgumentNullException(nameof(postWriteHandler));
			this.gitRemoteFeaturesFactory = gitRemoteFeaturesFactory ?? throw new ArgumentNullException(nameof(gitRemoteFeaturesFactory));
			this.repositoryLogger = repositoryLogger ?? throw new ArgumentNullException(nameof(repositoryLogger));
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			logger.LogTrace("Disposing...");
			semaphore.Dispose();
		}

		/// <inheritdoc />
		public async ValueTask<IRepository?> CloneRepository(
			Uri url,
			string? initialBranch,
			string? username,
			string? password,
			JobProgressReporter progressReporter,
			bool recurseSubmodules,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(url);
			lock (semaphore)
			{
				if (CloneInProgress)
					throw new JobException(ErrorCode.RepoCloning);
				CloneInProgress = true;
			}

			var repositoryPath = ioManager.ResolvePath();
			logger.LogInformation("Begin clone {url} to {path} (Branch: {initialBranch})", url, repositoryPath, initialBranch);

			try
			{
				using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken))
				{
					logger.LogTrace("Semaphore acquired for clone");
					if (!await ioManager.DirectoryExists(repositoryPath, cancellationToken))
						try
						{
							using var cloneProgressReporter = progressReporter.CreateSection(null, 0.75f);
							using var checkoutProgressReporter = progressReporter.CreateSection(null, 0.25f);
							var cloneOptions = new CloneOptions
							{
								RecurseSubmodules = recurseSubmodules,
								OnCheckoutProgress = (path, completed, remaining) =>
								{
									if (checkoutProgressReporter == null)
										return;

									var percentage = (double)completed / remaining;
									checkoutProgressReporter.ReportProgress(percentage);
								},
								BranchName = initialBranch,
							};

							cloneOptions.FetchOptions.Hydrate(
								logger,
								cloneProgressReporter,
								await repositoryFactory.GenerateCredentialsHandler(
									gitRemoteFeaturesFactory.CreateGitRemoteFeatures(url),
									username,
									password,
									cancellationToken),
								cancellationToken);

							await repositoryFactory.Clone(
								url,
								cloneOptions,
								repositoryPath,
								cancellationToken);
						}
						catch
						{
							try
							{
								logger.LogTrace("Deleting partially cloned repository...");

								// DCT: Cancellation token is for job, operation must run regardless
								await ioManager.DeleteDirectory(repositoryPath, CancellationToken.None);
							}
							catch (Exception innerException)
							{
								logger.LogError(innerException, "Error deleting partially cloned repository!");
							}

							throw;
						}
					else
					{
						logger.LogDebug("Repository exists, clone aborted!");
						return null;
					}
				}
			}
			finally
			{
				logger.LogTrace("Semaphore released after clone");
				CloneInProgress = false;
			}

			logger.LogInformation("Clone complete!");
			return await LoadRepository(cancellationToken);
		}

		/// <inheritdoc />
		public async ValueTask<IRepository?> LoadRepository(CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin LoadRepository...");
			lock (semaphore)
				if (CloneInProgress)
					throw new JobException(ErrorCode.RepoCloning);

			try
			{
				await semaphore.WaitAsync(cancellationToken);
				try
				{
					logger.LogTrace("Semaphore acquired for load");
					var libGit2Repo = await repositoryFactory.CreateFromPath(ioManager.ResolvePath(), cancellationToken);

					return new Repository(
						libGit2Repo,
						commands,
						ioManager,
						eventConsumer,
						repositoryFactory,
						postWriteHandler,
						gitRemoteFeaturesFactory,
						repositoryFactory,
						generalConfigurationOptions,
						repositoryLogger,
						() =>
						{
							logger.LogTrace("Releasing semaphore due to Repository disposal...");
							semaphore.Release();
						});
				}
				catch
				{
					logger.LogTrace("Releasing semaphore as load failed");
					semaphore.Release();
					throw;
				}
			}
			catch (RepositoryNotFoundException e)
			{
				logger.LogTrace(e, "Repository not found!");
				return null;
			}
		}

		/// <inheritdoc />
		public async ValueTask DeleteRepository(CancellationToken cancellationToken)
		{
			logger.LogInformation("Deleting repository...");
			try
			{
				using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken))
				{
					logger.LogTrace("Semaphore acquired, deleting Repository directory...");
					await ioManager.DeleteDirectory(ioManager.ResolvePath(), cancellationToken);
				}
			}
			finally
			{
				logger.LogTrace("Semaphore released after delete attempt");
			}
		}
	}
}
