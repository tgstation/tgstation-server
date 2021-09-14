using System;
using System.Threading;
using System.Threading.Tasks;

using GitLabApiClient;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// GitLab <see cref="IGitRemoteFeatures"/>.
	/// </summary>
	sealed class GitLabRemoteFeatures : GitRemoteFeaturesImpl
	{
		/// <summary>
		/// Url for main GitLab site.
		/// </summary>
		/// <remarks>Eventually we'll derive this from a repo's origin when someone request custom GitHub/GitLab installation support.</remarks>
		public const string GitLabUrl = "https://gitlab.com";

		/// <inheritdoc />
		public override string TestMergeRefSpecFormatter => "merge-requests/{0}/head:{1}";

		/// <inheritdoc />
		public override string TestMergeLocalBranchNameFormatter => "merge-requests/{0}/headrefs/heads/{1}";

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

			return new GitRemoteInformation(remoteRepositoryOwner, remoteRepositoryName, RemoteGitProvider.GitLab);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GitLabRemoteFeatures"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="GitRemoteFeaturesBase"/>.</param>
		/// <param name="remoteUrl">The remote repository <see cref="Uri"/>.</param>
		public GitLabRemoteFeatures(ILogger<GitLabRemoteFeatures> logger, Uri remoteUrl)
			: base(logger, ParseRemoteInformation(remoteUrl))
		{
		}

		/// <inheritdoc />
		protected override async Task<Models.TestMerge> GetTestMergeImpl(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken)
		{
			var client = repositorySettings.AccessToken != null
				? new GitLabClient(GitLabUrl, repositorySettings.AccessToken)
				: new GitLabClient(GitLabUrl);

			var mr = await client
				.MergeRequests
				.GetAsync($"{GitRemoteInformation.RepositoryOwner}/{GitRemoteInformation.RepositoryName}", parameters.Number)
				.WithToken(cancellationToken)
				.ConfigureAwait(false);

			var revisionToUse = parameters.TargetCommitSha == null
				|| mr.Sha.StartsWith(parameters.TargetCommitSha, StringComparison.OrdinalIgnoreCase)
				? mr.Sha
				: parameters.TargetCommitSha;

			return new Models.TestMerge
			{
				Author = mr.Author.Username,
				BodyAtMerge = mr.Description,
				TitleAtMerge = mr.Title,
				Comment = parameters.Comment,
				Number = parameters.Number,
				TargetCommitSha = revisionToUse,
				Url = mr.WebUrl,
			};
		}
	}
}
