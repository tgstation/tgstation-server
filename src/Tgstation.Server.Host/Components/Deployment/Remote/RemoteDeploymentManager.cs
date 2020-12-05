using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment.Remote
{
	/// <inheritdoc />
	sealed class RemoteDeploymentManager : IRemoteDeploymentManager
	{
		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="RemoteDeploymentManager"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="RemoteDeploymentManager"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="RemoteDeploymentManager"/>.
		/// </summary>
		readonly ILogger<RemoteDeploymentManager> logger;

		/// <summary>
		/// The <see cref="Models.Instance"/> for the <see cref="RemoteDeploymentManager"/>.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteDeploymentManager"/> <see langword="class"/>.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="metadata">The value of <see cref="metadata"/>.</param>
		public RemoteDeploymentManager(
			IDatabaseContextFactory databaseContextFactory,
			IGitHubClientFactory gitHubClientFactory,
			ILogger<RemoteDeploymentManager> logger,
			Api.Models.Instance metadata)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
		}

		/// <inheritdoc />
		public async Task StartDeployment(IRepository repository, CompileJob compileJob, CancellationToken cancellationToken)
		{
			if (repository == null)
				throw new ArgumentNullException(nameof(repository));
			if (compileJob == null)
				throw new ArgumentNullException(nameof(compileJob));

			if (repository.RemoteGitProvider != Api.Models.RemoteGitProvider.GitHub)
			{
				logger.LogTrace("Not managing deployment as this is not a GitHub repo");
				return;
			}

			logger.LogTrace("Starting deployment...");

			RepositorySettings repositorySettings = null;
			await databaseContextFactory.UseContext(
				async databaseContext =>
					repositorySettings = await databaseContext
						.RepositorySettings
						.AsQueryable()
						.Where(x => x.InstanceId == metadata.Id)
						.FirstAsync(cancellationToken)
						.ConfigureAwait(false))
				.ConfigureAwait(false);

			var instanceAuthenticated = repositorySettings.AccessToken == null;
			var gitHubClient = repositorySettings.AccessToken == null
				? gitHubClientFactory.CreateClient()
				: gitHubClientFactory.CreateClient(repositorySettings.AccessToken);

			var repositoryTask = gitHubClient
				.Repository
				.Get(
					repository.RemoteRepositoryOwner,
					repository.RemoteRepositoryName);

			if (!repositorySettings.CreateGitHubDeployments.Value)
				logger.LogTrace("Not creating deployment");
			else if (!instanceAuthenticated)
				logger.LogWarning("Can't create GitHub deployment as no access token is set for repository!");
			else
			{
				logger.LogTrace("Creating deployment...");
				var deployment = await gitHubClient
					.Repository
					.Deployment
					.Create(
						repository.RemoteRepositoryOwner,
						repository.RemoteRepositoryName,
						new NewDeployment(compileJob.RevisionInformation.CommitSha)
						{
							AutoMerge = false,
							Description = "TGS Game Deployment",
							Environment = $"TGS: {metadata.Name}",
							ProductionEnvironment = true,
							RequiredContexts = new Collection<string>()
						})
					.WithToken(cancellationToken)
					.ConfigureAwait(false);

				compileJob.GitHubDeploymentId = deployment.Id;
				logger.LogDebug("Created deployment ID {0}", deployment.Id);

				await gitHubClient
					.Repository
					.Deployment
					.Status
					.Create(
						repository.RemoteRepositoryOwner,
						repository.RemoteRepositoryName,
						deployment.Id,
						new NewDeploymentStatus(DeploymentState.InProgress)
						{
							Description = "The project is being deployed",
							AutoInactive = false
						})
					.WithToken(cancellationToken)
					.ConfigureAwait(false);

				logger.LogTrace("In-progress deployment status created");
			}

			try
			{
				var gitHubRepo = await repositoryTask
					.WithToken(cancellationToken)
					.ConfigureAwait(false);

				compileJob.GitHubRepoId = gitHubRepo.Id;
				logger.LogTrace("Set GitHub ID as {0}", compileJob.GitHubRepoId);
			}
			catch (RateLimitExceededException ex) when (!repositorySettings.CreateGitHubDeployments.Value)
			{
				logger.LogWarning(ex, "Unable to set compile job repository ID!");
			}
		}

		async Task UpdateDeployment(
			CompileJob compileJob,
			string description,
			DeploymentState deploymentState,
			CancellationToken cancellationToken)
		{
			if (compileJob == null)
				throw new ArgumentNullException(nameof(compileJob));

			if (!compileJob.GitHubRepoId.HasValue || !compileJob.GitHubDeploymentId.HasValue)
			{
				logger.LogTrace("Not updating deployment as it is missing a repo ID or deployment ID.");
				return;
			}

			logger.LogTrace("Updating deployment {0} to {1}...", compileJob.GitHubDeploymentId.Value, deploymentState);

			string gitHubAccessToken = null;
			await databaseContextFactory.UseContext(
				async databaseContext =>
					gitHubAccessToken = await databaseContext
						.RepositorySettings
						.AsQueryable()
						.Where(x => x.InstanceId == metadata.Id)
						.Select(x => x.AccessToken)
						.FirstAsync(cancellationToken)
						.ConfigureAwait(false))
				.ConfigureAwait(false);

			if (gitHubAccessToken == null)
			{
				logger.LogWarning(
					"GitHub access token disappeared during deployment, can't update to {0}!",
					deploymentState);
				return;
			}

			var gitHubClient = gitHubClientFactory.CreateClient(gitHubAccessToken);

			await gitHubClient
				.Repository
				.Deployment
				.Status
				.Create(
					compileJob.GitHubRepoId.Value,
					compileJob.GitHubDeploymentId.Value,
					new NewDeploymentStatus(deploymentState)
					{
						Description = description
					})
				.WithToken(cancellationToken)
				.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task StageDeployment(
			CompileJob compileJob,
			CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				"The deployment succeeded and will be applied a the next server reboot.",
				DeploymentState.Pending,
				cancellationToken);

		/// <inheritdoc />
		public Task ApplyDeployment(CompileJob compileJob, CompileJob oldCompileJob, CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				"The deployment is now live on the server.",
				DeploymentState.Success,
				cancellationToken);

		/// <inheritdoc />
		public Task FailDeployment(CompileJob compileJob, string errorMessage, CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				errorMessage,
				DeploymentState.Error,
				cancellationToken);

		/// <inheritdoc />
		public Task MarkInactive(CompileJob compileJob, CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				"The deployment has been superceeded.",
				DeploymentState.Inactive,
				cancellationToken);

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

			if ((previousRevisionInformation != null && previousRevisionInformation.CommitSha == previousRevisionInformation.CommitSha)
				|| !repositorySettings.PostTestMergeComment.Value)
				return;

			previousRevisionInformation = new RevisionInformation
			{
				ActiveTestMerges = new List<RevInfoTestMerge>()
			};

			var gitHubClient = gitHubClientFactory.CreateClient(repositorySettings.AccessToken);

			async Task CommentOnPR(int prNumber, string comment)
			{
				try
				{
					await gitHubClient.Issue.Comment.Create(repoOwner, repoName, prNumber, comment)
						.WithToken(cancellationToken)
						.ConfigureAwait(false);
				}
				catch (ApiException e)
				{
					logger.LogWarning(e, "Error posting GitHub comment!");
				}
			}

			var tasks = new List<Task>();

			var deployedRevisionInformation = compileJob.RevisionInformation;
			string FormatTestMerge(TestMerge testMerge, bool updated) => String.Format(CultureInfo.InvariantCulture, "#### Test Merge {4}{0}{0}##### Server Instance{0}{5}{1}{0}{0}##### Revision{0}Origin: {6}{0}Pull Request: {2}{0}Server: {7}{3}{8}",
				Environment.NewLine,
				repositorySettings.ShowTestMergeCommitters.Value ? String.Format(CultureInfo.InvariantCulture, "{0}{0}##### Merged By{0}{1}", Environment.NewLine, testMerge.MergedBy.Name) : String.Empty,
				testMerge.PullRequestRevision,
				testMerge.Comment != null ? String.Format(CultureInfo.InvariantCulture, "{0}{0}##### Comment{0}{1}", Environment.NewLine, testMerge.Comment) : String.Empty,
				updated ? "Updated" : "Deployed",
				metadata.Name,
				deployedRevisionInformation.OriginCommitSha,
				deployedRevisionInformation.CommitSha,
				compileJob.GitHubDeploymentId.HasValue
					? $"{Environment.NewLine}[GitHub Deployments](https://github.com/{repoOwner}/{repoName}/deployments/activity_log?environment=TGS%3A%20{metadata.Name})"
					: String.Empty);

			// added prs
			foreach (var I in deployedRevisionInformation
				.ActiveTestMerges
				.Select(x => x.TestMerge)
				.Where(x => !previousRevisionInformation
					.ActiveTestMerges
					.Any(y => y.TestMerge.Number == x.Number)))
				tasks.Add(CommentOnPR(I.Number, FormatTestMerge(I, false)));

			// removed prs
			foreach (var I in previousRevisionInformation
				.ActiveTestMerges
				.Select(x => x.TestMerge)
				.Where(x => !deployedRevisionInformation
				.ActiveTestMerges
				.Any(y => y.TestMerge.Number == x.Number)))
				tasks.Add(CommentOnPR(I.Number, "#### Test Merge Removed"));

			// updated prs
			foreach (var I in deployedRevisionInformation
				.ActiveTestMerges
				.Select(x => x.TestMerge)
				.Where(x => previousRevisionInformation
					.ActiveTestMerges
					.Any(y => y.TestMerge.Number == x.Number)))
				tasks.Add(CommentOnPR(I.Number, FormatTestMerge(I, true)));

			if (tasks.Any())
				await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<IReadOnlyCollection<RevInfoTestMerge>> RemoveMergedPullRequests(
			IRepository repository,
			RepositorySettings repositorySettings,
			RevisionInformation revisionInformation,
			CancellationToken cancellationToken)
		{
			if (revisionInformation.ActiveTestMerges?.Any() != true)
			{
				logger.LogTrace("No test merges to remove.");
				return Array.Empty<RevInfoTestMerge>();
			}

			var gitHubClient = repositorySettings.AccessToken != null
				? gitHubClientFactory.CreateClient(repositorySettings.AccessToken)
				: gitHubClientFactory.CreateClient();

			var tasks = revisionInformation
				.ActiveTestMerges
				.Select(x => gitHubClient
					.PullRequest
					.Get(repository.RemoteRepositoryOwner, repository.RemoteRepositoryName, x.TestMerge.Number)
					.WithToken(cancellationToken));
			try
			{
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}
			catch (Exception ex) when (!(ex is OperationCanceledException))
			{
				logger.LogWarning(ex, "Pull requests update check failed!");
			}

			var newList = revisionInformation.ActiveTestMerges.ToList();

			PullRequest lastMerged = null;
			async Task CheckRemovePR(Task<PullRequest> task)
			{
				var pr = await task.ConfigureAwait(false);
				if (!pr.Merged)
					return;

				// We don't just assume, actually check the repo contains the merge commit.
				if (await repository.ShaIsParent(pr.MergeCommitSha, cancellationToken).ConfigureAwait(false))
				{
					if (lastMerged == null || lastMerged.MergedAt < pr.MergedAt)
						lastMerged = pr;
					newList.Remove(
						newList.First(
							potential => potential.TestMerge.Number == pr.Number));
				}
			}

			foreach (var prTask in tasks)
				await CheckRemovePR(prTask).ConfigureAwait(false);

			return newList;
		}
	}
}
