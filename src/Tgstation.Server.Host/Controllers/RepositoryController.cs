using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
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
		/// Construct a <see cref="RepositoryController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="gitHubClient">The value of <see cref="gitHubClient"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public RepositoryController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager, Octokit.IGitHubClient gitHubClient, IJobManager jobManager, ILogger<RepositoryController> logger) : base(databaseContext, authenticationContextFactory, logger, true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
		}

		static async Task<bool> LoadRevisionInformation(Components.Repository.IRepository repository, IDatabaseContext databaseContext, Models.Instance instance, string lastOriginCommitSha, Action<Models.RevisionInformation> revInfoSink, CancellationToken cancellationToken)
		{
			var repoSha = repository.Head;

			IQueryable<Models.RevisionInformation> queryTarget = databaseContext.RevisionInformations;

			var  revisionInfo = await databaseContext.RevisionInformations.Where(x => x.CommitSha == repoSha && x.Instance.Id == instance.Id)
				.Include(x => x.CompileJobs)
				.Include(x => x.ActiveTestMerges).ThenInclude(x => x.TestMerge) //minimal info, they can query the rest if they're allowed
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);  //search every rev info because LOL SHA COLLISIONS

			if (revisionInfo == default)
				revisionInfo = databaseContext.RevisionInformations.Local.Where(x => x.CommitSha == repoSha).FirstOrDefault();

			var needsDbUpdate = revisionInfo == default;
			if (needsDbUpdate)
			{
				//needs insertion
				revisionInfo = new Models.RevisionInformation
				{
					Instance = instance,
					CommitSha = repoSha,
					CompileJobs = new List<Models.CompileJob>(),
					ActiveTestMerges = new List<RevInfoTestMerge>()  //non null vals for api returns
				};

				lock (databaseContext)	//cleaner this way
					databaseContext.RevisionInformations.Add(revisionInfo);
			}
			revisionInfo.OriginCommitSha = revisionInfo.OriginCommitSha ?? lastOriginCommitSha ?? repository.Head;
			revInfoSink?.Invoke(revisionInfo);
			return needsDbUpdate;
		}

		static async Task<bool> PopulateApi(Repository model, Components.Repository.IRepository repository, IDatabaseContext databaseContext, Models.Instance instance, string lastOriginCommitSha, Action<Models.RevisionInformation> revInfoSink, CancellationToken cancellationToken)
		{
			model.IsGitHub = repository.IsGitHubRepository;
			model.Origin = repository.Origin;
			model.Reference = repository.Reference;

			//rev info stuff
			Models.RevisionInformation revisionInfo = null;
			var needsDbUpdate = await LoadRevisionInformation(repository, databaseContext, instance, lastOriginCommitSha, x => revisionInfo = x, cancellationToken).ConfigureAwait(false);
			model.RevisionInformation = revisionInfo.ToApi();
			revInfoSink?.Invoke(revisionInfo);
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
				model.Origin = uiOrigin.Replace(uiBad, uiGitHub, StringComparison.Ordinal);

			currentModel.AccessToken = model.AccessToken;
			currentModel.AccessUser = model.AccessUser; //intentionally only these fields, user not allowed to change anything else atm
			var cloneBranch = model.Reference;
			var origin = model.Origin;

			var repoManager = instanceManager.GetInstance(Instance).RepositoryManager;

			using (var repo = await repoManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				if (repo != null)
					//clone conflict
					return Conflict();

				var job = new Models.Job
				{
					Description = String.Format(CultureInfo.InvariantCulture, "Clone branch {1} of repository {0}", origin, cloneBranch ?? "master"),
					StartedBy = AuthenticationContext.User,
					CancelRightsType = RightsType.Repository,
					CancelRight = (int)RepositoryRights.CancelClone,
					Instance = Instance
				};
				var api = currentModel.ToApi();
				await jobManager.RegisterOperation(job, async (paramJob, serviceProvider, progressReporter, ct) =>
				{
					using (var repos = await repoManager.CloneRepository(new Uri(origin), cloneBranch, currentModel.GetAccessString(), progressReporter, cancellationToken).ConfigureAwait(false))
					{
						if (repos == null)
							throw new Exception("Filesystem conflict while cloning repository!");
						await PopulateApi(api, repo, DatabaseContext, Instance, null, null, cancellationToken).ConfigureAwait(false);
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

			currentModel.AccessToken = null;
			currentModel.AccessUser = null;

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			Logger.LogInformation("Instance {0} repository delete initiated by user {1}", Instance.Id, AuthenticationContext.User.Id);

			var job = new Models.Job
			{
				Description = "Delete repository",
				StartedBy = AuthenticationContext.User,
				Instance = Instance
			};
			var api = currentModel.ToApi();
			await jobManager.RegisterOperation(job, (paramJob, serviceProvider, progressReporter, ct) => instanceManager.GetInstance(Instance).RepositoryManager.DeleteRepository(cancellationToken), cancellationToken).ConfigureAwait(false);
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
				if (repo != null && await PopulateApi(api, repo, DatabaseContext, Instance, null, null, cancellationToken).ConfigureAwait(false))
					//user may have fucked with the repo without telling us, do what we can
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

			if (model.CheckoutSha != null && model.Reference != null)
				return BadRequest(new { message = "Only one of sha or reference may be specified!" });

			if (model.CheckoutSha != null && model.UpdateFromOrigin == true)
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
				|| CheckModified(x => x.ShowTestMergeCommitters, RepositoryRights.ChangeTestMergeCommits)
				|| (model.UpdateFromOrigin == true && !userRights.HasFlag(RepositoryRights.UpdateBranch)))
				return Forbid();

			//no, just one save changes at the end for sanity
			//await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			var job = new Models.Job
			{
				Description = "Apply repository changes",
				StartedBy = AuthenticationContext.User,
				Instance = Instance,
				CancelRightsType = RightsType.Repository,
				CancelRight = (int)RepositoryRights.CancelPendingChanges,
			};

			await jobManager.RegisterOperation(job, async (paramJob, serviceProvider, progressReporter, ct) =>
			{
				using (var repo = await instanceManager.GetInstance(Instance).RepositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
				{
					if (repo == null)
						throw new InvalidOperationException("Repository could not be loaded!");

					var modelHasShaOrReference = model.CheckoutSha != null || model.Reference != null;

					var startReference = repo.Reference;
					var startSha = repo.Head;

					if (newTestMerges && !repo.IsGitHubRepository)
						throw new InvalidOperationException("Cannot test merge on a non GitHub based repository!");

					var committerName = currentModel.ShowTestMergeCommitters.Value ? AuthenticationContext.User.Name : currentModel.CommitterName;
					var accessString = currentModel.GetAccessString();

					var numFetches = (model.NewTestMerges?.Count ?? 0) + (model.UpdateFromOrigin == true ? 1 : 0);
					var doneFetches = 0;
					if (numFetches > 0)
						progressReporter(0);

					//get a base line for where we are
					Models.RevisionInformation lastRevisionInfo = null;

					var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
					var attachedInstance = new Models.Instance
					{
						Id = Instance.Id
					};
					databaseContext.Instances.Attach(attachedInstance);

					await LoadRevisionInformation(repo, databaseContext, attachedInstance, null, x => lastRevisionInfo = x, cancellationToken).ConfigureAwait(false);

					//apply new rev info, tracking applied test merges
					async Task UpdateRevInfo()
					{
						var last = lastRevisionInfo;
						await LoadRevisionInformation(repo, databaseContext, attachedInstance, last.OriginCommitSha, x => lastRevisionInfo = x, cancellationToken).ConfigureAwait(false);
						lastRevisionInfo.ActiveTestMerges.AddRange(last.ActiveTestMerges);
					};

					try
					{
						//fetch/pull
						if (model.UpdateFromOrigin == true)
						{
							if (!repo.Tracking && model.Reference == null)
								throw new InvalidOperationException("Not on an updatable reference!");
							await repo.FetchOrigin(accessString, x => progressReporter(x / numFetches), cancellationToken).ConfigureAwait(false);
							doneFetches = 1;
							if (!modelHasShaOrReference)
							{
								var fastForward = await repo.MergeOrigin(committerName, currentModel.CommitterEmail, cancellationToken).ConfigureAwait(false);
								if (!fastForward.HasValue)
									throw new InvalidOperationException("Merge conflict occurred during origin update!");
								await UpdateRevInfo().ConfigureAwait(false);
								if (fastForward.Value)
									lastRevisionInfo.OriginCommitSha = repo.Head;
							}
						}

						//checkout/hard reset
						if (modelHasShaOrReference)
						{
							if ((model.CheckoutSha != null && repo.Head.ToUpperInvariant() != model.CheckoutSha.ToUpperInvariant())
								|| (model.Reference != null && repo.Reference != model.Reference))
							{
								await repo.CheckoutObject(model.CheckoutSha ?? model.Reference, cancellationToken).ConfigureAwait(false);
								await LoadRevisionInformation(repo, databaseContext, attachedInstance, null, x => lastRevisionInfo = x, cancellationToken).ConfigureAwait(false);  //we've either seen origin before or what we're checking out is on origin
							}

							if (model.UpdateFromOrigin == true && model.Reference != null)
							{
								if (!repo.Tracking)
									throw new InvalidOperationException("Checked out reference does not track a remote object!");
								await repo.ResetToOrigin(cancellationToken).ConfigureAwait(false);
								await LoadRevisionInformation(repo, databaseContext, attachedInstance, null, x => lastRevisionInfo = x, cancellationToken).ConfigureAwait(false);
								//repo head is on origin so force this
								//will update the db if necessary
								lastRevisionInfo.OriginCommitSha = repo.Head;
							}
						}

						//test merging
						if (newTestMerges)
						{

							var contextUser = new Models.User
							{
								Id = AuthenticationContext.User.Id
							};
							databaseContext.Users.Attach(contextUser);

							var repoOwner = repo.GitHubOwner;
							var repoName = repo.GitHubRepoName;
							foreach (var I in model.NewTestMerges)
							{
								var prTask = gitHubClient.PullRequest.Get(repoOwner, repoName, I.Number);

								var mergeResult = await repo.AddTestMerge(I.Number, I.PullRequestRevision, committerName, currentModel.CommitterEmail, accessString, x => progressReporter((x + 100 * doneFetches) / numFetches), cancellationToken).ConfigureAwait(false);

								if (!mergeResult.HasValue)	//conflict, we don't care, dd already knows
									continue;

								++doneFetches;

								var revInfoUpdateTask = UpdateRevInfo();

								Octokit.PullRequest pr = null;
								string errorMessage = null;
								try
								{
									pr = await prTask.ConfigureAwait(false);
								}
								catch (Octokit.RateLimitExceededException)
								{
									//you look at your anonymous access and sigh
									errorMessage = "P.R.E. RATE LIMITED";
								}
								catch (Octokit.NotFoundException)
								{
									//you look at your shithub access and sigh
									errorMessage = "P.R.E. NOT FOUND";
								}

								var tm = new Models.TestMerge
								{
									Author = pr?.User.Login ?? errorMessage,
									BodyAtMerge = pr?.Body ?? errorMessage ?? String.Empty,
									MergedAt = DateTimeOffset.Now,
									TitleAtMerge = pr?.Title ?? errorMessage ?? String.Empty,
									Comment = I.Comment,
									Number = I.Number,
									MergedBy = contextUser,
									PullRequestRevision = I.PullRequestRevision,
									Url = pr?.HtmlUrl ?? errorMessage
								};

								await revInfoUpdateTask.ConfigureAwait(false);

								lastRevisionInfo.PrimaryTestMerge = tm;
								lastRevisionInfo.ActiveTestMerges.Add(new RevInfoTestMerge
								{
									TestMerge = tm
								});
							}
						}

						if (startSha != repo.Head)
						{
							await repo.Sychronize(accessString, ct).ConfigureAwait(false);
							await UpdateRevInfo().ConfigureAwait(false);
						}
						await databaseContext.Save(cancellationToken).ConfigureAwait(false);
					}
					catch
					{
						//the stuff didn't make it into the db, forget what we've done and abort
						await repo.CheckoutObject(startReference ?? startSha, default).ConfigureAwait(false);
						if (startReference != null && repo.Head != startSha)
							await repo.ResetToSha(startSha, default).ConfigureAwait(false);
						throw;
					}
				}
			}, cancellationToken).ConfigureAwait(false);

			return Json(job.ToApi());
		}
	}
}
