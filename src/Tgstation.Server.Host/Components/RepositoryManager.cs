using LibGit2Sharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class RepositoryManager : IRepositoryManager
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// Construct a <see cref="RepositoryManager"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		public RepositoryManager(IIOManager ioManager) => this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));

		/// <inheritdoc />
		public async Task<IRepository> CloneRepository(string url, string accessString, CancellationToken cancellationToken)
		{
			await ioManager.DeleteDirectory(".", cancellationToken).ConfigureAwait(false);

			await Task.Factory.StartNew(() =>
			{
				string path = null;
				try
				{
					path = LibGit2Sharp.Repository.Clone(Repository.GenerateAuthUrl(url, accessString), ioManager.ResolvePath("."), new CloneOptions
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
		public Task<IRepository> LoadRepository(CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			var repo = new LibGit2Sharp.Repository(ioManager.ResolvePath("."));
			return (IRepository)new Repository(repo, ioManager);
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
	}
}
