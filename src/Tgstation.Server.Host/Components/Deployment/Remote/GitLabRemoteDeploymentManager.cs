using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GitLabApiClient;
using GitLabApiClient.Models.MergeRequests.Responses;
using GitLabApiClient.Models.Notes.Requests;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment.Remote
{
	/// <summary>
	/// <see cref="IRemoteDeploymentManager"/> for GitLab.com.
	/// </summary>
	sealed class GitLabRemoteDeploymentManager : BaseRemoteDeploymentManager
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GitLabRemoteDeploymentManager"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		/// <param name="metadata">The <see cref="Api.Models.Instance"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		/// <param name="activationCallbacks">The activation callback <see cref="ConcurrentDictionary{TKey, TValue}"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		public GitLabRemoteDeploymentManager(
			ILogger<GitLabRemoteDeploymentManager> logger,
			Api.Models.Instance metadata,
			ConcurrentDictionary<long, Action<bool>> activationCallbacks)
			: base(logger, metadata, activationCallbacks)
		{
		}

		/// <inheritdoc />
		public override async ValueTask<IReadOnlyCollection<TestMerge>> RemoveMergedTestMerges(
			IRepository repository,
			RepositorySettings repositorySettings,
			RevisionInformation revisionInformation,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(repository);
			ArgumentNullException.ThrowIfNull(repositorySettings);
			ArgumentNullException.ThrowIfNull(revisionInformation);

			if ((revisionInformation.ActiveTestMerges?.Count > 0) != true)
			{
				Logger.LogTrace("No test merges to remove.");
				return Array.Empty<TestMerge>();
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
					.WaitAsync(cancellationToken));
			try
			{
				await Task.WhenAll(tasks);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Logger.LogWarning(ex, "Merge requests update check failed!");
			}

			var newList = revisionInformation.ActiveTestMerges.Select(x => x.TestMerge).ToList();

			MergeRequest? lastMerged = null;
			async ValueTask CheckRemoveMR(Task<MergeRequest> task)
			{
				var mergeRequest = await task;
				if (mergeRequest.State != MergeRequestState.Merged)
					return;

				// We don't just assume, actually check the repo contains the merge commit.
				if (await repository.CommittishIsParent(mergeRequest.MergeCommitSha, cancellationToken))
				{
					if (lastMerged == null || lastMerged.ClosedAt < mergeRequest.ClosedAt)
						lastMerged = mergeRequest;
					newList.Remove(
						newList.First(
							potential => potential.Number == mergeRequest.Id));
				}
			}

			foreach (var prTask in tasks)
				await CheckRemoveMR(prTask);

			return newList;
		}

		/// <inheritdoc />
		public override ValueTask FailDeployment(
			CompileJob compileJob,
			string errorMessage,
			CancellationToken cancellationToken) => ValueTask.CompletedTask;

		/// <inheritdoc />
		public override ValueTask StartDeployment(
			Api.Models.Internal.IGitRemoteInformation remoteInformation,
			CompileJob compileJob,
			CancellationToken cancellationToken) => ValueTask.CompletedTask;

		/// <inheritdoc />
		protected override ValueTask ApplyDeploymentImpl(
			CompileJob compileJob,
			CancellationToken cancellationToken) => ValueTask.CompletedTask;

		/// <inheritdoc />
		protected override ValueTask StageDeploymentImpl(CompileJob compileJob, CancellationToken cancellationToken) => ValueTask.CompletedTask;

		/// <inheritdoc />
		protected override ValueTask MarkInactiveImpl(CompileJob compileJob, CancellationToken cancellationToken) => ValueTask.CompletedTask;

		/// <inheritdoc />
		protected override async ValueTask CommentOnTestMergeSource(
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

			try
			{
				await client
					.MergeRequests
					.CreateNoteAsync(
						$"{remoteRepositoryOwner}/{remoteRepositoryName}",
						testMergeNumber,
						new CreateMergeRequestNoteRequest(comment))
					.WaitAsync(cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Logger.LogWarning(ex, "Error posting GitHub comment!");
			}
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
			"<details><summary>Test Merge {4} @ {8}</summary>{0}{0}##### Server Instance{0}{5}{1}{0}{0}##### Revision{0}Origin: {6}{0}Merge Request: {2}{0}Server: {7}{3}</details>{0}",
			Environment.NewLine, // 0
			repositorySettings.ShowTestMergeCommitters!.Value
				? String.Format(
					CultureInfo.InvariantCulture,
					"{0}{0}##### Merged By{0}{1}",
					Environment.NewLine,
					testMerge.MergedBy!.Name)
				: String.Empty, // 1
			testMerge.TargetCommitSha, // 2
			String.IsNullOrEmpty(testMerge.Comment)
				? String.Format(
					CultureInfo.InvariantCulture,
					"{0}{0}##### Comment{0}{1}",
					Environment.NewLine,
					testMerge.Comment)
				: String.Empty, // 3
			updated ? "Updated" : "Deployed", // 4
			Metadata.Name, // 5
			compileJob.RevisionInformation.OriginCommitSha, // 6
			compileJob.RevisionInformation.CommitSha, // 7
			compileJob.Job.StoppedAt); // 8

		/// <inheritdoc />
		protected override string FormatTestMergeRemoval(
			RepositorySettings repositorySettings,
			CompileJob compileJob,
			TestMerge testMerge,
			string remoteRepositoryOwner,
			string remoteRepositoryName) => String.Format(
			CultureInfo.InvariantCulture,
			"<details><summary>Test Merge Removed @ {2}:</summary>{0}{0}##### Server Instance{0}{1}{0}</details>{0}",
			Environment.NewLine, // 0
			Metadata.Name, // 1
			compileJob.Job.StoppedAt); // 2
	}
}
