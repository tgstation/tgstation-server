using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

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
		/// The <see cref="ILibGit2RepositoryFactory"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly ILibGit2RepositoryFactory repositoryFactory;

		/// <summary>
		/// The <see cref="ILibGit2Commands"/> for the <see cref="RepositoryManager"/>.
		/// </summary>
		readonly ILibGit2Commands commands;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="ILogger"/> created <see cref="Repository"/>s
		/// </summary>
		readonly ILogger<Repository> repositoryLogger;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly ILogger<RepositoryManager> logger;

		/// <summary>
		/// Used for controlling single access to the <see cref="IRepository"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// Construct a <see cref="RepositoryManager"/>
		/// </summary>
		/// <param name="repositoryFactory">The value of <see cref="repositoryFactory"/>.</param>
		/// <param name="commands">The value of <see cref="commands"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="repositoryLogger">The value of <see cref="repositoryLogger"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public RepositoryManager(
			ILibGit2RepositoryFactory repositoryFactory,
			ILibGit2Commands commands,
			IIOManager ioManager,
			IEventConsumer eventConsumer,
			ILogger<Repository> repositoryLogger,
			ILogger<RepositoryManager> logger)
		{
			this.repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
			this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.repositoryLogger = repositoryLogger ?? throw new ArgumentNullException(nameof(repositoryLogger));
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
		public async Task<IRepository> CloneRepository(Uri url, string initialBranch, string username, string password, Action<int> progressReporter, CancellationToken cancellationToken)
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));

			logger.LogInformation("Begin clone {0} (Branch: {1})", url, initialBranch);
			lock (semaphore)
			{
				if (CloneInProgress)
					throw new JobException(ErrorCode.RepoCloning);
				CloneInProgress = true;
			}

			try
			{
				using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				{
					logger.LogTrace("Semaphore acquired");
					var repositoryPath = ioManager.ResolvePath();
					if (!await ioManager.DirectoryExists(repositoryPath, cancellationToken).ConfigureAwait(false))
						try
						{
							var cloneOptions = new CloneOptions
							{
								OnProgress = (a) => !cancellationToken.IsCancellationRequested,
								OnTransferProgress = (a) =>
								{
									var percentage = 100 * (((float)a.IndexedObjects + a.ReceivedObjects) / (a.TotalObjects * 2));
									progressReporter((int)percentage);
									return !cancellationToken.IsCancellationRequested;
								},
								RecurseSubmodules = true,
								OnUpdateTips = (a, b, c) => !cancellationToken.IsCancellationRequested,
								RepositoryOperationStarting = (a) => !cancellationToken.IsCancellationRequested,
								BranchName = initialBranch,
								CredentialsProvider = repositoryFactory.GenerateCredentialsHandler(username, password)
							};

							await repositoryFactory.Clone(
								url,
								cloneOptions,
								repositoryPath,
								cancellationToken)
								.ConfigureAwait(false);
						}
						catch
						{
							try
							{
								logger.LogTrace("Deleting partially cloned repository...");
								await ioManager.DeleteDirectory(repositoryPath, default).ConfigureAwait(false);
							}
							catch (Exception e)
							{
								logger.LogDebug("Error deleting partially cloned repository! Exception: {0}", e);
							}

							throw;
						}
					else
					{
						logger.LogDebug("Repository exists, clone aborted!");
						return null;
					}
				}

				logger.LogInformation("Clone complete!");
			}
			finally
			{
				CloneInProgress = false;
			}

			return await LoadRepository(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<IRepository> LoadRepository(CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin LoadRepository...");
			lock (semaphore)
				if (CloneInProgress)
					throw new JobException(ErrorCode.RepoCloning);
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				try
				{
					var libGitRepo = await repositoryFactory.CreateFromPath(ioManager.ResolvePath(), cancellationToken).ConfigureAwait(false);
					return new Repository(
						libGitRepo,
						commands,
						ioManager,
						eventConsumer,
						repositoryFactory,
						repositoryLogger, () =>
					{
						logger.LogTrace("Releasing semaphore due to Repository disposal...");
						semaphore.Release();
					});
				}
				catch
				{
					semaphore.Release();
					throw;
				}
			}
			catch (RepositoryNotFoundException e)
			{
				logger.LogDebug("Repository not found!");
				logger.LogTrace("Exception: {0}", e);
				return null;
			}
		}

		/// <inheritdoc />
		public async Task DeleteRepository(CancellationToken cancellationToken)
		{
			logger.LogInformation("Deleting repository...");
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				logger.LogTrace("Semaphore acquired, deleting Repository directory...");
				await ioManager.DeleteDirectory(ioManager.ResolvePath(), cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
