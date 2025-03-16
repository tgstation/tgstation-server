﻿using System;
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
		public override string TestMergeRefSpecFormatter => "merge-requests/{0}/head:{1}";

		/// <inheritdoc />
		public override string TestMergeLocalBranchNameFormatter => "merge-requests/{0}/headrefs/heads/{1}";

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
		protected override async ValueTask<Models.TestMerge> GetTestMergeImpl(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken)
		{
			await using var client = await GraphQLGitLabClientFactory.CreateClient(repositorySettings.AccessToken);
			try
			{
				var operationResult = await client.GraphQL.GetMergeRequest.ExecuteAsync(
					$"{RemoteRepositoryOwner}/{RemoteRepositoryName}",
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
					TargetCommitSha = parameters.TargetCommitSha,
					Url = $"https://gitlab.com/{RemoteRepositoryOwner}/{RemoteRepositoryName}/-/merge_requests/{parameters.Number}",
				};
			}
		}
	}
}
