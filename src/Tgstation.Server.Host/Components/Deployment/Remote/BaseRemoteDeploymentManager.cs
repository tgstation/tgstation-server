using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment.Remote
{
	/// <summary>
	/// Base class for implementing <see cref="IRemoteDeploymentManager"/>s.
	/// </summary>
	abstract class BaseRemoteDeploymentManager : IRemoteDeploymentManager
	{
		/// <summary>
		/// The header comment that begins every deployment message comment/note.
		/// </summary>
		public const string DeploymentMsgHeaderStart = "<!-- tgs_test_merge_comment -->";

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="BaseRemoteDeploymentManager"/>.
		/// </summary>
		protected Api.Models.Instance Metadata { get; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="BaseRemoteDeploymentManager"/>.
		/// </summary>
		protected ILogger<BaseRemoteDeploymentManager> Logger { get; }

		/// <summary>
		/// A map of <see cref="CompileJob"/> <see cref="Api.Models.EntityId.Id"/>s to activation callback <see cref="Action{T1}"/>s.
		/// </summary>
		readonly ConcurrentDictionary<long, Action<bool>> activationCallbacks;

		/// <summary>
		/// Initializes a new instance of the <see cref="BaseRemoteDeploymentManager"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="metadata">The value of <see cref="Metadata"/>.</param>
		/// <param name="activationCallbacks">The value of <see cref="activationCallbacks"/>.</param>
		protected BaseRemoteDeploymentManager(
			ILogger<BaseRemoteDeploymentManager> logger,
			Api.Models.Instance metadata,
			ConcurrentDictionary<long, Action<bool>> activationCallbacks)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			this.activationCallbacks = activationCallbacks ?? throw new ArgumentNullException(nameof(activationCallbacks));
		}

		/// <inheritdoc />
		public async ValueTask PostDeploymentComments(
			CompileJob compileJob,
			RevisionInformation? previousRevisionInformation,
			RepositorySettings repositorySettings,
			string? repoOwner,
			string? repoName,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(compileJob);
			ArgumentNullException.ThrowIfNull(repositorySettings);
			ArgumentNullException.ThrowIfNull(repoOwner);
			ArgumentNullException.ThrowIfNull(repoName);

			if (repositorySettings.AccessToken == null)
				return;

			var revisionInformation = compileJob.RevisionInformation;
			if ((previousRevisionInformation != null && previousRevisionInformation.CommitSha == revisionInformation.CommitSha)
				|| !repositorySettings.PostTestMergeComment!.Value)
				return;

			var previousTestMerges = (IEnumerable<RevInfoTestMerge>?)previousRevisionInformation?.ActiveTestMerges ?? Enumerable.Empty<RevInfoTestMerge>();
			var currentTestMerges = (IEnumerable<RevInfoTestMerge>?)revisionInformation.ActiveTestMerges ?? Enumerable.Empty<RevInfoTestMerge>();

			// determine what TMs were changed and how
			var addedTestMerges = currentTestMerges
				.Select(x => x.TestMerge)
				.Where(x => !previousTestMerges
					.Any(y => y.TestMerge.Number == x.Number && y.TestMerge.SourceRepository == x.SourceRepository))
				.ToList();
			var removedTestMerges = previousTestMerges
				.Select(x => x.TestMerge)
				.Where(x => !currentTestMerges
					.Any(y => y.TestMerge.Number == x.Number && y.TestMerge.SourceRepository == x.SourceRepository))
				.ToList();
			var updatedTestMerges = currentTestMerges
				.Select(x => x.TestMerge)
				.Where(x => previousTestMerges
					.Any(y => y.TestMerge.Number == x.Number && y.TestMerge.SourceRepository == x.SourceRepository))
				.ToList();

			if (addedTestMerges.Count == 0 && removedTestMerges.Count == 0 && updatedTestMerges.Count == 0)
				return;

			Logger.LogTrace(
				"Commenting on {addedCount} added, {removedCount} removed, and {updatedCount} updated test merge sources...",
				addedTestMerges.Count,
				removedTestMerges.Count,
				updatedTestMerges.Count);

			var tasks = new List<ValueTask>(addedTestMerges.Count + updatedTestMerges.Count + removedTestMerges.Count);
			foreach (var addedTestMerge in addedTestMerges)
			{
				if (addedTestMerge.SourceRepository != null)
					continue;

				var addCommentTask = CommentOnTestMergeSource(
					repositorySettings,
					repoOwner,
					repoName,
					FormatTestMerge(
						repositorySettings,
						compileJob,
						addedTestMerge,
						repoOwner,
						repoName,
						false),
					addedTestMerge.Number,
					cancellationToken);
				tasks.Add(addCommentTask);
			}

			foreach (var removedTestMerge in removedTestMerges)
			{
				if (removedTestMerge.SourceRepository != null)
					continue;

				var removeCommentTask = CommentOnTestMergeSource(
					repositorySettings,
					repoOwner,
					repoName,
					FormatTestMergeRemoval(
						repositorySettings,
						compileJob,
						removedTestMerge,
						repoOwner,
						repoName),
					removedTestMerge.Number,
					cancellationToken);
				tasks.Add(removeCommentTask);
			}

			foreach (var updatedTestMerge in updatedTestMerges)
			{
				if (updatedTestMerge.SourceRepository != null)
					continue;

				var updateCommentTask = CommentOnTestMergeSource(
					repositorySettings,
					repoOwner,
					repoName,
					FormatTestMerge(
						repositorySettings,
						compileJob,
						updatedTestMerge,
						repoOwner,
						repoName,
						true),
					updatedTestMerge.Number,
					cancellationToken);
				tasks.Add(updateCommentTask);
			}

			if (tasks.Count > 0)
				await ValueTaskExtensions.WhenAll(tasks);
		}

		/// <inheritdoc />
		public ValueTask ApplyDeployment(CompileJob compileJob, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(compileJob);

			if (activationCallbacks.TryGetValue(compileJob.Require(x => x.Id), out var activationCallback))
				activationCallback(true);

			return ApplyDeploymentImpl(compileJob, cancellationToken);
		}

		/// <inheritdoc />
		public abstract ValueTask FailDeployment(CompileJob compileJob, string errorMessage, CancellationToken cancellationToken);

		/// <inheritdoc />
		public ValueTask MarkInactive(CompileJob compileJob, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(compileJob);

			if (activationCallbacks.TryRemove(compileJob.Require(x => x.Id), out var activationCallback))
				activationCallback(false);

			return MarkInactiveImpl(compileJob, cancellationToken);
		}

		/// <inheritdoc />
		public abstract ValueTask<IReadOnlyCollection<TestMerge>> RemoveMergedTestMerges(
			IRepository repository,
			RepositorySettings repositorySettings,
			RevisionInformation revisionInformation,
			CancellationToken cancellationToken);

		/// <inheritdoc />
		public ValueTask StageDeployment(CompileJob compileJob, Action<bool>? activationCallback, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(compileJob);

			var compileJobId = compileJob.Require(x => x.Id);
			if (activationCallback != null && !activationCallbacks.TryAdd(compileJobId, activationCallback))
				Logger.LogError("activationCallbacks conflicted on CompileJob #{id}!", compileJobId);

			return StageDeploymentImpl(compileJob, cancellationToken);
		}

		/// <inheritdoc />
		public abstract ValueTask StartDeployment(
			Api.Models.Internal.IGitRemoteInformation remoteInformation,
			CompileJob compileJob,
			CancellationToken cancellationToken);

		/// <summary>
		/// Implementation of <see cref="StageDeployment(CompileJob, Action{bool}, CancellationToken)"/>.
		/// </summary>
		/// <param name="compileJob">The staged <see cref="CompileJob"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected abstract ValueTask StageDeploymentImpl(CompileJob compileJob, CancellationToken cancellationToken);

		/// <summary>
		/// Implementation of <see cref="ApplyDeployment(CompileJob, CancellationToken)"/>.
		/// </summary>
		/// <param name="compileJob">The <see cref="CompileJob"/> being applied.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected abstract ValueTask ApplyDeploymentImpl(CompileJob compileJob, CancellationToken cancellationToken);

		/// <summary>
		/// Implementation of <see cref="MarkInactive(CompileJob, CancellationToken)"/>.
		/// </summary>
		/// <param name="compileJob">The inactive <see cref="CompileJob"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected abstract ValueTask MarkInactiveImpl(CompileJob compileJob, CancellationToken cancellationToken);

		/// <summary>
		/// Formats a comment for a given <paramref name="testMerge"/>.
		/// </summary>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/> to use.</param>
		/// <param name="compileJob">The test merge's <see cref="CompileJob"/>.</param>
		/// <param name="testMerge">The <see cref="TestMerge"/>.</param>
		/// <param name="remoteRepositoryOwner">The <see cref="Api.Models.Internal.IGitRemoteInformation.RemoteRepositoryOwner"/>.</param>
		/// <param name="remoteRepositoryName">The <see cref="Api.Models.Internal.IGitRemoteInformation.RemoteRepositoryName"/>.</param>
		/// <param name="updated">If <see langword="false"/> <paramref name="testMerge"/> is new, otherwise it has been updated to a different <see cref="Api.Models.TestMergeParameters.TargetCommitSha"/>.</param>
		/// <returns>A formatted <see cref="string"/> for posting a informative comment about the <paramref name="testMerge"/>.</returns>
		protected abstract string FormatTestMerge(
			RepositorySettings repositorySettings,
			CompileJob compileJob,
			TestMerge testMerge,
			string remoteRepositoryOwner,
			string remoteRepositoryName,
			bool updated);

		/// <summary>
		/// Formats a comment for a given <paramref name="testMerge"/> removal.
		/// </summary>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/> to use.</param>
		/// <param name="compileJob">The test merge's <see cref="CompileJob"/>.</param>
		/// <param name="testMerge">The <see cref="TestMerge"/>.</param>
		/// <param name="remoteRepositoryOwner">The <see cref="Api.Models.Internal.IGitRemoteInformation.RemoteRepositoryOwner"/>.</param>
		/// <param name="remoteRepositoryName">The <see cref="Api.Models.Internal.IGitRemoteInformation.RemoteRepositoryName"/>.</param>
		/// <returns>A formatted <see cref="string"/> for posting a informative comment about the <paramref name="testMerge"/> removal.</returns>
		protected abstract string FormatTestMergeRemoval(
			RepositorySettings repositorySettings,
			CompileJob compileJob,
			TestMerge testMerge,
			string remoteRepositoryOwner,
			string remoteRepositoryName);

		/// <summary>
		/// Create a comment of a given <paramref name="testMergeNumber"/>'s source.
		/// </summary>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/> to use.</param>
		/// <param name="remoteRepositoryOwner">The <see cref="Api.Models.Internal.IGitRemoteInformation.RemoteRepositoryOwner"/>.</param>
		/// <param name="remoteRepositoryName">The <see cref="Api.Models.Internal.IGitRemoteInformation.RemoteRepositoryName"/>.</param>
		/// <param name="comment">The comment to post.</param>
		/// <param name="testMergeNumber">The <see cref="Api.Models.TestMergeParameters.Number"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected abstract ValueTask CommentOnTestMergeSource(
			RepositorySettings repositorySettings,
			string remoteRepositoryOwner,
			string remoteRepositoryName,
			string comment,
			int testMergeNumber,
			CancellationToken cancellationToken);
	}
}
