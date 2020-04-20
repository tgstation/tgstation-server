using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <inheritdoc />
	sealed class RepositoryFactory : IRepositoryFactory
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="RepositoryFactory"/>.
		/// </summary>
		readonly ILogger<RepositoryFactory> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryFactory"/> <see langword="class"/>.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public RepositoryFactory(ILogger<RepositoryFactory> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public LibGit2Sharp.IRepository CreateInMemory()
		{
			logger.LogTrace("Creating in-memory repository...");
			return new LibGit2Sharp.Repository();
		}

		/// <inheritdoc />
		public Task<LibGit2Sharp.IRepository> CreateFromPath(string path, CancellationToken cancellationToken)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			logger.LogTrace("Creating repostory at {0}...", path);
			return Task.Factory.StartNew(
				() => (LibGit2Sharp.IRepository)new LibGit2Sharp.Repository(path),
				cancellationToken,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Current);
		}

		/// <inheritdoc />
		public Task Clone(Uri url, CloneOptions cloneOptions, string path, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			try
			{
				logger.LogTrace("Cloning {0} into {1}...", url, path);
				LibGit2Sharp.Repository.Clone(url.ToString(), path, cloneOptions);
			}
			catch (UserCancelledException ex)
			{
				logger.LogTrace("Suppressing clone cancellation exception: {0}", ex);
				cancellationToken.ThrowIfCancellationRequested();
			}
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

		/// <inheritdoc />
		public CredentialsHandler GenerateCredentialsHandler(string username, string password) => (a, b, supportedCredentialTypes) =>
		{
			var hasCreds = username != null;
			var supportsUserPass = supportedCredentialTypes.HasFlag(SupportedCredentialTypes.UsernamePassword);
			var supportsAnonymous = supportedCredentialTypes.HasFlag(SupportedCredentialTypes.Default);

			logger.LogTrace("Credentials requested. Present: {0}. Supports anonymous: {1}. Supports user/pass: {2}", hasCreds, supportsAnonymous, supportsUserPass);
			if (supportsUserPass && hasCreds)
				return new UsernamePasswordCredentials
				{
					Username = username,
					Password = password
				};

			if (supportsAnonymous)
				return new DefaultCredentials();

			if (!hasCreds)
				throw new JobException("Remote does not support anonymous authentication!");

			throw new JobException("Server does not support anonymous or username/password authentication!");
		};
	}
}
