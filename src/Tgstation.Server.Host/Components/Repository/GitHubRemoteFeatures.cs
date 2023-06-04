﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Octokit;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// GitHub <see cref="IGitRemoteFeatures"/>.
	/// </summary>
	sealed class GitHubRemoteFeatures : GitRemoteFeaturesBase
	{
		/// <inheritdoc />
		public override string TestMergeRefSpecFormatter => "pull/{0}/head:{1}";

		/// <inheritdoc />
		public override string TestMergeLocalBranchNameFormatter => "pull/{0}/headrefs/heads/{1}";

		/// <inheritdoc />
		public override RemoteGitProvider? RemoteGitProvider => Api.Models.RemoteGitProvider.GitHub;

		/// <inheritdoc />
		public override string RemoteRepositoryOwner { get; }

		/// <inheritdoc />
		public override string RemoteRepositoryName { get; }

		/// <summary>
		/// The <see cref="IGitHubServiceFactory"/> for the <see cref="GitHubRemoteFeatures"/>.
		/// </summary>
		readonly IGitHubServiceFactory gitHubServiceFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubRemoteFeatures"/> class.
		/// </summary>
		/// <param name="gitHubServiceFactory">The value of <see cref="gitHubServiceFactory"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="GitRemoteFeaturesBase"/>.</param>
		/// <param name="remoteUrl">The remote repository <see cref="Uri"/>.</param>
		public GitHubRemoteFeatures(IGitHubServiceFactory gitHubServiceFactory, ILogger<GitHubRemoteFeatures> logger, Uri remoteUrl)
			: base(logger, remoteUrl)
		{
			this.gitHubServiceFactory = gitHubServiceFactory ?? throw new ArgumentNullException(nameof(gitHubServiceFactory));

			if (remoteUrl == null)
				throw new ArgumentNullException(nameof(remoteUrl));

			RemoteRepositoryOwner = remoteUrl.Segments[1].TrimEnd('/');
			RemoteRepositoryName = remoteUrl.Segments[2].TrimEnd('/');
			if (RemoteRepositoryName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
				RemoteRepositoryName = RemoteRepositoryName[0..^4];
		}

		/// <inheritdoc />
		protected override async Task<Models.TestMerge> GetTestMergeImpl(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken)
		{
			var gitHubService = repositorySettings.AccessToken != null
				? gitHubServiceFactory.CreateService(repositorySettings.AccessToken)
				: gitHubServiceFactory.CreateService();

			PullRequest pr = null;
			ApiException exception = null;
			string errorMessage = null;
			try
			{
				pr = await gitHubService.GetPullRequest(RemoteRepositoryOwner, RemoteRepositoryName, parameters.Number, cancellationToken);
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
