using GitLabApiClient;
using GitLabApiClient.Models.MergeRequests.Responses;
using GitLabApiClient.Models.Notes.Requests;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment.Remote
{
	/// <summary>
	/// <see cref="IRemoteDeploymentManager"/> for GitLab.com
	/// </summary>
	sealed class GitLabRemoteDeploymentManager : BaseRemoteDeploymentManager
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GitLabRemoteDeploymentManager"/> <see langword="class"/>.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		/// <param name="metadata">The <see cref="Api.Models.Instance"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		public GitLabRemoteDeploymentManager(ILogger<GitLabRemoteDeploymentManager> logger, Api.Models.Instance metadata)
			: base(logger, metadata)
		{
		}

		/// <inheritdoc />
		protected override Task CommentOnTestMergeSource(
			RepositorySettings repositorySettings,
			string remoteRepositoryOwner,
			string remoteRepositoryName,
			string comment,
			int testMergeNumber,
			CancellationToken cancellationToken)
		{
			var client = repositorySettings.AccessToken != null
				? new GitLabClient(GitLabRemoteFeatures.GitLabUrl, repositorySettings.AccessToken)
				: new GitLabClient(GitLabRemoteFeatures.GitLabUrl);

			return client
				.MergeRequests
				.CreateNoteAsync(
					$"{remoteRepositoryOwner}/{remoteRepositoryName}",
					testMergeNumber,
					new CreateMergeRequestNoteRequest(comment))
				.WithToken(cancellationToken);
		}

		/// <inheritdoc />
		protected override string FormatTestMerge(
			RepositorySettings repositorySettings,
			CompileJob compileJob,
			TestMerge testMerge,
			string remoteRepositoryOwner,
			string remoteRepositoryName,
			bool updated) => String.Format(
			CultureInfo.InvariantCulture,
			"#### Test Merge {4}{0}{0}##### Server Instance{0}{5}{1}{0}{0}##### Revision{0}Origin: {6}{0}Merge Request: {2}{0}Server: {7}{3}",
			Environment.NewLine,
			repositorySettings.ShowTestMergeCommitters.Value
				? String.Format(
					CultureInfo.InvariantCulture,
					"{0}{0}##### Merged By{0}{1}",
					Environment.NewLine,
					testMerge.MergedBy.Name)
				: String.Empty,
			testMerge.TargetCommitSha,
			testMerge.Comment != null
				? String.Format(
					CultureInfo.InvariantCulture,
					"{0}{0}##### Comment{0}{1}",
					Environment.NewLine,
					testMerge.Comment)
				: String.Empty,
			updated ? "Updated" : "Deployed",
			Metadata.Name,
			compileJob.RevisionInformation.OriginCommitSha,
			compileJob.RevisionInformation.CommitSha);

		/// <inheritdoc />
		public override async Task<IReadOnlyCollection<RevInfoTestMerge>> RemoveMergedTestMerges(
			IRepository repository,
			RepositorySettings repositorySettings,
			RevisionInformation revisionInformation,
			CancellationToken cancellationToken)
		{
			if (repository == null)
				throw new ArgumentNullException(nameof(repository));
			if (repositorySettings == null)
				throw new ArgumentNullException(nameof(repositorySettings));
			if (revisionInformation == null)
				throw new ArgumentNullException(nameof(revisionInformation));

			if (revisionInformation.ActiveTestMerges?.Any() != true)
			{
				Logger.LogTrace("No test merges to remove.");
				return Array.Empty<RevInfoTestMerge>();
			}

			var client = repositorySettings.AccessToken != null
				? new GitLabClient(GitLabRemoteFeatures.GitLabUrl, repositorySettings.AccessToken)
				: new GitLabClient(GitLabRemoteFeatures.GitLabUrl);

			var tasks = revisionInformation
				.ActiveTestMerges
				.Select(x => client
					.MergeRequests
					.GetAsync(
						$"{repository.RemoteRepositoryOwner}/{repository.RemoteRepositoryName}",
						x.TestMerge.Number)
					.WithToken(cancellationToken));
			try
			{
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}
			catch (Exception ex) when (!(ex is OperationCanceledException))
			{
				Logger.LogWarning(ex, "Merge requests update check failed!");
			}

			var newList = revisionInformation.ActiveTestMerges.ToList();

			MergeRequest lastMerged = null;
			async Task CheckRemoveMR(Task<MergeRequest> task)
			{
				var mergeRequest = await task.ConfigureAwait(false);
				if (mergeRequest.State != MergeRequestState.Merged)
					return;

				// We don't just assume, actually check the repo contains the merge commit.
				if (await repository.ShaIsParent(mergeRequest.MergeCommitSha, cancellationToken).ConfigureAwait(false))
				{
					if (lastMerged == null || lastMerged.ClosedAt < mergeRequest.ClosedAt)
						lastMerged = mergeRequest;
					newList.Remove(
						newList.First(
							potential => potential.TestMerge.Number == mergeRequest.Id));
				}
			}

			foreach (var prTask in tasks)
				await CheckRemoveMR(prTask).ConfigureAwait(false);

			return newList;
		}

		/// <inheritdoc />
		public override Task ApplyDeployment(
			CompileJob compileJob,
			CompileJob oldCompileJob, CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public override Task FailDeployment(
			CompileJob compileJob,
			string errorMessage,
			CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public override Task MarkInactive(CompileJob compileJob, CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public override Task StageDeployment(CompileJob compileJob, CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public override Task StartDeployment(
			Api.Models.Internal.IGitRemoteInformation remoteInformation,
			CompileJob compileJob,
			CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
