using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Octokit;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// GitHub <see cref="IGitRemoteFeatures"/>.
	/// </summary>
	sealed class GitHubRemoteFeatures : GitRemoteFeaturesImpl
	{
		/// <inheritdoc />
		public override string TestMergeRefSpecFormatter => "pull/{0}/head:{1}";

		/// <inheritdoc />
		public override string TestMergeLocalBranchNameFormatter => "pull/{0}/headrefs/heads/{1}";

		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="GitHubRemoteFeatures"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// Parse a given <paramref name="remoteUrl"/> into <see cref="GitRemoteInformation"/>.
		/// </summary>
		/// <param name="remoteUrl">The remote repository <see cref="Uri"/>.</param>
		/// <returns>A <see cref="GitRemoteInformation"/> instance based on <paramref name="remoteUrl"/>.</returns>
		static GitRemoteInformation ParseRemoteInformation(Uri remoteUrl)
		{
			if (remoteUrl == null)
				throw new ArgumentNullException(nameof(remoteUrl));

			var remoteRepositoryOwner = remoteUrl.Segments[1].TrimEnd('/');
			var remoteRepositoryName = remoteUrl.Segments[2].TrimEnd('/');
			if (remoteRepositoryName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
				remoteRepositoryName = remoteRepositoryName[0..^4];

			return new GitRemoteInformation(remoteRepositoryOwner, remoteRepositoryName, RemoteGitProvider.GitHub);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubRemoteFeatures"/> class.
		/// </summary>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="GitRemoteFeaturesBase"/>.</param>
		/// <param name="remoteUrl">The remote repository <see cref="Uri"/>.</param>
		public GitHubRemoteFeatures(IGitHubClientFactory gitHubClientFactory, ILogger<GitHubRemoteFeatures> logger, Uri remoteUrl)
			: base(logger, ParseRemoteInformation(remoteUrl))
		{
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
		}

		/// <inheritdoc />
		protected override async Task<Models.TestMerge> GetTestMergeImpl(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken)
		{
			var gitHubClient = repositorySettings.AccessToken != null
				? gitHubClientFactory.CreateClient(repositorySettings.AccessToken)
				: gitHubClientFactory.CreateClient();

			PullRequest? pr = null;
			ApiException? exception = null;
			string? errorMessage = null;
			try
			{
				pr = await gitHubClient
					.PullRequest
					.Get(GitRemoteInformation.RepositoryOwner, GitRemoteInformation.RepositoryOwner, parameters.Number)
					.WithToken(cancellationToken)
					.ConfigureAwait(false);
			}
			catch (RateLimitExceededException ex)
			{
				// you look at your anonymous access and sigh
				errorMessage = "GITHUB API ERROR: RATE LIMITED";
				exception = ex;
			}
			catch (AuthorizationException ex)
			{
				errorMessage = "GITHUB API ERROR: BAD CREDENTIALS";
				exception = ex;
			}
			catch (NotFoundException ex)
			{
				// you look at your shithub and sigh
				errorMessage = "GITHUB API ERROR: PULL REQUEST NOT FOUND";
				exception = ex;
			}

			if (exception != null)
				Logger.LogWarning(exception, "Error retrieving pull request metadata!");

			var revisionToUse = parameters.TargetCommitSha == null
				|| pr?.Head.Sha.StartsWith(parameters.TargetCommitSha, StringComparison.OrdinalIgnoreCase) == true
				? pr?.Head.Sha
				: parameters.TargetCommitSha;

			var testMerge = new Models.TestMerge
			{
				Author = pr?.User.Login ?? errorMessage,
				BodyAtMerge = pr?.Body ?? errorMessage ?? String.Empty,
				TitleAtMerge = pr?.Title ?? errorMessage ?? String.Empty,
				Comment = parameters.Comment,
				Number = parameters.Number,
				TargetCommitSha = revisionToUse,
				Url = pr?.HtmlUrl ?? errorMessage,
			};

			return testMerge;
		}
	}
}
