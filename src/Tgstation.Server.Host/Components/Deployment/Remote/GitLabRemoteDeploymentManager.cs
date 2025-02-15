using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StrawberryShake;

using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Utils.GitLab.GraphQL;

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

			var newList = revisionInformation.ActiveTestMerges.Select(x => x.TestMerge).ToList();

			await using var client = await GraphQLGitLabClientFactory.CreateClient(repositorySettings.AccessToken);
			IOperationResult<IGetMergeRequestsResult> operationResult;
			try
			{
				operationResult = await client.GraphQL.GetMergeRequests.ExecuteAsync(
					$"{repository.RemoteRepositoryOwner}/{repository.RemoteRepositoryName}",
					revisionInformation.ActiveTestMerges.Select(revInfoTestMerge => revInfoTestMerge.TestMerge.Number.ToString(CultureInfo.InvariantCulture)).ToList(),
					cancellationToken);

				operationResult.EnsureNoErrors();
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Logger.LogWarning(ex, "Merge requests update check failed!");
				return newList;
			}

			var data = operationResult.Data?.Project?.MergeRequests?.Nodes;
			if (data == null)
			{
				Logger.LogWarning("GitLab MergeRequests check returned null!");
				return newList;
			}

			async ValueTask CheckRemoveMR(IGetMergeRequests_Project_MergeRequests_Nodes? mergeRequest)
			{
				if (mergeRequest == null)
				{
					Logger.LogWarning("GitLab MergeRequest node was null!");
					return;
				}

				if (mergeRequest.State != MergeRequestState.Merged)
					return;

				var mergeCommitSha = mergeRequest.MergeCommitSha;
				if (mergeCommitSha == null)
				{
					Logger.LogWarning("MergeRequest #{id} had no MergeCommitSha!", mergeRequest.Iid);
					return;
				}

				var closedAtStr = mergeRequest.ClosedAt;
				if (closedAtStr == null)
				{
					Logger.LogWarning("MergeRequest #{id} had no ClosedAt!", mergeRequest.Iid);
					return;
				}

				if (!DateTimeOffset.TryParseExact(closedAtStr, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset closedAt))
				{
					Logger.LogWarning("MergeRequest #{id} had invalid ClosedAt: {closedAt}", mergeRequest.Iid, closedAtStr);
					return;
				}

				if (!Int64.TryParse(mergeRequest.Iid, out long number))
				{
					Logger.LogWarning("MergeRequest #{id} is non-numeric!", mergeRequest.Iid);
					return;
				}

				// We don't just assume, actually check the repo contains the merge commit.
				if (await repository.CommittishIsParent(mergeCommitSha, cancellationToken))
					newList.Remove(
						newList.First(
							potential => potential.Number == number));
			}

			foreach (var mergeRequest in data)
				await CheckRemoveMR(mergeRequest);

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
			await using var client = await GraphQLGitLabClientFactory.CreateClient(repositorySettings.AccessToken);
			try
			{
				string headerStart = "<!-- test_merge_tgs_bot -->";
				string header = String.Format(CultureInfo.InvariantCulture, "{1}{0}## Test merge deployment history:{0}{0}", Environment.NewLine, headerStart);

				// Try to find an existing note
				var notesQueryResult = await client.GraphQL.GetMergeRequestNotes.ExecuteAsync(
					$"{remoteRepositoryOwner}/{remoteRepositoryName}",
					testMergeNumber.ToString(CultureInfo.InvariantCulture),
					cancellationToken);

				notesQueryResult.EnsureNoErrors();

				var mergeRequest = notesQueryResult.Data?.Project?.MergeRequest;
				if (mergeRequest == null)
				{
					Logger.LogWarning("GitLab GetMergeRequestNotes mergeRequest returned null!");
					return;
				}

				var comments = mergeRequest.Notes?.Nodes;
				IGetMergeRequestNotes_Project_MergeRequest_Notes_Nodes? existingComment = null;
				if (comments != null)
				{
					for (int i = comments.Count - 1; i > -1; i--)
					{
						var currentComment = comments[i];
						if (currentComment?.Author?.Username == repositorySettings.AccessUser && (currentComment?.Body?.StartsWith(headerStart) ?? false))
						{
							if (currentComment.Body.Length > 987856)
							{ // Limit should be 999,999 so we'll leave a 12,143 buffer
								break;
							}

							existingComment = currentComment;
							break;
						}
					}
				}

				// Either amend or create the note
				if (existingComment != null)
				{
					var noteModificationResult = await client.GraphQL.ModifyNote.ExecuteAsync(
						existingComment.Id,
						existingComment.Body + comment,
						cancellationToken);

					notesQueryResult.EnsureNoErrors();
				}
				else
				{
					var noteCreationResult = await client.GraphQL.CreateNote.ExecuteAsync(
						mergeRequest.Id,
						header + comment,
						cancellationToken);

					noteCreationResult.EnsureNoErrors();
				}
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Logger.LogWarning(ex, "Error posting GitLab comment!");
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
				? String.Empty
				: String.Format(
					CultureInfo.InvariantCulture,
					"{0}{0}##### Comment{0}{1}",
					Environment.NewLine,
					testMerge.Comment), // 3
			updated ? "Updated" : "Deployed", // 4
			Metadata.Name, // 5
			compileJob.RevisionInformation.OriginCommitSha, // 6
			compileJob.RevisionInformation.CommitSha, // 7
			compileJob.Job.StartedAt); // 8

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
			compileJob.Job.StartedAt); // 2
	}
}
