using LibGit2Sharp;
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
		public RepositoryManager(RepositorySettings repositorySettings, IIOManager ioManager, IEventConsumer eventConsumer)
		{
			this.repositorySettings = repositorySettings ?? throw new ArgumentNullException(nameof(repositorySettings));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
		public async Task<IRepository> CloneRepository(Uri url, string initialBranch, string username, string password, Action<int> progressReporter, CancellationToken cancellationToken)
		{
			lock (this)
			{
				if (CloneInProgress)
					throw new InvalidOperationException("The repository is already being cloned!");
				CloneInProgress = true;
			}
			try
			{
				using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
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
										CredentialsProvider = (a, b, c) => username != null ? (Credentials)new UsernamePasswordCredentials
										{
											Username = username,
											Password = password
										} : new DefaultCredentials()
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
								await ioManager.DeleteDirectory(".", default).ConfigureAwait(false);
							}
							catch { }
							throw;
						}
					else
						return null;
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
			lock (this)
				if (CloneInProgress)
					throw new InvalidOperationException("The repository is being cloned!");
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			LibGit2Sharp.Repository repo = null;
			await Task.Factory.StartNew(() =>
			{
				try
				{
					repo = new LibGit2Sharp.Repository(ioManager.ResolvePath("."));
				}
				catch (RepositoryNotFoundException) { }
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
			if (repo == null)
			{
				semaphore.Release();
				return null;
			}
			var localSemaphore = semaphore;
			return new Repository(repo, ioManager, eventConsumer, () =>
			{
				localSemaphore?.Release();
				localSemaphore = null;
			});
		}

		/// <inheritdoc />
		public async Task DeleteRepository(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				await ioManager.DeleteDirectory(".", cancellationToken).ConfigureAwait(false);
		}
	}
}
