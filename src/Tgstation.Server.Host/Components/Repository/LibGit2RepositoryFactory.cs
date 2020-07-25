using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <inheritdoc />
	sealed class LibGit2RepositoryFactory : ILibGit2RepositoryFactory
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="LibGit2RepositoryFactory"/>.
		/// </summary>
		readonly ILogger<LibGit2RepositoryFactory> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="LibGit2RepositoryFactory"/> <see langword="class"/>.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public LibGit2RepositoryFactory(ILogger<LibGit2RepositoryFactory> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public LibGit2Sharp.IRepository CreateInMemory()
		{
			logger.LogTrace("Creating in-memory libgit2 repository...");
			return new LibGit2Sharp.Repository();
		}

		/// <inheritdoc />
		public Task<LibGit2Sharp.IRepository> CreateFromPath(string path, CancellationToken cancellationToken)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			return Task.Factory.StartNew<LibGit2Sharp.IRepository>(
				() =>
				{
					logger.LogTrace("Creating libgit2 repostory at {0}...", path);
					return new LibGit2Sharp.Repository(path);
				},
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
				logger.LogTrace(ex, "Suppressing clone cancellation exception");
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

			if (supportsUserPass)
				throw new JobException(ErrorCode.RepoCredentialsRequired);

			throw new JobException(ErrorCode.RepoCannotAuthenticate);
		};
	}
}
