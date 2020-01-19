using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing the <see cref="Repository"/>s
	/// </summary>
	[Route(Routes.Repository)]
	#pragma warning disable CA1506 // TODO: Decomplexify
	public sealed class RepositoryController : ApiController
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="RepositoryController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="RepositoryController"/>
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="RepositoryController"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="RepositoryController"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Construct a <see cref="RepositoryController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="generalConfiguration"/></param>
		public RepositoryController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager, IGitHubClientFactory gitHubClientFactory, IJobManager jobManager, ILogger<RepositoryController> logger, IOptions<GeneralConfiguration> generalConfigurationOptions) : base(databaseContext, authenticationContextFactory, logger, true, true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		async Task<bool> LoadRevisionInformation(Components.Repository.IRepository repository, IDatabaseContext databaseContext, Models.Instance instance, string lastOriginCommitSha, Action<Models.RevisionInformation> revInfoSink, CancellationToken cancellationToken)
		{
			var repoSha = repository.Head;

			IQueryable<Models.RevisionInformation> ApplyQuery(IQueryable<Models.RevisionInformation> query) => query
				.Where(x => x.CommitSha == repoSha && x.Instance.Id == instance.Id)
				.Include(x => x.CompileJobs)
				.Include(x => x.ActiveTestMerges).ThenInclude(x => x.TestMerge).ThenInclude(x => x.MergedBy);

			var revisionInfo = await ApplyQuery(databaseContext.RevisionInformations).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (revisionInfo == default)
				revisionInfo = databaseContext.RevisionInformations.Local.Where(x => x.CommitSha == repoSha && x.Instance.Id == instance.Id).FirstOrDefault();

			var needsDbUpdate = revisionInfo == default;
			if (needsDbUpdate)
			{
				// needs insertion
				revisionInfo = new Models.RevisionInformation
				{
					Instance = instance,
					CommitSha = repoSha,
					CompileJobs = new List<Models.CompileJob>(),
					ActiveTestMerges = new List<RevInfoTestMerge>() // non null vals for api returns
				};

				lock (databaseContext) // cleaner this way
					databaseContext.RevisionInformations.Add(revisionInfo);
			}

			revisionInfo.OriginCommitSha = revisionInfo.OriginCommitSha ?? lastOriginCommitSha;
			if (revisionInfo.OriginCommitSha == null)
			{
				revisionInfo.OriginCommitSha = repoSha;
				Logger.LogWarning(Components.Repository.Repository.OriginTrackingErrorTemplate, repoSha);
			}

			revInfoSink?.Invoke(revisionInfo);
			return needsDbUpdate;
		}

		async Task<bool> PopulateApi(Repository model, Components.Repository.IRepository repository, IDatabaseContext databaseContext, Models.Instance instance, CancellationToken cancellationToken)
		{
			if (repository.IsGitHubRepository)
			{
				model.GitHubOwner = repository.GitHubOwner;
				model.GitHubName = repository.GitHubRepoName;
			}

			model.Origin = repository.Origin;
			model.Reference = repository.Reference;

			// rev info stuff
			Models.RevisionInformation revisionInfo = null;
			var needsDbUpdate = await LoadRevisionInformation(repository, databaseContext, instance, null, x => revisionInfo = x, cancellationToken).ConfigureAwait(false);
			model.RevisionInformation = revisionInfo.ToApi();
			return needsDbUpdate;
		}

		/// <summary>
		/// Begin cloning the repository if it doesn't exist.
		/// </summary>
		/// <param name="model">Initial <see cref="Repository"/> settings.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="201">The <see cref="Repository"/> was created successfully and the <see cref="Api.Models.Job"/> to clone it has begun.</response>
		/// <response code="410">Instance no longer available.</response>
		[HttpPut]
		[TgsAuthorize(RepositoryRights.SetOrigin)]
		[ProducesResponseType(typeof(Repository), 201)]
		[ProducesResponseType(410)]
		public async Task<IActionResult> Create([FromBody] Repository model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.Origin == null)
				return BadRequest(new ErrorMessage { Message = "Missing repo origin!" });

			if (model.AccessUser == null ^ model.AccessToken == null)
				return BadRequest(new ErrorMessage { Message = "Either both accessToken and accessUser must be present or neither!" });

			var currentModel = await DatabaseContext.RepositorySettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (currentModel == default)
				return StatusCode((int)HttpStatusCode.Gone);

			// normalize github urls
			const string BadGitHubUrl = "://www.github.com/";
			var uiOrigin = model.Origin.ToUpperInvariant();
			var uiBad = BadGitHubUrl.ToUpperInvariant();
			var uiGitHub = Components.Repository.Repository.GitHubUrl.ToUpperInvariant();
			if (uiOrigin.Contains(uiBad, StringComparison.Ordinal))
				model.Origin = uiOrigin.Replace(uiBad, uiGitHub, StringComparison.Ordinal);

			currentModel.AccessToken = model.AccessToken;
			currentModel.AccessUser = model.AccessUser; // intentionally only these fields, user not allowed to change anything else atm
			var cloneBranch = model.Reference;
			var origin = model.Origin;

			var repoManager = instanceManager.GetInstance(Instance).RepositoryManager;

			if (repoManager.CloneInProgress)
				return Conflict(new ErrorMessage
				{
					Message = "A clone operation is in progress!"
				});

			if(repoManager.InUse)
				return Conflict(new ErrorMessage
				{
					Message = "The repo is busy!"
				});

			using (var repo = await repoManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				// clone conflict
				if (repo != null)
					return Conflict(new ErrorMessage
					{
						Message = "The repository already exists!"
					});

				var job = new Models.Job
				{
					Description = String.Format(CultureInfo.InvariantCulture, "Clone branch {1} of repository {0}", origin, cloneBranch ?? "master"),
					StartedBy = AuthenticationContext.User,
					CancelRightsType = RightsType.Repository,
					CancelRight = (ulong)RepositoryRights.CancelClone,
					Instance = Instance
				};
				var api = currentModel.ToApi();
				await jobManager.RegisterOperation(job, async (paramJob, databaseContext, progressReporter, ct) =>
				{
					using (var repos = await repoManager.CloneRepository(new Uri(origin), cloneBranch, currentModel.AccessUser, currentModel.AccessToken, progressReporter, ct).ConfigureAwait(false))
					{
						if (repos == null)
							throw new JobException("Filesystem conflict while cloning repository!");
						var instance = new Models.Instance
						{
							Id = Instance.Id
						};
						databaseContext.Instances.Attach(instance);
						if (await PopulateApi(api, repos, databaseContext, instance, ct).ConfigureAwait(false))
							await databaseContext.Save(ct).ConfigureAwait(false);
					}
				}, cancellationToken).ConfigureAwait(false);

				api.Origin = model.Origin;
				api.Reference = model.Reference;
				api.ActiveJob = job.ToApi();

				return StatusCode((int)HttpStatusCode.Created, api);
			}
		}

		/// <summary>
		/// Delete the <see cref="Repository"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		/// <response code="202">Job to delete the repository created successfully.</response>
		/// <response code="410">Instance no longer available.</response>
		[HttpDelete("{id}")]
		[TgsAuthorize(RepositoryRights.Delete)]
		[ProducesResponseType(typeof(Repository), 202)]
		[ProducesResponseType(410)]
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
			await jobManager.RegisterOperation(job, (paramJob, databaseContext, progressReporter, ct) => instanceManager.GetInstance(Instance).RepositoryManager.DeleteRepository(cancellationToken), cancellationToken).ConfigureAwait(false);
			api.ActiveJob = job.ToApi();
			return Accepted(api);
		}

		/// <summary>
		/// Get <see cref="Repository"/> status.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Retrieved the <see cref="Repository"/> settings successfully.</response>
		/// <response code="201">Retrieved the <see cref="Repository"/> settings successfully, though they did not previously exist.</response>
		/// <response code="410">Instance no longer available.</response>
		[HttpGet]
		[TgsAuthorize(RepositoryRights.Read)]
		[ProducesResponseType(typeof(Repository), 200)]
		[ProducesResponseType(typeof(Repository), 201)]
		[ProducesResponseType(410)]
		public async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var currentModel = await DatabaseContext.RepositorySettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (currentModel == default)
				return StatusCode((int)HttpStatusCode.Gone);

			var api = currentModel.ToApi();
			var repoManager = instanceManager.GetInstance(Instance).RepositoryManager;

			if (repoManager.CloneInProgress)
				return Conflict(new ErrorMessage
				{
					Message = "A clone operation is in progress!"
				});

			if (repoManager.InUse)
				return Conflict(new ErrorMessage
				{
					Message = "The repo is busy!"
				});

			using (var repo = await repoManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				if (repo != null && await PopulateApi(api, repo, DatabaseContext, Instance, cancellationToken).ConfigureAwait(false))
				{
					// user may have fucked with the repo manually, do what we can
					await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
					return StatusCode((int)HttpStatusCode.Created, api);
				}

				return Json(api);
			}
		}

		/// <summary>
		/// Perform updats to the <see cref="Repository"/>.
		/// </summary>
		/// <param name="model">The updated <see cref="Repository"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Updated the <see cref="Repository"/> settings successfully.</response>
		/// <response code="201">Updated the <see cref="Repository"/> settings successfully and a <see cref="Api.Models.Job"/> was created to make the requested git changes.</response>
		/// <response code="410">Instance no longer available.</response>
		[HttpPost]
		[TgsAuthorize(RepositoryRights.ChangeAutoUpdateSettings | RepositoryRights.ChangeCommitter | RepositoryRights.ChangeCredentials | RepositoryRights.ChangeTestMergeCommits | RepositoryRights.MergePullRequest | RepositoryRights.SetReference | RepositoryRights.SetSha | RepositoryRights.UpdateBranch)]
		[ProducesResponseType(typeof(Repository), 200)]
		[ProducesResponseType(typeof(Repository), 202)]
		[ProducesResponseType(410)]
		#pragma warning disable CA1502 // TODO: Decomplexify
		#pragma warning disable CA1505
		public async Task<IActionResult> Update([FromBody]Repository model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.AccessUser == null ^ model.AccessToken == null)
				return BadRequest(new ErrorMessage { Message = "Either both accessToken and accessUser must be present or neither!" });

			if (model.CheckoutSha != null && model.Reference != null)
				return BadRequest(new ErrorMessage { Message = "Only one of sha or reference may be specified!" });

			if (model.CheckoutSha != null && model.UpdateFromOrigin == true)
				return BadRequest(new ErrorMessage { Message = "Cannot update a reference when checking out a sha!" });

			if (model.Origin != null)
				return BadRequest(new ErrorMessage { Message = "origin cannot be modified without deleting the repository!" });

			if (model.NewTestMerges?.Any(x => !x.Number.HasValue) == true)
				return BadRequest(new ErrorMessage { Message = "All new test merges must provide a number!" });

			if (model.NewTestMerges?.Any(x => model.NewTestMerges.Any(y => x != y && x.Number == y.Number)) == true)
				return BadRequest(new ErrorMessage { Message = "Cannot test merge the same PR twice in one job!" });

			if (model.CommitterName?.Length == 0)
				return BadRequest(new ErrorMessage { Message = "Cannot set empty committer name!" });

			if (model.CommitterEmail?.Length == 0)
				return BadRequest(new ErrorMessage { Message = "Cannot set empty committer e=mail!" });

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
			}

			if (CheckModified(x => x.AccessToken, RepositoryRights.ChangeCredentials)
				|| CheckModified(x => x.AccessUser, RepositoryRights.ChangeCredentials)
				|| CheckModified(x => x.AutoUpdatesKeepTestMerges, RepositoryRights.ChangeAutoUpdateSettings)
				|| CheckModified(x => x.AutoUpdatesSynchronize, RepositoryRights.ChangeAutoUpdateSettings)
				|| CheckModified(x => x.CommitterEmail, RepositoryRights.ChangeCommitter)
				|| CheckModified(x => x.CommitterName, RepositoryRights.ChangeCommitter)
				|| CheckModified(x => x.PushTestMergeCommits, RepositoryRights.ChangeTestMergeCommits)
				|| CheckModified(x => x.ShowTestMergeCommitters, RepositoryRights.ChangeTestMergeCommits)
				|| CheckModified(x => x.PostTestMergeComment, RepositoryRights.ChangeTestMergeCommits)
				|| (model.UpdateFromOrigin == true && !userRights.HasFlag(RepositoryRights.UpdateBranch)))
				return Forbid();

			if (currentModel.AccessToken?.Length == 0 && currentModel.AccessUser?.Length == 0)
			{
				// setting an empty string clears everything
				currentModel.AccessUser = null;
				currentModel.AccessToken = null;
			}

			var canRead = userRights.HasFlag(RepositoryRights.Read);

			var api = canRead ? currentModel.ToApi() : new Repository();
			var repoManager = instanceManager.GetInstance(Instance).RepositoryManager;

			if (canRead)
			{
				if (repoManager.CloneInProgress)
					return Conflict(new ErrorMessage
					{
						Message = "A clone operation is in progress!"
					});

				if (repoManager.InUse)
					return Conflict(new ErrorMessage
					{
						Message = "The repo is busy!"
					});

				using (var repo = await repoManager.LoadRepository(cancellationToken).ConfigureAwait(false))
				{
					if (repo == null)
						return Conflict(new ErrorMessage
						{
							Message = "Repository could not be loaded!"
						});
					await PopulateApi(api, repo, DatabaseContext, Instance, cancellationToken).ConfigureAwait(false);
				}
			}

			// this is just db stuf so stow it away
			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			// format the job description
			string description = null;
			if (model.UpdateFromOrigin == true)
				if (model.Reference != null)
					description = String.Format(CultureInfo.InvariantCulture, "Fetch and hard reset repository to origin/{0}", model.Reference);
				else if (model.CheckoutSha != null)
					description = String.Format(CultureInfo.InvariantCulture, "Fetch and checkout {0} in repository", model.CheckoutSha);
				else
					description = "Pull current repository reference";
			else if (model.Reference != null || model.CheckoutSha != null)
				description = String.Format(CultureInfo.InvariantCulture, "Checkout repository {0} {1}", model.Reference != null ? "reference" : "SHA", model.Reference ?? model.CheckoutSha);

			if (newTestMerges)
				description = String.Format(CultureInfo.InvariantCulture, "{0}est merge pull request(s) {1}{2}",
					description != null ? String.Format(CultureInfo.InvariantCulture, "{0} and t", description) : "T",
					String.Join(", ", model.NewTestMerges.Select(x =>
					String.Format(CultureInfo.InvariantCulture, "#{0}{1}", x.Number,
					x.PullRequestRevision != null ? String.Format(CultureInfo.InvariantCulture, " at {0}", x.PullRequestRevision.Substring(0, 7)) : String.Empty))),
					description != null ? String.Empty : " in repository");

			if (description == null)
				return Json(api); // no git changes

			var job = new Models.Job
			{
				Description = description,
				StartedBy = AuthenticationContext.User,
				Instance = Instance,
				CancelRightsType = RightsType.Repository,
				CancelRight = (ulong)RepositoryRights.CancelPendingChanges,
			};

			await jobManager.RegisterOperation(job, async (paramJob, databaseContext, progressReporter, ct) =>
			{
				using (var repo = await repoManager.LoadRepository(ct).ConfigureAwait(false))
				{
					if (repo == null)
						throw new JobException("Repository could not be loaded!");

					var modelHasShaOrReference = model.CheckoutSha != null || model.Reference != null;

					var startReference = repo.Reference;
					var startSha = repo.Head;
					string postUpdateSha = null;

					if (newTestMerges && !repo.IsGitHubRepository)
						throw new JobException("Cannot test merge on a non GitHub based repository!");

					var committerName = currentModel.ShowTestMergeCommitters.Value ? AuthenticationContext.User.Name : currentModel.CommitterName;

					var hardResettingToOriginReference = model.UpdateFromOrigin == true && model.Reference != null;

					var numSteps = (model.NewTestMerges?.Count ?? 0) + (model.UpdateFromOrigin == true ? 1 : 0) + (!modelHasShaOrReference ? 2 : (hardResettingToOriginReference ? 3 : 1));
					var doneSteps = 0;

					Action<int> NextProgressReporter()
					{
						var tmpDoneSteps = doneSteps;
						++doneSteps;
						return progress => progressReporter((progress + (100 * tmpDoneSteps)) / numSteps);
					}

					progressReporter(0);

					// get a base line for where we are
					Models.RevisionInformation lastRevisionInfo = null;

					var attachedInstance = new Models.Instance
					{
						Id = Instance.Id
					};
					databaseContext.Instances.Attach(attachedInstance);

					await LoadRevisionInformation(repo, databaseContext, attachedInstance, null, x => lastRevisionInfo = x, ct).ConfigureAwait(false);

					// apply new rev info, tracking applied test merges
					async Task UpdateRevInfo()
					{
						var last = lastRevisionInfo;
						await LoadRevisionInformation(repo, databaseContext, attachedInstance, last.OriginCommitSha, x => lastRevisionInfo = x, ct).ConfigureAwait(false);
						lastRevisionInfo.ActiveTestMerges.AddRange(last.ActiveTestMerges);
					}

					try
					{
						// fetch/pull
						if (model.UpdateFromOrigin == true)
						{
							if (!repo.Tracking)
								throw new JobException("Not on an updatable reference!");
							await repo.FetchOrigin(currentModel.AccessUser, currentModel.AccessToken, NextProgressReporter(), ct).ConfigureAwait(false);
							doneSteps = 1;
							if (!modelHasShaOrReference)
							{
								var fastForward = await repo.MergeOrigin(committerName, currentModel.CommitterEmail, NextProgressReporter(), ct).ConfigureAwait(false);
								if (!fastForward.HasValue)
									throw new JobException("Merge conflict occurred during origin update!");
								await UpdateRevInfo().ConfigureAwait(false);
								if (fastForward.Value)
								{
									lastRevisionInfo.OriginCommitSha = repo.Head;
									await repo.Sychronize(currentModel.AccessUser, currentModel.AccessToken, currentModel.CommitterName, currentModel.CommitterEmail, NextProgressReporter(), true, ct).ConfigureAwait(false);
									postUpdateSha = repo.Head;
								}
								else
									NextProgressReporter()(100);
							}
						}

						// checkout/hard reset
						if (modelHasShaOrReference)
						{
							if ((model.CheckoutSha != null && repo.Head.ToUpperInvariant().StartsWith(model.CheckoutSha.ToUpperInvariant(), StringComparison.Ordinal))
								|| (model.Reference != null && repo.Reference.ToUpperInvariant() != model.Reference.ToUpperInvariant()))
							{
								var committish = model.CheckoutSha ?? model.Reference;
								var isSha = await repo.IsSha(committish, cancellationToken).ConfigureAwait(false);

								if ((isSha && model.Reference != null) || (!isSha && model.CheckoutSha != null))
									throw new JobException("Attempted to checkout a SHA or reference that was actually the opposite!");

								await repo.CheckoutObject(committish, NextProgressReporter(), ct).ConfigureAwait(false);
								await LoadRevisionInformation(repo, databaseContext, attachedInstance, null, x => lastRevisionInfo = x, ct).ConfigureAwait(false); // we've either seen origin before or what we're checking out is on origin
							}
							else
								NextProgressReporter()(100);

							if (hardResettingToOriginReference)
							{
								if (!repo.Tracking)
									throw new JobException("Checked out reference does not track a remote object!");
								await repo.ResetToOrigin(NextProgressReporter(), ct).ConfigureAwait(false);
								await repo.Sychronize(currentModel.AccessUser, currentModel.AccessToken, currentModel.CommitterName, currentModel.CommitterEmail, NextProgressReporter(), true, ct).ConfigureAwait(false);
								await LoadRevisionInformation(repo, databaseContext, attachedInstance, null, x => lastRevisionInfo = x, ct).ConfigureAwait(false);

								// repo head is on origin so force this
								// will update the db if necessary
								lastRevisionInfo.OriginCommitSha = repo.Head;
							}
						}

						// test merging
						Dictionary<int, Octokit.PullRequest> prMap = null;
						if (newTestMerges)
						{
							// bit of sanitization
							foreach (var I in model.NewTestMerges.Where(x => String.IsNullOrWhiteSpace(x.PullRequestRevision)))
								I.PullRequestRevision = null;

							var gitHubClient = currentModel.AccessToken != null
							? gitHubClientFactory.CreateClient(currentModel.AccessToken)
							: (String.IsNullOrEmpty(generalConfiguration.GitHubAccessToken)
							? gitHubClientFactory.CreateClient()
							: gitHubClientFactory.CreateClient(generalConfiguration.GitHubAccessToken));

							var repoOwner = repo.GitHubOwner;
							var repoName = repo.GitHubRepoName;

							// optimization: if we've already merged these exact same commits in this fashion before, just find the rev info for it and check it out
							Models.RevisionInformation revInfoWereLookingFor = null;
							bool needToApplyRemainingPrs = true;
							if (lastRevisionInfo.OriginCommitSha == lastRevisionInfo.CommitSha)
							{
								// In order for this to work though we need the shas of all the commits
								if (model.NewTestMerges.Any(x => x.PullRequestRevision == null))
									prMap = new Dictionary<int, Octokit.PullRequest>();

								bool cantSearch = false;
								foreach (var I in model.NewTestMerges)
								{
									if (I.PullRequestRevision != null)
#pragma warning disable CA1308 // Normalize strings to uppercase
										I.PullRequestRevision = I.PullRequestRevision?.ToLowerInvariant(); // ala libgit2
#pragma warning restore CA1308 // Normalize strings to uppercase
									else
										try
										{
											// retrieve the latest sha
											var pr = await gitHubClient.PullRequest.Get(repoOwner, repoName, I.Number.Value).ConfigureAwait(false);
											prMap.Add(I.Number.Value, pr);
											I.PullRequestRevision = pr.Head.Sha;
										}
										catch
										{
											cantSearch = true;
											break;
										}
								}

								if (!cantSearch)
								{
									var dbPull = await databaseContext.RevisionInformations
										.Where(x => x.Instance.Id == Instance.Id
										&& x.OriginCommitSha == lastRevisionInfo.OriginCommitSha
										&& x.ActiveTestMerges.Count <= model.NewTestMerges.Count
										&& x.ActiveTestMerges.Count > 0)
										.Include(x => x.ActiveTestMerges)
										.ThenInclude(x => x.TestMerge)
										.ToListAsync(cancellationToken).ConfigureAwait(false);

									// split here cause this bit has to be done locally
									revInfoWereLookingFor = dbPull
										.Where(x => x.ActiveTestMerges.Count == model.NewTestMerges.Count
										&& x.ActiveTestMerges.Select(y => y.TestMerge)
										.All(y => model.NewTestMerges.Any(z =>
										y.Number == z.Number
										&& y.PullRequestRevision.StartsWith(z.PullRequestRevision, StringComparison.Ordinal)
										&& (y.Comment?.Trim().ToUpperInvariant() == z.Comment?.Trim().ToUpperInvariant() || z.Comment == null))))
										.FirstOrDefault();

									if (revInfoWereLookingFor == null && model.NewTestMerges.Count > 1)
									{
										// okay try to add at least SOME prs we've seen before
										var search = model.NewTestMerges.ToList();

										var appliedTestMergeIds = new List<long>();

										Models.RevisionInformation lastGoodRevInfo = null;
										do
										{
											foreach (var I in search)
											{
												revInfoWereLookingFor = dbPull
													.Where(x => model.NewTestMerges.Any(z =>
													x.PrimaryTestMerge.Number == z.Number
													&& x.PrimaryTestMerge.PullRequestRevision.StartsWith(z.PullRequestRevision, StringComparison.Ordinal)
													&& (x.PrimaryTestMerge.Comment?.Trim().ToUpperInvariant() == z.Comment?.Trim().ToUpperInvariant() || z.Comment == null))
													&& x.ActiveTestMerges.Select(y => y.TestMerge).All(y => appliedTestMergeIds.Contains(y.Id)))
													.FirstOrDefault();

												if (revInfoWereLookingFor != null)
												{
													lastGoodRevInfo = revInfoWereLookingFor;
													appliedTestMergeIds.Add(revInfoWereLookingFor.PrimaryTestMerge.Id);
													search.Remove(I);
													break;
												}
											}
										}
										while (revInfoWereLookingFor != null && search.Count > 0);

										revInfoWereLookingFor = lastGoodRevInfo;
										needToApplyRemainingPrs = search.Count != 0;
										if (needToApplyRemainingPrs)
											model.NewTestMerges = search;
									}
									else if (revInfoWereLookingFor != null)
										needToApplyRemainingPrs = false;
								}
							}

							if (revInfoWereLookingFor != null)
							{
								// goteem
								await repo.ResetToSha(revInfoWereLookingFor.CommitSha, NextProgressReporter(), cancellationToken).ConfigureAwait(false);
								lastRevisionInfo = revInfoWereLookingFor;
							}

							if (needToApplyRemainingPrs)
							{
								// an invocation of LoadRevisionInformation could have already loaded this user
								var contextUser = databaseContext.Users.Local.Where(x => x.Id == AuthenticationContext.User.Id).FirstOrDefault();
								if (contextUser == default)
								{
									contextUser = new Models.User
									{
										Id = AuthenticationContext.User.Id
									};
									databaseContext.Users.Attach(contextUser);
								}
								else
									Logger.LogTrace("Skipping attaching the user to the database context as it is already loaded!");

								foreach (var I in model.NewTestMerges)
								{
									Octokit.PullRequest pr = null;
									string errorMessage = null;

									if (lastRevisionInfo.ActiveTestMerges.Any(x => x.TestMerge.Number == I.Number.Value))
										throw new JobException("Cannot test merge the same PR twice in one HEAD!");

									try
									{
										// load from cache if possible
										if (prMap == null || !prMap.TryGetValue(I.Number.Value, out pr))
											pr = await gitHubClient.PullRequest.Get(repoOwner, repoName, I.Number.Value).ConfigureAwait(false);
									}
									catch (Octokit.RateLimitExceededException)
									{
										// you look at your anonymous access and sigh
										errorMessage = "P.R.E. RATE LIMITED";
									}
									catch (Octokit.AuthorizationException)
									{
										errorMessage = "P.R.E. BAD CREDENTIALS";
									}
									catch (Octokit.NotFoundException)
									{
										// you look at your shithub and sigh
										errorMessage = "P.R.E. NOT FOUND";
									}

									// we want to take the earliest truth possible to prevent RCEs, if this fails AddTestMerge will set it
									if (I.PullRequestRevision == null && pr != null)
										I.PullRequestRevision = pr.Head.Sha;

									var mergeResult = await repo.AddTestMerge(I, committerName, currentModel.CommitterEmail, currentModel.AccessUser, currentModel.AccessToken, NextProgressReporter(), ct).ConfigureAwait(false);

									if (!mergeResult.HasValue)
										throw new JobException(String.Format(CultureInfo.InvariantCulture, "Merge of PR #{0} at {1} conflicted!", I.Number, I.PullRequestRevision.Substring(0, 7)));

									++doneSteps;

									var revInfoUpdateTask = UpdateRevInfo();

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
						}

						var currentHead = repo.Head;
						if (startSha != currentHead || (postUpdateSha != null && postUpdateSha != currentHead))
						{
							await repo.Sychronize(currentModel.AccessUser, currentModel.AccessToken, currentModel.CommitterName, currentModel.CommitterEmail, NextProgressReporter(), false, ct).ConfigureAwait(false);
							await UpdateRevInfo().ConfigureAwait(false);
						}

						await databaseContext.Save(ct).ConfigureAwait(false);
					}
					catch
					{
						doneSteps = 0;
						numSteps = 2;

						// the stuff didn't make it into the db, forget what we've done and abort
						await repo.CheckoutObject(startReference ?? startSha, NextProgressReporter(), default).ConfigureAwait(false);
						if (startReference != null && repo.Head != startSha)
							await repo.ResetToSha(startSha, NextProgressReporter(), default).ConfigureAwait(false);
						else
							progressReporter(100);
						throw;
					}
				}
			}, cancellationToken).ConfigureAwait(false);

			api.ActiveJob = job.ToApi();
			return Accepted(api);
		}
		#pragma warning restore CA1502
		#pragma warning restore CA1505
	}
}
