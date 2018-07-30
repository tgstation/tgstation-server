using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for managing the <see cref="Repository"/>s
	/// </summary>
	[Route("/" + nameof(Repository))]
	public sealed class RepositoryController : ModelController<Repository>
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="RepositoryController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// The <see cref="Octokit.IGitHubClient"/> for the <see cref="RepositoryController"/>
		/// </summary>
		readonly Octokit.IGitHubClient gitHubClient;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="RepositoryController"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="RepositoryController"/>
		/// </summary>
		readonly ILogger<RepositoryController> logger;

		/// <summary>
		/// Construct a <see cref="RepositoryController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="gitHubClient">The value of <see cref="gitHubClient"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public RepositoryController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager, Octokit.IGitHubClient gitHubClient, IJobManager jobManager, ILogger<RepositoryController> logger) : base(databaseContext, authenticationContextFactory, true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		static string GetAccessString(Api.Models.Internal.RepositorySettings repositorySettings) => repositorySettings.AccessUser != null ? String.Concat(repositorySettings.AccessUser, '@', repositorySettings.AccessToken) : null;

		async Task<bool> LoadRevisionInformation(Components.Repository.IRepository repository, Action<Models.RevisionInformation> revInfoSink, CancellationToken cancellationToken)
		{
			var repoSha = repository.Head;
			var revisionInfo = await DatabaseContext.RevisionInformations.Where(x => x.CommitSha == repoSha)
				.Include(x => x.CompileJobs)
				.Include(x => x.ActiveTestMerges) //minimal info, they can query the rest if they're allowed
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);  //search every rev info because LOL SHA COLLISIONS

			var needsDbUpdate = revisionInfo == default;
			if (needsDbUpdate)
			{
				//needs insertion
				revisionInfo = new Models.RevisionInformation
				{
					CommitSha = repoSha,
					CompileJobs = new List<Models.CompileJob>(),
					ActiveTestMerges = new List<Models.RevInfoTestMerge>()  //non null vals for api returns
				};

				lock (DatabaseContext)	//cleaner this way
					DatabaseContext.RevisionInformations.Add(revisionInfo);
			}
			revInfoSink?.Invoke(revisionInfo);
			return needsDbUpdate;
		}

		async Task<bool> PopulateApi(Repository model, Components.Repository.IRepository repository, string lastOriginCommitSha, Action<Models.RevisionInformation> revInfoSink, CancellationToken cancellationToken)
		{
			model.IsGitHub = repository.IsGitHubRepository;
			model.Origin = repository.Origin;
			model.Reference = repository.Reference;
			model.Sha = repository.Head;

			//rev info stuff
			Models.RevisionInformation revisionInfo = null;
			var needsDbUpdate = await LoadRevisionInformation(repository, x => revisionInfo = x, cancellationToken).ConfigureAwait(false);
			revisionInfo.OriginCommitSha = lastOriginCommitSha ?? model.Sha;
			revInfoSink?.Invoke(revisionInfo);
			model.RevisionInformation = revisionInfo.ToApi();
			return needsDbUpdate;
		}

		/// <inheritdoc />
		[TgsAuthorize(RepositoryRights.SetOrigin)]
		public override async Task<IActionResult> Create([FromBody] Repository model, CancellationToken cancellationToken)
		{
			if (model == null)
				return BadRequest(new { message = "Missing request model!" });

			if (model.Origin == null)
				return BadRequest(new { message = "Missing repo origin!" });

			if (model.AccessUser == null ^ model.AccessToken == null)
				return BadRequest(new { message = "Either both accessToken and accessUser must be present or neither!" });

			var currentModel = await DatabaseContext.RepositorySettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (currentModel == default)
				return StatusCode((int)HttpStatusCode.Gone);

			//normalize github urls
			const string BadGitHubUrl = "://www.github.com/";
			var uiOrigin = model.Origin.ToUpperInvariant();
			var uiBad = BadGitHubUrl.ToUpperInvariant();
			var uiGitHub = Components.Repository.Repository.GitHubUrl.ToUpperInvariant();
			if (uiOrigin.Contains(uiBad))
				model.Origin = uiOrigin.Replace(uiBad, uiGitHub);

			currentModel.AccessToken = model.AccessToken;
			currentModel.AccessUser = model.AccessUser; //intentionally only these fields, user not allowed to change anything else atm
			var cloneBranch = model.Reference;
			var origin = model.Origin;

			var repoManager = instanceManager.GetInstance(Instance).RepositoryManager;

			using (var repo = await repoManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				if (repo == null)
					//clone conflict
					return Conflict();

				var job = new Models.Job
				{
					Description = "Clone repository",
					StartedBy = AuthenticationContext.User,
					CancelRightsType = RightsType.Repository,
					CancelRight = (int)RepositoryRights.CancelClone,
					Instance = Instance
				};
				var api = currentModel.ToApi();
				await jobManager.RegisterOperation(job, async (paramJob, serviceProvider, ct) =>
				{
					using (var repos = await repoManager.CloneRepository(new Uri(origin), cloneBranch, GetAccessString(currentModel), cancellationToken).ConfigureAwait(false))
					{
						if (repos == null)
							throw new Exception("Filesystem conflict while cloning repository!");
						await PopulateApi(api, repo, null, null, cancellationToken).ConfigureAwait(false);
					}
				}, cancellationToken).ConfigureAwait(false);

				api.Origin = model.Origin;
				api.Reference = model.Reference;
				api.IsGitHub = model.Origin.ToUpperInvariant().Contains(uiGitHub);
				api.ActiveJob = job.ToApi();

				return Json(api);
			}
		}

		/// <summary>
		/// Delete the <see cref="Repository"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		[TgsAuthorize(RepositoryRights.Delete)]
		public async Task<IActionResult> Delete(CancellationToken cancellationToken)
		{
			var currentModel = await DatabaseContext.RepositorySettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (currentModel == default)
				return StatusCode((int)HttpStatusCode.Gone);
			
			currentModel.LastOriginCommitSha = null;
			currentModel.AccessToken = null;
			currentModel.AccessUser = null;

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			logger.LogInformation("Instance {0} repository delete initiated by user {1}", Instance.Id, AuthenticationContext.User.Id);

			var job = new Models.Job
			{
				Description = "Delete repository",
				StartedBy = AuthenticationContext.User,
				Instance = Instance
			};
			var api = currentModel.ToApi();
			await jobManager.RegisterOperation(job, (paramJob, serviceProvider, ct) => instanceManager.GetInstance(Instance).RepositoryManager.DeleteRepository(cancellationToken), cancellationToken).ConfigureAwait(false);
			api.ActiveJob = job.ToApi();
			return Ok();
		}

		/// <inheritdoc />
		[TgsAuthorize(RepositoryRights.Read)]
		public override async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var currentModel = await DatabaseContext.RepositorySettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (currentModel == default)
				return StatusCode((int)HttpStatusCode.Gone);

			var api = currentModel.ToApi();

			using (var repo = await instanceManager.GetInstance(Instance).RepositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				if (await PopulateApi(api, repo, currentModel.LastOriginCommitSha, null, cancellationToken).ConfigureAwait(false))
					await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
				return Json(api);
			}
		}

		/// <inheritdoc />
		[TgsAuthorize(RepositoryRights.ChangeAutoUpdateSettings | RepositoryRights.ChangeCommitter | RepositoryRights.ChangeCredentials | RepositoryRights.ChangeTestMergeCommits | RepositoryRights.MergePullRequest | RepositoryRights.SetReference | RepositoryRights.SetSha | RepositoryRights.UpdateBranch)]
		public override async Task<IActionResult> Update([FromBody]Repository model, CancellationToken cancellationToken)
		{
			if (model == null)
				return BadRequest(new { message = "Missing request model!" });

			if (model.AccessUser == null ^ model.AccessToken == null)
				return BadRequest(new { message = "Either both accessToken and accessUser must be present or neither!" });

			if (model.Sha != null && model.Reference != null)
				return BadRequest(new { message = "Only one of sha or reference may be specified!" });

			if(model.Sha != null && model.UpdateReference == true)
				return BadRequest(new { message = "Cannot update a reference when checking out a sha!" });

			if (model.Origin != null)
				return BadRequest(new { message = "origin cannot be modified without deleting the repository!" });

			var newTestMerges = model.NewTestMerges != null && model.NewTestMerges.Count > 0;
			var userRights = (RepositoryRights)AuthenticationContext.GetRight(RightsType.Repository);
			if (newTestMerges && !userRights.HasFlag(RepositoryRights.MergePullRequest))
				return Forbid();

			var currentModel = await DatabaseContext.RepositorySettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (currentModel == default)
				return StatusCode((int)HttpStatusCode.Gone);

			bool CheckModified<T>(Expression<Func<Api.Models.Internal.RepositorySettings, T>> expression, RepositoryRights requiredRight)
			{
				var memberSelectorExpression = (MemberExpression)expression.Body;
				var property = (PropertyInfo)memberSelectorExpression.Member;

				var newVal = property.GetValue(model);
				if (newVal == null)
					return false;
				if (!userRights.HasFlag(requiredRight) && property.GetValue(currentModel) != newVal)
					return true;

				property.SetValue(currentModel, newVal);
				return false;
			};

			if (CheckModified(x => x.AccessToken, RepositoryRights.ChangeCredentials)
				|| CheckModified(x => x.AccessUser, RepositoryRights.ChangeCredentials)
				|| CheckModified(x => x.AutoUpdatesKeepTestMerges, RepositoryRights.ChangeAutoUpdateSettings)
				|| CheckModified(x => x.AutoUpdatesSynchronize, RepositoryRights.ChangeAutoUpdateSettings)
				|| CheckModified(x => x.CommitterEmail, RepositoryRights.ChangeCommitter)
				|| CheckModified(x => x.CommitterName, RepositoryRights.ChangeCommitter)
				|| CheckModified(x => x.PushTestMergeCommits, RepositoryRights.ChangeTestMergeCommits)
				|| CheckModified(x => x.ShowTestMergeCommitters, RepositoryRights.ChangeTestMergeCommits))
				return Forbid();

			//so that's the stuff that is just db, the rest is tricky

			using (var repo = await instanceManager.GetInstance(Instance).RepositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				var startSha = repo.Head;

				if (newTestMerges && !repo.IsGitHubRepository)
					return Conflict(new { message = "Cannot test merge on a non GitHub based repository!" });

				var committerName = currentModel.ShowTestMergeCommitters.Value ? AuthenticationContext.User.Name : currentModel.CommitterName;
				var accessString = GetAccessString(currentModel);
				if (model.UpdateReference == true)
				{
					if (!repo.Tracking && model.Reference == null)
						return Conflict(new { message = "Not on an updatable reference!" });
					if (!userRights.HasFlag(RepositoryRights.UpdateBranch))
						return Forbid();
					await repo.FetchOrigin(accessString, cancellationToken).ConfigureAwait(false);
					if (model.Sha == null && model.Reference == null)
						await repo.MergeOrigin(committerName, currentModel.CommitterEmail, cancellationToken).ConfigureAwait(false);
				}

				if (model.Sha != null || model.Reference != null)
				{
					await repo.CheckoutObject(model.Sha ?? model.Reference, cancellationToken).ConfigureAwait(false);

					if (model.UpdateReference == true && model.Reference != null)
					{
						if (!repo.Tracking)
							return Conflict(new { message = "Checked out reference is does not track an object!" });
						await repo.ResetToOrigin(cancellationToken).ConfigureAwait(false);
					}
				}
				
				//now add any testmerges
				if (newTestMerges)
				{
					var repoOwner = repo.GitHubOwner;
					var repoName = repo.GitHubRepoName;
					var allAddedTestMerges = new List<Models.TestMerge>();
					foreach (var I in model.NewTestMerges)
					{
						var prTask = gitHubClient.PullRequest.Get(repoOwner, repoName, I.Number);

						await repo.AddTestMerge(I.Number, I.PullRequestRevision, committerName, currentModel.CommitterEmail, accessString, cancellationToken).ConfigureAwait(false);

						Octokit.PullRequest pr = null;
						string errorMessage = null;

						Models.RevisionInformation revisionInformation = null;
						var revInfoTask = LoadRevisionInformation(repo, x => revisionInformation = x, cancellationToken);

						try
						{
							pr = await prTask.ConfigureAwait(false);
						}
						catch (Octokit.RateLimitExceededException)
						{
							//you look at your anonymous access and sigh
							errorMessage = "PRE RATE LIMITED";
						}
						catch (Octokit.NotFoundException)
						{
							//you look at your shithub access and sigh
							errorMessage = "PRE NOT FOUND";
						}

						var attachedContextUser = new Models.User
						{
							Id = AuthenticationContext.User.Id
						};
						DatabaseContext.Users.Attach(attachedContextUser);

						var tm = new Models.TestMerge
						{
							Author = pr?.User.Login ?? errorMessage,
							BodyAtMerge = pr?.Body,
							MergedAt = DateTimeOffset.Now,
							TitleAtMerge = pr?.Title ?? errorMessage,
							Comment = I.Comment,
							Number = I.Number,
							MergedBy = attachedContextUser,
							PullRequestRevision = I.PullRequestRevision,
							Url = pr?.HtmlUrl ?? errorMessage
						};
						if (pr == null)
							tm.BodyAtMerge = errorMessage;

						allAddedTestMerges.Add(tm);

						await revInfoTask.ConfigureAwait(false);

						revisionInformation.PrimaryTestMerge = tm;
						revisionInformation.ActiveTestMerges.AddRange(allAddedTestMerges.Select(x => new RevInfoTestMerge
						{
							TestMerge = x
						}));
					}
				}

				var api = currentModel.ToApi();
				if (await PopulateApi(api, repo, currentModel.LastOriginCommitSha, null, cancellationToken).ConfigureAwait(false) || newTestMerges)
					await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

				//synchronize now because we don't care if we fail so its safe to fire/forget the job
				
				if (startSha != repo.Head)
				{
					var job = new Models.Job
					{
						Description = "Synchronize repository changes",
						StartedBy = AuthenticationContext.User,
						Instance = Instance,
						CancelRightsType = RightsType.Repository,
						CancelRight = (int)RepositoryRights.CancelSynchronize,
					};
					await jobManager.RegisterOperation(job, async (paramJob, serviceProvider, ct) =>
					{
						using (var repos = await instanceManager.GetInstance(Instance).RepositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
							if (repos != null)
								await repos.Sychronize(accessString, ct).ConfigureAwait(false);
					}, cancellationToken).ConfigureAwait(false);

					api.ActiveJob = job.ToApi();
				}

				return Json(api);
			}
		}
	}
}
