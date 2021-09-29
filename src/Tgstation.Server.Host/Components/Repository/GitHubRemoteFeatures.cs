using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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

			var pr = await gitHubClient
				.PullRequest
				.Get(GitRemoteInformation.RepositoryOwner, GitRemoteInformation.RepositoryOwner, parameters.Number)
				.WithToken(cancellationToken)
				.ConfigureAwait(false);

			var revisionToUse = parameters.TargetCommitSha == null
				|| pr.Head.Sha.StartsWith(parameters.TargetCommitSha, StringComparison.OrdinalIgnoreCase) == true
				? pr.Head.Sha
				: parameters.TargetCommitSha;

			return new ()
			{
				Author = pr.User.Login,
				BodyAtMerge = pr.Body,
				TitleAtMerge = pr.Title,
				Comment = parameters.Comment,
				Number = parameters.Number,
				Url = pr?.HtmlUrl,
				TargetCommitSha = revisionToUse,
			};
		}
	}
}
