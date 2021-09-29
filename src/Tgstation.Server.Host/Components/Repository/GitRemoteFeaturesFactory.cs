using System;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <inheritdoc />
	sealed class GitRemoteFeaturesFactory : IGitRemoteFeaturesFactory
	{
		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="GitRemoteFeaturesFactory"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="GitRemoteFeaturesFactory"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GitRemoteFeaturesFactory"/>.
		/// </summary>
		readonly ILogger<GitRemoteFeaturesFactory> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitRemoteFeaturesFactory"/> class.
		/// </summary>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public GitRemoteFeaturesFactory(
			IGitHubClientFactory gitHubClientFactory,
			ILoggerFactory loggerFactory,
			ILogger<GitRemoteFeaturesFactory> logger)
		{
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public IGitRemoteFeatures CreateGitRemoteFeatures(IRepository repository)
		{
			if (repository == null)
				throw new ArgumentNullException(nameof(repository));

			var primaryRemote = repository.Origin;
			var remoteGitProvider = ParseRemoteGitProviderFromOrigin(primaryRemote);
			return remoteGitProvider switch
			{
				RemoteGitProvider.GitHub => new GitHubRemoteFeatures(
					gitHubClientFactory,
					loggerFactory.CreateLogger<GitHubRemoteFeatures>(),
					primaryRemote),
				RemoteGitProvider.GitLab => new GitLabRemoteFeatures(
					loggerFactory.CreateLogger<GitLabRemoteFeatures>(),
					primaryRemote),
				_ => new DefaultGitRemoteFeatures(),
			};
		}

		/// <inheritdoc />
		public RemoteGitProvider? ParseRemoteGitProviderFromOrigin(Uri origin)
		{
			if (origin == null)
				throw new ArgumentNullException(nameof(origin));

			switch (origin.Host.ToUpperInvariant())
			{
				case "GITHUB.COM":
				case "WWW.GITHUB.COM":
				case "GIT.GITHUB.COM":
					return RemoteGitProvider.GitHub;
				case "GITLAB.COM":
				case "WWW.GITLAB.COM":
				case "GIT.GITLAB.COM":
					return RemoteGitProvider.GitLab;
				default:
					logger.LogTrace("Unknown git remote: {0}", origin);
					return null;
			}
		}
	}
}
