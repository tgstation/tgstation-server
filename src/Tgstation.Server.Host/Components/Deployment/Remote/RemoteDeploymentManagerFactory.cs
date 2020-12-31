using Microsoft.Extensions.Logging;
using System;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;

namespace Tgstation.Server.Host.Components.Deployment.Remote
{
	/// <inheritdoc />
	sealed class RemoteDeploymentManagerFactory : IRemoteDeploymentManagerFactory
	{
		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="RemoteDeploymentManagerFactory"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="RemoteDeploymentManagerFactory"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="IGitRemoteFeaturesFactory"/> for the <see cref="RemoteDeploymentManagerFactory"/>.
		/// </summary>
		readonly IGitRemoteFeaturesFactory gitRemoteFeaturesFactory;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="RemoteDeploymentManagerFactory"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="RemoteDeploymentManagerFactory"/>.
		/// </summary>
		readonly ILogger<RemoteDeploymentManagerFactory> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteDeploymentManagerFactory"/> <see langword="class"/>.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="gitRemoteFeaturesFactory">The value of <see cref="gitRemoteFeaturesFactory"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public RemoteDeploymentManagerFactory(
			IDatabaseContextFactory databaseContextFactory,
			IGitHubClientFactory gitHubClientFactory,
			IGitRemoteFeaturesFactory gitRemoteFeaturesFactory,
			ILoggerFactory loggerFactory,
			ILogger<RemoteDeploymentManagerFactory> logger)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.gitRemoteFeaturesFactory = gitRemoteFeaturesFactory ?? throw new ArgumentNullException(nameof(gitRemoteFeaturesFactory));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public IRemoteDeploymentManager CreateRemoteDeploymentManager(Api.Models.Instance metadata, RemoteGitProvider remoteGitProvider)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			logger.LogTrace("Creating remote deployment manager for remote git provider {0}...", remoteGitProvider);
			return remoteGitProvider switch
			{
				RemoteGitProvider.GitHub => new GitHubRemoteDeploymentManager(
					databaseContextFactory,
					gitHubClientFactory,
					loggerFactory.CreateLogger<GitHubRemoteDeploymentManager>(),
					metadata),
				RemoteGitProvider.GitLab => new GitLabRemoteDeploymentManager(
					loggerFactory.CreateLogger<GitLabRemoteDeploymentManager>(),
					metadata),
				RemoteGitProvider.Unknown => new NoOpRemoteDeploymentManager(),
				_ => throw new InvalidOperationException($"Invalid RemoteGitProvider: {remoteGitProvider}!"),
			};
		}

		/// <inheritdoc />
		public IRemoteDeploymentManager CreateRemoteDeploymentManager(Api.Models.Instance metadata, Models.CompileJob compileJob)
		{
			if (compileJob == null)
				throw new ArgumentNullException(nameof(compileJob));

			RemoteGitProvider remoteGitProvider;

			// Pre 4.7.X
			if (compileJob.RepositoryOrigin == null)
				remoteGitProvider = RemoteGitProvider.Unknown;
			else
				remoteGitProvider = gitRemoteFeaturesFactory.ParseRemoteGitProviderFromOrigin(
					new Uri(
						compileJob.RepositoryOrigin));

			return CreateRemoteDeploymentManager(metadata, remoteGitProvider);
		}
	}
}
