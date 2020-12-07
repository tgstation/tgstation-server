using Microsoft.Extensions.Logging;
using System;
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
		/// Initializes a new instance of the <see cref="GitRemoteFeaturesFactory"/> <see langword="class"/>.
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
			try
			{
				var primaryRemoteUrl = new Uri(primaryRemote);

				switch (primaryRemoteUrl.Host.ToUpperInvariant())
				{
					case "GITHUB.COM":
					case "WWW.GITHUB.COM":
					case "GIT.GITHUB.COM":
						return new GitHubRemoteFeatures(
							gitHubClientFactory,
							loggerFactory.CreateLogger<GitHubRemoteFeatures>(),
							primaryRemoteUrl);
					case "GITLAB.COM":
					case "WWW.GITLAB.COM":
					case "GIT.GITLAB.COM":
						return new GitLabRemoteFeatures(
							loggerFactory.CreateLogger<GitLabRemoteFeatures>(),
							primaryRemoteUrl);
					default:
						logger.LogTrace("Unknown git remote: {0}", primaryRemoteUrl);
						break;
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error parsing remote git provider.");
			}

			return new DefaultGitRemoteFeatures();
		}
	}
}
