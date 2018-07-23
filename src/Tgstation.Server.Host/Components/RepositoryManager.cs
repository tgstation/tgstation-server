using LibGit2Sharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class RepositoryManager : IRepositoryManager, IDisposable
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IIOManager ioManager;

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
		public RepositoryManager(RepositorySettings repositorySettings, IIOManager ioManager)
		{
			this.repositorySettings = repositorySettings ?? throw new ArgumentNullException(nameof(repositorySettings));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
		public async Task<IRepository> CloneRepository(Uri url, string accessString, CancellationToken cancellationToken)
		{
			await ioManager.DeleteDirectory(".", cancellationToken).ConfigureAwait(false);

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
						RepositoryOperationStarting = (a) => !cancellationToken.IsCancellationRequested
					});
				}
				catch (UserCancelledException) { }
				cancellationToken.ThrowIfCancellationRequested();
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

			return await LoadRepository(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<IRepository> LoadRepository(CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			LibGit2Sharp.Repository repo = null;
			await Task.Factory.StartNew(() =>
			{
				repo = new LibGit2Sharp.Repository(ioManager.ResolvePath("."));
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
			var localSemaphore = semaphore;
			return new Repository(repo, ioManager, () =>
			{
				localSemaphore?.Release();
				localSemaphore = null;
			});
		}
	}
}
