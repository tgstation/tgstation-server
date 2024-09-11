using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Octokit;

using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Host.Components.Deployment.Remote
{
	/// <summary>
	/// <see cref="IRemoteDeploymentManager"/> for GitHub.com.
	/// </summary>
	sealed class GitHubRemoteDeploymentManager : BaseRemoteDeploymentManager
	{
		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="GitHubRemoteDeploymentManager"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IGitHubServiceFactory"/> for the <see cref="GitHubRemoteDeploymentManager"/>.
		/// </summary>
		readonly IGitHubServiceFactory gitHubServiceFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubRemoteDeploymentManager"/> class.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="gitHubServiceFactory">The value of <see cref="gitHubServiceFactory"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		/// <param name="metadata">The <see cref="Api.Models.Instance"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		/// <param name="activationCallbacks">The activation callback <see cref="ConcurrentDictionary{TKey, TValue}"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		public GitHubRemoteDeploymentManager(
			IDatabaseContextFactory databaseContextFactory,
			IGitHubServiceFactory gitHubServiceFactory,
			ILogger<GitHubRemoteDeploymentManager> logger,
			Api.Models.Instance metadata,
			ConcurrentDictionary<long, Action<bool>> activationCallbacks)
			: base(logger, metadata, activationCallbacks)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.gitHubServiceFactory = gitHubServiceFactory ?? throw new ArgumentNullException(nameof(gitHubServiceFactory));
		}

		/// <inheritdoc />
		public override async ValueTask StartDeployment(
			Api.Models.Internal.IGitRemoteInformation remoteInformation,
			CompileJob compileJob,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(remoteInformation);
			ArgumentNullException.ThrowIfNull(compileJob);

			Logger.LogTrace("Starting deployment...");

			RepositorySettings? repositorySettings = null;
			await databaseContextFactory.UseContext(
				async databaseContext =>
					repositorySettings = await databaseContext
						.RepositorySettings
						.AsQueryable()
						.Where(x => x.InstanceId == Metadata.Id)
						.FirstAsync(cancellationToken));

			var instanceAuthenticated = repositorySettings!.AccessToken != null;
			IAuthenticatedGitHubService? authenticatedGitHubService;
			IGitHubService? gitHubService;
			if (instanceAuthenticated)
			{
				authenticatedGitHubService = await gitHubServiceFactory.CreateService(
					repositorySettings.AccessToken!,
					new RepositoryIdentifier(remoteInformation),
					cancellationToken);

				if (authenticatedGitHubService == null)
				{
					Logger.LogWarning("Can't create GitHub deployment as authentication for repository failed!");
					gitHubService = await gitHubServiceFactory.CreateService(cancellationToken);
				}
				else
					gitHubService = authenticatedGitHubService;
			}
			else
			{
				authenticatedGitHubService = null;
				gitHubService = await gitHubServiceFactory.CreateService(cancellationToken);
			}

			var repoOwner = remoteInformation.RemoteRepositoryOwner!;
			var repoName = remoteInformation.RemoteRepositoryName!;
			var repositoryIdTask = gitHubService.GetRepositoryId(
				repoOwner,
				repoName,
				cancellationToken);

			if (!repositorySettings.CreateGitHubDeployments!.Value)
				Logger.LogTrace("Not creating deployment");
			else if (!instanceAuthenticated)
				Logger.LogWarning("Can't create GitHub deployment as no access token is set for repository!");
			else if (authenticatedGitHubService != null)
			{
				Logger.LogTrace("Creating deployment...");
				try
				{
					compileJob.GitHubDeploymentId = await authenticatedGitHubService.CreateDeployment(
						new NewDeployment(compileJob.RevisionInformation.CommitSha)
						{
							AutoMerge = false,
							Description = "TGS Game Deployment",
							Environment = $"TGS: {Metadata.Name}",
							ProductionEnvironment = true,
							RequiredContexts = new Collection<string>(),
						},
						repoOwner,
						repoName,
						cancellationToken);

					Logger.LogDebug("Created deployment ID {deploymentId}", compileJob.GitHubDeploymentId);

					await authenticatedGitHubService.CreateDeploymentStatus(
						new NewDeploymentStatus(DeploymentState.InProgress)
						{
							Description = "The project is being deployed",
							AutoInactive = false,
						},
						repoOwner,
						repoName,
						compileJob.GitHubDeploymentId.Value,
						cancellationToken);

					Logger.LogTrace("In-progress deployment status created");
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					Logger.LogWarning(ex, "Unable to create GitHub deployment!");
				}
			}

			try
			{
				compileJob.GitHubRepoId = await repositoryIdTask;
				Logger.LogTrace("Set GitHub ID as {gitHubRepoId}", compileJob.GitHubRepoId);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Logger.LogWarning(ex, "Unable to set compile job repository ID!");
			}
		}

		/// <inheritdoc />
		public override ValueTask FailDeployment(CompileJob compileJob, string errorMessage, CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				errorMessage,
				DeploymentState.Error,
				cancellationToken);

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

			var gitHubService = repositorySettings.AccessToken != null
				? await gitHubServiceFactory.CreateService(
					repositorySettings.AccessToken,
					new RepositoryIdentifier(repository),
					cancellationToken)
					?? await gitHubServiceFactory.CreateService(cancellationToken)
				: await gitHubServiceFactory.CreateService(cancellationToken);

			var tasks = revisionInformation
				.ActiveTestMerges
				.Select(x => gitHubService.GetPullRequest(
					repository.RemoteRepositoryOwner!,
					repository.RemoteRepositoryName!,
					x.TestMerge.Number,
					cancellationToken));
			try
			{
				await Task.WhenAll(tasks);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Logger.LogWarning(ex, "Pull requests update check failed!");
			}

			var newList = revisionInformation.ActiveTestMerges.Select(x => x.TestMerge).ToList();

			PullRequest? lastMerged = null;
			async ValueTask CheckRemovePR(Task<PullRequest> task)
			{
				var pr = await task;
				if (!pr.Merged)
					return;

				// We don't just assume, actually check the repo contains the merge commit.
				if (await repository.CommittishIsParent(pr.MergeCommitSha, cancellationToken))
				{
					if (lastMerged == null || lastMerged.MergedAt < pr.MergedAt)
						lastMerged = pr;
					newList.Remove(
						newList.First(
							potential => potential.Number == pr.Number));
				}
			}

			foreach (var prTask in tasks)
				await CheckRemovePR(prTask);

			return newList;
		}

		/// <inheritdoc />
		protected override ValueTask StageDeploymentImpl(
			CompileJob compileJob,
			CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				"The deployment succeeded and will be applied a the next server reboot.",
				DeploymentState.Pending,
				cancellationToken);

		/// <inheritdoc />
		protected override ValueTask ApplyDeploymentImpl(CompileJob compileJob, CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				"The deployment is now live on the server.",
				DeploymentState.Success,
				cancellationToken);

		/// <inheritdoc />
		protected override ValueTask MarkInactiveImpl(CompileJob compileJob, CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				"The deployment has been superceeded.",
				DeploymentState.Inactive,
				cancellationToken);

		/// <inheritdoc />
		protected override async ValueTask CommentOnTestMergeSource(
			RepositorySettings repositorySettings,
			string remoteRepositoryOwner,
			string remoteRepositoryName,
			string comment,
			int testMergeNumber,
			CancellationToken cancellationToken)
		{
			var gitHubService = await gitHubServiceFactory.CreateService(
				repositorySettings.AccessToken!,
				new RepositoryIdentifier(remoteRepositoryOwner, remoteRepositoryName),
				cancellationToken);

			if (gitHubService == null)
			{
				Logger.LogWarning("Error posting GitHub comment: Authentication failed!");
				return;
			}

			try
			{
				await gitHubService.CommentOnIssue(remoteRepositoryOwner, remoteRepositoryName, comment, testMergeNumber, cancellationToken);
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
			"#### Test Merge {4}{0}{0}<details><summary>Details</summary>{0}{0}##### Server Instance{0}{5}{1}{0}{0}##### Revision{0}Origin: {6}{0}Pull Request: {2}{0}Server: {7}{3}{8}{0}</details>",
			Environment.NewLine,
			repositorySettings.ShowTestMergeCommitters!.Value
				? String.Format(
					CultureInfo.InvariantCulture,
					"{0}{0}##### Merged By{0}{1}",
					Environment.NewLine,
					testMerge.MergedBy!.Name)
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
			compileJob.RevisionInformation.CommitSha,
			compileJob.GitHubDeploymentId.HasValue
				? $"{Environment.NewLine}[GitHub Deployments](https://github.com/{remoteRepositoryOwner}/{remoteRepositoryName}/deployments/activity_log?environment=TGS%3A+{Metadata.Name!.Replace(" ", "+", StringComparison.Ordinal)})"
				: String.Empty);

		/// <summary>
		/// Update the deployment for a given <paramref name="compileJob"/>.
		/// </summary>
		/// <param name="compileJob">The <see cref="CompileJob"/>.</param>
		/// <param name="description">A description of the update.</param>
		/// <param name="deploymentState">The new <see cref="DeploymentState"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask UpdateDeployment(
			CompileJob compileJob,
			string description,
			DeploymentState deploymentState,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(compileJob);

			if (!compileJob.GitHubRepoId.HasValue || !compileJob.GitHubDeploymentId.HasValue)
			{
				Logger.LogTrace("Not updating deployment as it is missing a repo ID or deployment ID.");
				return;
			}

			Logger.LogTrace("Updating deployment {gitHubDeploymentId} to {deploymentState}...", compileJob.GitHubDeploymentId.Value, deploymentState);

			string? gitHubAccessToken = null;
			await databaseContextFactory.UseContext(
				async databaseContext =>
					gitHubAccessToken = await databaseContext
						.RepositorySettings
						.AsQueryable()
						.Where(x => x.InstanceId == Metadata.Id)
						.Select(x => x.AccessToken)
						.FirstAsync(cancellationToken));

			if (gitHubAccessToken == null)
			{
				Logger.LogWarning(
					"GitHub access token disappeared during deployment, can't update to {deploymentState}!",
					deploymentState);
				return;
			}

			var gitHubService = await gitHubServiceFactory.CreateService(
				gitHubAccessToken,
				new RepositoryIdentifier(compileJob.GitHubRepoId.Value),
				cancellationToken);

			if (gitHubService == null)
			{
				Logger.LogWarning(
					"GitHub authentication failed, can't update to {deploymentState}!",
					deploymentState);
				return;
			}

			try
			{
				await gitHubService.CreateDeploymentStatus(
					new NewDeploymentStatus(deploymentState)
					{
						Description = description,
					},
					compileJob.GitHubRepoId.Value,
					compileJob.GitHubDeploymentId.Value,
					cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Logger.LogWarning(ex, "Error updating GitHub deployment!");
			}
		}
	}
}
