using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Utils.GitHub;

#nullable disable

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
		readonly IGitHubServiceFactory gitHubServiceFactory;

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
		/// A map of <see cref="Models.CompileJob"/> <see cref="EntityId.Id"/>s to activation callback <see cref="Action{T1}"/>s.
		/// </summary>
		readonly ConcurrentDictionary<long, Action<bool>> activationCallbacks;

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteDeploymentManagerFactory"/> class.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="gitHubServiceFactory">The value of <see cref="gitHubServiceFactory"/>.</param>
		/// <param name="gitRemoteFeaturesFactory">The value of <see cref="gitRemoteFeaturesFactory"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public RemoteDeploymentManagerFactory(
			IDatabaseContextFactory databaseContextFactory,
			IGitHubServiceFactory gitHubServiceFactory,
			IGitRemoteFeaturesFactory gitRemoteFeaturesFactory,
			ILoggerFactory loggerFactory,
			ILogger<RemoteDeploymentManagerFactory> logger)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.gitHubServiceFactory = gitHubServiceFactory ?? throw new ArgumentNullException(nameof(gitHubServiceFactory));
			this.gitRemoteFeaturesFactory = gitRemoteFeaturesFactory ?? throw new ArgumentNullException(nameof(gitRemoteFeaturesFactory));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			activationCallbacks = new ConcurrentDictionary<long, Action<bool>>();
		}

		/// <inheritdoc />
		public IRemoteDeploymentManager CreateRemoteDeploymentManager(Api.Models.Instance metadata, RemoteGitProvider remoteGitProvider)
		{
			ArgumentNullException.ThrowIfNull(metadata);

			logger.LogTrace("Creating remote deployment manager for remote git provider {remoteGitProvider}...", remoteGitProvider);
			return remoteGitProvider switch
			{
				RemoteGitProvider.GitHub => new GitHubRemoteDeploymentManager(
					databaseContextFactory,
					gitHubServiceFactory,
					loggerFactory.CreateLogger<GitHubRemoteDeploymentManager>(),
					metadata,
					activationCallbacks),
				RemoteGitProvider.GitLab => new GitLabRemoteDeploymentManager(
					loggerFactory.CreateLogger<GitLabRemoteDeploymentManager>(),
					metadata,
					activationCallbacks),
				RemoteGitProvider.Unknown => new NoOpRemoteDeploymentManager(
					loggerFactory.CreateLogger<NoOpRemoteDeploymentManager>(),
					metadata,
					activationCallbacks),
				_ => throw new InvalidOperationException($"Invalid RemoteGitProvider: {remoteGitProvider}!"),
			};
		}

		/// <inheritdoc />
		public IRemoteDeploymentManager CreateRemoteDeploymentManager(Api.Models.Instance metadata, Models.CompileJob compileJob)
		{
			ArgumentNullException.ThrowIfNull(compileJob);

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

		/// <inheritdoc />
		public void ForgetLocalStateForCompileJobs(IEnumerable<long> compileJobIds)
		{
			ArgumentNullException.ThrowIfNull(compileJobIds);

			foreach (var compileJobId in compileJobIds)
				activationCallbacks.Remove(compileJobId, out _);
		}
	}
}
