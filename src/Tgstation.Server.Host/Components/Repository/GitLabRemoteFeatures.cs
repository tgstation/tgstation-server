using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StrawberryShake;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Utils.GitLab.GraphQL;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// GitLab <see cref="IGitRemoteFeatures"/>.
	/// </summary>
	sealed class GitLabRemoteFeatures : GitRemoteFeaturesBase
	{
		/// <summary>
		/// Url for main GitLab site.
		/// </summary>
		/// <remarks>Eventually we'll derive this from a repo's origin when someone request custom GitHub/GitLab installation support.</remarks>
		public const string GitLabUrl = "https://gitlab.com";

		/// <inheritdoc />
		public override string GetTestMergeRefSpec(TestMergeParameters parameters)
		{
			ArgumentNullException.ThrowIfNull(parameters);
			return String.Format(CultureInfo.InvariantCulture, "merge-requests/{0}/head", parameters.Number);
		}

		/// <inheritdoc />
		public override string GetTestMergeLocalBranchName(TestMergeParameters parameters)
		{
			ArgumentNullException.ThrowIfNull(parameters);
			return String.Format(CultureInfo.InvariantCulture, "merge-requests/{0}/head", parameters.Number);
		}

		/// <inheritdoc />
		public override RemoteGitProvider? RemoteGitProvider => Api.Models.RemoteGitProvider.GitLab;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitLabRemoteFeatures"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="GitRemoteFeaturesBase"/>.</param>
		/// <param name="remoteUrl">The remote repository <see cref="Uri"/>.</param>
		public GitLabRemoteFeatures(ILogger<GitLabRemoteFeatures> logger, Uri remoteUrl)
			: base(logger, remoteUrl)
		{
		}

		/// <inheritdoc />
		public override ValueTask<string?> TransformRepositoryPassword(string? rawPassword, CancellationToken cancellationToken)
			=> ValueTask.FromResult(rawPassword);

		/// <inheritdoc />
		protected override async ValueTask<Models.TestMerge> GetTestMergeImpl(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken)
		{
			await using var client = await GraphQLGitLabClientFactory.CreateClient(repositorySettings.AccessToken);
			var (owner, name) = GetRepositoryOwnerAndName(parameters);
			try
			{
				var operationResult = await client.GraphQL.GetMergeRequest.ExecuteAsync(
					$"{owner}/{name}",
					parameters.Number.ToString(CultureInfo.InvariantCulture),
					cancellationToken);

				operationResult.EnsureNoErrors();
				var mr = operationResult.Data?.Project?.MergeRequest ?? throw new InvalidOperationException("GitLab MergeRequest check returned null!");

				return new Models.TestMerge
				{
					Author = mr.Author?.Username,
					BodyAtMerge = mr.Description,
					TitleAtMerge = mr.Title,
					Comment = parameters.Comment,
					Number = parameters.Number,
					SourceRepository = parameters.SourceRepository,
					TargetCommitSha = mr.DiffHeadSha,
					Url = mr.WebUrl,
				};
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Error retrieving merge request metadata!");

				return new Models.TestMerge
				{
					Author = ex.Message,
					BodyAtMerge = ex.Message,
					TitleAtMerge = ex.Message,
					Comment = parameters.Comment,
					Number = parameters.Number,
					SourceRepository = parameters.SourceRepository,
					TargetCommitSha = parameters.TargetCommitSha,
					Url = $"https://gitlab.com/{owner}/{name}/-/merge_requests/{parameters.Number}",
				};
			}
		}
	}
}
