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
		public async Task<IRepository> CloneRepository(Uri url, string initialBranch, string accessString, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
				if (!await ioManager.DirectoryExists(".", cancellationToken).ConfigureAwait(false))
				{
					await DeleteRepository(cancellationToken).ConfigureAwait(false);

					await Task.Factory.StartNew(() =>
					{
						string path = null;
						try
						{
							path = LibGit2Sharp.Repository.Clone(Repository.GenerateAuthUrl(url.ToString(), accessString), ioManager.ResolvePath("."), new CloneOptions
							{
								OnProgress = (a) => !cancellationToken.IsCancellationRequested,
								OnTransferProgress = (a) => !cancellationToken.IsCancellationRequested,
								RecurseSubmodules = true,
								OnUpdateTips = (a, b, c) => !cancellationToken.IsCancellationRequested,
								RepositoryOperationStarting = (a) => !cancellationToken.IsCancellationRequested,
								BranchName = initialBranch
							});
						}
						catch (UserCancelledException) { }
						cancellationToken.ThrowIfCancellationRequested();
					}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
				}
			return await LoadRepository(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<IRepository> LoadRepository(CancellationToken cancellationToken)
		{
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
				return null;
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
