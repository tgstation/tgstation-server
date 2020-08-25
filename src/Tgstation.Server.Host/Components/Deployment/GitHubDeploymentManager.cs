using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <inheritdoc />
	sealed class GitHubDeploymentManager : IGitHubDeploymentManager
	{
		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="GitHubDeploymentManager"/>.
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="GitHubDeploymentManager"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GitHubDeploymentManager"/>.
		/// </summary>
		readonly ILogger<GitHubDeploymentManager> logger;

		/// <summary>
		/// The <see cref="Models.Instance"/> for the <see cref="GitHubDeploymentManager"/>.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubDeploymentManager"/> <see langword="class"/>.
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/>.</param>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="metadata">The value of <see cref="metadata"/>.</param>
		public GitHubDeploymentManager(
			IDatabaseContextFactory databaseContextFactory,
			IGitHubClientFactory gitHubClientFactory,
			ILogger<GitHubDeploymentManager> logger,
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

			if (!repository.IsGitHubRepository)
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

			var gitHubClient = repositorySettings.AccessToken == null
				? gitHubClientFactory.CreateClient()
				: gitHubClientFactory.CreateClient(repositorySettings.AccessToken);

			var repositoryTask = gitHubClient
				.Repository
				.Get(
					repository.GitHubOwner,
					repository.GitHubRepoName);

			if (repositorySettings.CreateGitHubDeployments.Value)
			{
				logger.LogTrace("Creating deployment...");
				var deployment = await gitHubClient
					.Repository
					.Deployment
					.Create(
						repository.GitHubOwner,
						repository.GitHubRepoName,
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
						repository.GitHubOwner,
						repository.GitHubRepoName,
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
			else
				logger.LogTrace("Not creating deployment");

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
	}
}
