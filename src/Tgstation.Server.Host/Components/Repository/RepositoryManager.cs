using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

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
		/// The <see cref="IIOManager"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="ICredentialsProvider"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly ICredentialsProvider credentialsProvider;

		/// <summary>
		/// The <see cref="ILogger"/> created <see cref="Repository"/>s
		/// </summary>
		readonly ILogger<Repository> repositoryLogger;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly ILogger<RepositoryManager> logger;

		/// <summary>
		/// The <see cref="RepositorySettings"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly RepositorySettings repositorySettings;

		/// <summary>
		/// Used for controlling single access to the <see cref="IRepository"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// Construct a <see cref="RepositoryManager"/>
		/// </summary>
		/// <param name="repositorySettings">The value of <see cref="repositorySettings"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="credentialsProvider">The value of <see cref="credentialsProvider"/></param>
		/// <param name="repositoryLogger">The value of <see cref="repositoryLogger"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public RepositoryManager(RepositorySettings repositorySettings, IIOManager ioManager, IEventConsumer eventConsumer, ICredentialsProvider credentialsProvider, ILogger<Repository> repositoryLogger, ILogger<RepositoryManager> logger)
		{
			this.repositorySettings = repositorySettings ?? throw new ArgumentNullException(nameof(repositorySettings));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.credentialsProvider = credentialsProvider ?? throw new ArgumentNullException(nameof(credentialsProvider));
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
			lock (this)
			{
				if (CloneInProgress)
					throw new InvalidOperationException("The repository is already being cloned!");
				CloneInProgress = true;
			}

			try
			{
				using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				{
					logger.LogTrace("Semaphore acquired");
					if (!await ioManager.DirectoryExists(".", cancellationToken).ConfigureAwait(false))
						try
						{
							await Task.Factory.StartNew(() =>
							{
								string path = null;
								try
								{
									path = LibGit2Sharp.Repository.Clone(url.ToString(), ioManager.ResolvePath("."), new CloneOptions
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
										CredentialsProvider = credentialsProvider.GenerateHandler(username, password)
									});
								}
								catch (UserCancelledException) { }
								cancellationToken.ThrowIfCancellationRequested();
							}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
						}
						catch
						{
							try
							{
								logger.LogTrace("Deleting partially cloned repository...");
								await ioManager.DeleteDirectory(".", default).ConfigureAwait(false);
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
			lock (this)
				if (CloneInProgress)
					throw new InvalidOperationException("The repository is being cloned!");
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			LibGit2Sharp.Repository repo = null;
			await Task.Factory.StartNew(() =>
			{
				try
				{
					logger.LogTrace("Creating LibGit2Sharp.Repository...");
					repo = new LibGit2Sharp.Repository(ioManager.ResolvePath("."));
				}
				catch (RepositoryNotFoundException e)
				{
					logger.LogDebug("Repository not found!");
					logger.LogTrace("Exception: {0}", e);
				}
				catch
				{
					semaphore.Release();
					throw;
				}
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
			if (repo == null)
			{
				semaphore.Release();
				return null;
			}

			return new Repository(repo, ioManager, eventConsumer, credentialsProvider, repositoryLogger, () =>
			{
				logger.LogTrace("Releasing semaphore due to Repository disposal...");
				semaphore.Release();
			});
		}

		/// <inheritdoc />
		public async Task DeleteRepository(CancellationToken cancellationToken)
		{
			logger.LogInformation("Deleting repository...");
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				logger.LogTrace("Semaphore acquired, deleting Repository directory...");
				await ioManager.DeleteDirectory(".", cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
