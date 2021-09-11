using System;
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
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;

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
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="GitHubRemoteDeploymentManager"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubRemoteDeploymentManager"/> class.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		/// <param name="metadata">The <see cref="Api.Models.Instance"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		public GitHubRemoteDeploymentManager(
			IDatabaseContextFactory databaseContextFactory,
			IGitHubClientFactory gitHubClientFactory,
			ILogger<GitHubRemoteDeploymentManager> logger,
			Api.Models.Instance metadata)
			: base(logger, metadata)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
		}

		/// <inheritdoc />
		public override async Task StartDeployment(
			Api.Models.GitRemoteInformation remoteInformation,
			CompileJob compileJob,
			CancellationToken cancellationToken)
		{
			if (remoteInformation == null)
				throw new ArgumentNullException(nameof(remoteInformation));
			if (compileJob == null)
				throw new ArgumentNullException(nameof(compileJob));

			Logger.LogTrace("Starting deployment...");

			RepositorySettings? repositorySettingsTemp = null;
			await databaseContextFactory.UseContext(
				async databaseContext =>
					repositorySettingsTemp = await databaseContext
						.RepositorySettings
						.AsQueryable()
						.Where(x => x.InstanceId == Metadata.Id)
						.FirstAsync(cancellationToken)
						.ConfigureAwait(false))
				.ConfigureAwait(false);

			var repositorySettings = repositorySettingsTemp!;

			var instanceAuthenticated = repositorySettings.AccessToken == null;
			var gitHubClient = repositorySettings.AccessToken == null
				? gitHubClientFactory.CreateClient()
				: gitHubClientFactory.CreateClient(repositorySettings.AccessToken);

			var repositoryTask = gitHubClient
				.Repository
				.Get(
					remoteInformation.RepositoryOwner,
					remoteInformation.RepositoryName);

			if (!repositorySettings.CreateGitHubDeployments.Value)
				Logger.LogTrace("Not creating deployment");
			else if (!instanceAuthenticated)
				Logger.LogWarning("Can't create GitHub deployment as no access token is set for repository!");
			else
			{
				Logger.LogTrace("Creating deployment...");
				var deployment = await gitHubClient
					.Repository
					.Deployment
					.Create(
						remoteInformation.RepositoryOwner,
						remoteInformation.RepositoryName,
						new NewDeployment(compileJob.RevisionInformation.CommitSha)
						{
							AutoMerge = false,
							Description = "TGS Game Deployment",
							Environment = $"TGS: {Metadata.Name}",
							ProductionEnvironment = true,
							RequiredContexts = new Collection<string>(),
						})
					.WithToken(cancellationToken)
					.ConfigureAwait(false);

				compileJob.GitHubDeploymentId = deployment.Id;
				Logger.LogDebug("Created deployment ID {0}", deployment.Id);

				await gitHubClient
					.Repository
					.Deployment
					.Status
					.Create(
						remoteInformation.RepositoryOwner,
						remoteInformation.RepositoryName,
						deployment.Id,
						new NewDeploymentStatus(DeploymentState.InProgress)
						{
							Description = "The project is being deployed",
							AutoInactive = false,
						})
					.WithToken(cancellationToken)
					.ConfigureAwait(false);

				Logger.LogTrace("In-progress deployment status created");
			}

			try
			{
				var gitHubRepo = await repositoryTask
					.WithToken(cancellationToken)
					.ConfigureAwait(false);

				compileJob.GitHubRepoId = gitHubRepo.Id;
				Logger.LogTrace("Set GitHub ID as {0}", compileJob.GitHubRepoId);
			}
			catch (RateLimitExceededException ex) when (!repositorySettings.CreateGitHubDeployments.Value)
			{
				Logger.LogWarning(ex, "Unable to set compile job repository ID!");
			}
		}

		/// <inheritdoc />
		public override Task StageDeployment(
			CompileJob compileJob,
			CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				"The deployment succeeded and will be applied a the next server reboot.",
				DeploymentState.Pending,
				cancellationToken);

		/// <inheritdoc />
		public override Task ApplyDeployment(CompileJob compileJob, CompileJob? oldCompileJob, CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				"The deployment is now live on the server.",
				DeploymentState.Success,
				cancellationToken);

		/// <inheritdoc />
		public override Task FailDeployment(CompileJob compileJob, string errorMessage, CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				errorMessage,
				DeploymentState.Error,
				cancellationToken);

		/// <inheritdoc />
		public override Task MarkInactive(CompileJob compileJob, CancellationToken cancellationToken)
			=> UpdateDeployment(
				compileJob,
				"The deployment has been superceeded.",
				DeploymentState.Inactive,
				cancellationToken);

		/// <inheritdoc />
		public override async Task<IReadOnlyCollection<RevInfoTestMerge>> RemoveMergedTestMerges(
			IRepository repository,
			RepositorySettings repositorySettings,
			RevisionInformation revisionInformation,
			CancellationToken cancellationToken)
		{
			if (repositorySettings == null)
				throw new ArgumentNullException(nameof(repositorySettings));
			if (revisionInformation == null)
				throw new ArgumentNullException(nameof(revisionInformation));

			if (revisionInformation.ActiveTestMerges?.Any() != true)
			{
				Logger.LogTrace("No test merges to remove.");
				return Array.Empty<RevInfoTestMerge>();
			}

			var remoteInformation = repository.GitRemoteInformation;
			if (remoteInformation == null)
				throw new InvalidOperationException("Remote git info no longer available!");

			var gitHubClient = repositorySettings.AccessToken != null
				? gitHubClientFactory.CreateClient(repositorySettings.AccessToken)
				: gitHubClientFactory.CreateClient();

			var tasks = revisionInformation
				.ActiveTestMerges
				.Select(x => gitHubClient
					.PullRequest
					.Get(
						remoteInformation.RepositoryOwner,
						remoteInformation.RepositoryName,
						x.TestMerge.Number)
					.WithToken(cancellationToken));
			try
			{
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}
			catch (Exception ex) when (!(ex is OperationCanceledException))
			{
				Logger.LogWarning(ex, "Pull requests update check failed!");
			}

			var newList = revisionInformation.ActiveTestMerges.ToList();

			PullRequest? lastMerged = null;
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

		/// <inheritdoc />
		protected override async Task CommentOnTestMergeSource(
			RepositorySettings repositorySettings,
			Api.Models.GitRemoteInformation remoteInformation,
			string comment,
			int testMergeNumber,
			CancellationToken cancellationToken)
		{
			var gitHubClient = gitHubClientFactory.CreateClient(repositorySettings.AccessToken);

			try
			{
				await gitHubClient.Issue.Comment.Create(
					remoteInformation.RepositoryOwner,
					remoteInformation.RepositoryName,
					testMergeNumber,
					comment)
					.WithToken(cancellationToken)
					.ConfigureAwait(false);
			}
			catch (ApiException e)
			{
				Logger.LogWarning(e, "Error posting GitHub comment!");
			}
		}

		/// <inheritdoc />
		protected override string FormatTestMerge(
			RepositorySettings repositorySettings,
			CompileJob compileJob,
			TestMerge testMerge,
			Api.Models.GitRemoteInformation remoteInformation,
			bool updated) => String.Format(
			CultureInfo.InvariantCulture,
			"#### Test Merge {4}{0}{0}##### Server Instance{0}{5}{1}{0}{0}##### Revision{0}Origin: {6}{0}Pull Request: {2}{0}Server: {7}{3}{8}",
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
			compileJob.RevisionInformation.CommitSha,
			compileJob.GitHubDeploymentId.HasValue
				? $"{Environment.NewLine}[GitHub Deployments](https://github.com/{remoteInformation.RepositoryOwner}/{remoteInformation.RepositoryName}/deployments/activity_log?environment=TGS%3A%20{Metadata.Name})"
				: String.Empty);

		/// <summary>
		/// Update the deployment for a given <paramref name="compileJob"/>.
		/// </summary>
		/// <param name="compileJob">The <see cref="CompileJob"/>.</param>
		/// <param name="description">A description of the update.</param>
		/// <param name="deploymentState">The new <see cref="DeploymentState"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
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
				Logger.LogTrace("Not updating deployment as it is missing a repo ID or deployment ID.");
				return;
			}

			Logger.LogTrace("Updating deployment {0} to {1}...", compileJob.GitHubDeploymentId.Value, deploymentState);

			string? gitHubAccessToken = null;
			await databaseContextFactory.UseContext(
				async databaseContext =>
					gitHubAccessToken = await databaseContext
						.RepositorySettings
						.AsQueryable()
						.Where(x => x.InstanceId == Metadata.Id)
						.Select(x => x.AccessToken)
						.FirstAsync(cancellationToken)
						.ConfigureAwait(false))
				.ConfigureAwait(false);

			if (gitHubAccessToken == null)
			{
				Logger.LogWarning(
					"GitHub access token disappeared during deployment, can't update to {state}!",
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
						Description = description,
					})
				.WithToken(cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
