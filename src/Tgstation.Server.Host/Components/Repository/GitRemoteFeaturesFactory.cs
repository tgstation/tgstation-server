using System;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <inheritdoc />
	sealed class GitRemoteFeaturesFactory : IGitRemoteFeaturesFactory
	{
		/// <summary>
		/// The <see cref="IGitHubServiceFactory"/> for the <see cref="GitRemoteFeaturesFactory"/>.
		/// </summary>
		readonly IGitHubServiceFactory gitHubServiceFactory;

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
		/// <param name="gitHubServiceFactory">The value of <see cref="gitHubServiceFactory"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public GitRemoteFeaturesFactory(
			IGitHubServiceFactory gitHubServiceFactory,
			ILoggerFactory loggerFactory,
			ILogger<GitRemoteFeaturesFactory> logger)
		{
			this.gitHubServiceFactory = gitHubServiceFactory ?? throw new ArgumentNullException(nameof(gitHubServiceFactory));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public IGitRemoteFeatures CreateGitRemoteFeatures(IRepository repository)
		{
			ArgumentNullException.ThrowIfNull(repository);
			return CreateGitRemoteFeatures(repository.Origin);
		}

		/// <inheritdoc />
		public IGitRemoteFeatures CreateGitRemoteFeatures(Uri origin)
		{
			ArgumentNullException.ThrowIfNull(origin);

			var remoteGitProvider = ParseRemoteGitProviderFromOrigin(origin);
			return remoteGitProvider switch
			{
				RemoteGitProvider.GitHub => new GitHubRemoteFeatures(
					gitHubServiceFactory,
					loggerFactory.CreateLogger<GitHubRemoteFeatures>(),
					origin),
				RemoteGitProvider.GitLab => new GitLabRemoteFeatures(
					loggerFactory.CreateLogger<GitLabRemoteFeatures>(),
					origin),
				RemoteGitProvider.Unknown => new DefaultGitRemoteFeatures(),
				_ => throw new InvalidOperationException($"Unknown RemoteGitProvider: {remoteGitProvider}!"),
			};
		}

		/// <inheritdoc />
		public RemoteGitProvider ParseRemoteGitProviderFromOrigin(Uri origin)
		{
			ArgumentNullException.ThrowIfNull(origin);

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
					logger.LogDebug("Unknown git remote: {origin}", origin);
					return RemoteGitProvider.Unknown;
			}
		}
	}
}
