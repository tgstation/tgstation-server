using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
		/// The <see cref="Api.Models.Instance"/> for the <see cref="BaseRemoteDeploymentManager"/>.
		/// </summary>
		protected Api.Models.Instance Metadata { get; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="BaseRemoteDeploymentManager"/>.
		/// </summary>
		protected ILogger<BaseRemoteDeploymentManager> Logger { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BaseRemoteDeploymentManager"/> <see langword="class"/>.
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="metadata">The value of <see cref="Metadata"/>.</param>
		protected BaseRemoteDeploymentManager(ILogger<BaseRemoteDeploymentManager> logger, Api.Models.Instance metadata)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
		}

		/// <inheritdoc />
		public async Task PostDeploymentComments(
			CompileJob compileJob,
			RevisionInformation previousRevisionInformation,
			RepositorySettings repositorySettings,
			string repoOwner,
			string repoName,
			CancellationToken cancellationToken)
		{
			if (repositorySettings?.AccessToken == null)
				return;

			var deployedRevisionInformation = compileJob.RevisionInformation;
			if ((previousRevisionInformation != null && previousRevisionInformation.CommitSha == deployedRevisionInformation.CommitSha)
				|| !repositorySettings.PostTestMergeComment.Value)
				return;

			previousRevisionInformation ??= new RevisionInformation();
			previousRevisionInformation.ActiveTestMerges ??= new List<RevInfoTestMerge>();

			deployedRevisionInformation.ActiveTestMerges ??= new List<RevInfoTestMerge>();
			var tasks = new List<Task>();

			// added prs
			var tmsAdded = deployedRevisionInformation
				.ActiveTestMerges
				.Select(x => x.TestMerge)
				.Where(x => !previousRevisionInformation
					.ActiveTestMerges
					.Any(y => y.TestMerge.Number == x.Number))
				.ToList();
			var tmsRemoved = previousRevisionInformation
				.ActiveTestMerges
				.Select(x => x.TestMerge)
				.Where(x => !deployedRevisionInformation
					.ActiveTestMerges
					.Any(y => y.TestMerge.Number == x.Number))
				.ToList();
			var tmsUpdated = deployedRevisionInformation
				.ActiveTestMerges
				.Select(x => x.TestMerge)
				.Where(x => previousRevisionInformation
					.ActiveTestMerges
					.Any(y => y.TestMerge.Number == x.Number))
				.ToList();

			if (!tmsAdded.Any() && !tmsRemoved.Any() && !tmsUpdated.Any())
				return;

			Logger.LogTrace(
				"Commenting on {0} added, {1} removed, and {2} updated test merge sources...",
				tmsAdded.Count,
				tmsRemoved.Count,
				tmsUpdated.Count);
			foreach (var I in tmsAdded)
				tasks.Add(
					CommentOnTestMergeSource(
						repositorySettings,
						repoOwner,
						repoName,
						FormatTestMerge(
							repositorySettings,
							compileJob,
							I,
							repoOwner,
							repoName,
							false),
						I.Number,
						cancellationToken));

			foreach (var I in tmsRemoved)
				tasks.Add(
					CommentOnTestMergeSource(
						repositorySettings,
						repoOwner,
						repoName,
						"#### Test Merge Removed",
						I.Number,
						cancellationToken));

			foreach (var I in tmsUpdated)
				tasks.Add(
					CommentOnTestMergeSource(
						repositorySettings,
						repoOwner,
						repoName,
						FormatTestMerge(
							repositorySettings,
							compileJob,
							I,
							repoOwner,
							repoName,
							true),
						I.Number,
						cancellationToken));

			if (tasks.Any())
				await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		/// <summary>
		/// Create a comment of a given <paramref name="testMergeNumber"/>'s source.
		/// </summary>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/> to use.</param>
		/// <param name="remoteRepositoryOwner">The <see cref="Api.Models.Internal.IGitRemoteInformation.RemoteRepositoryOwner"/>.</param>
		/// <param name="remoteRepositoryName">The <see cref="Api.Models.Internal.IGitRemoteInformation.RemoteRepositoryName"/>.</param>
		/// <param name="comment">The comment to post.</param>
		/// <param name="testMergeNumber">The <see cref="Api.Models.TestMergeParameters.Number"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected abstract Task CommentOnTestMergeSource(
			RepositorySettings repositorySettings,
			string remoteRepositoryOwner,
			string remoteRepositoryName,
			string comment,
			int testMergeNumber,
			CancellationToken cancellationToken);

		/// <summary>
		/// Formats a comment for a given <paramref name="testMerge"/>.
		/// </summary>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/> to use.</param>
		/// <param name="compileJob">The test merge's <see cref="CompileJob"/>.</param>
		/// <param name="testMerge">The <see cref="TestMerge"/>.</param>
		/// <param name="remoteRepositoryOwner">The <see cref="Api.Models.Internal.IGitRemoteInformation.RemoteRepositoryOwner"/>.</param>
		/// <param name="remoteRepositoryName">The <see cref="Api.Models.Internal.IGitRemoteInformation.RemoteRepositoryName"/>.</param>
		/// <param name="updated">If <see langword="false"/> <paramref name="testMerge"/> is new, otherwise it has been updated to a different <see cref="Api.Models.TestMergeParameters.TargetCommitSha"/>.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected abstract string FormatTestMerge(
			RepositorySettings repositorySettings,
			CompileJob compileJob,
			TestMerge testMerge,
			string remoteRepositoryOwner,
			string remoteRepositoryName,
			bool updated);

		/// <inheritdoc />
		public abstract Task ApplyDeployment(CompileJob compileJob, CompileJob oldCompileJob, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task FailDeployment(CompileJob compileJob, string errorMessage, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task MarkInactive(CompileJob compileJob, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task<IReadOnlyCollection<RevInfoTestMerge>> RemoveMergedTestMerges(
			IRepository repository,
			RepositorySettings repositorySettings,
			RevisionInformation revisionInformation,
			CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task StageDeployment(CompileJob compileJob, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task StartDeployment(
			Api.Models.Internal.IGitRemoteInformation remoteInformation,
			CompileJob compileJob,
			CancellationToken cancellationToken);
	}
}
