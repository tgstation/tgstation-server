using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing the git repository.
	/// </summary>
	[Route(Routes.Repository)]
#pragma warning disable CA1506 // TODO: Decomplexify
	public sealed class RepositoryController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="RepositoryController"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="RepositoryController"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		public RepositoryController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IInstanceManager instanceManager,
			IGitHubClientFactory gitHubClientFactory,
			IJobManager jobManager,
			ILogger<RepositoryController> logger)
			: base(
				  instanceManager,
				  databaseContext,
				  authenticationContextFactory,
				  logger)
		{
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
		}

		/// <summary>
		/// Begin cloning the repository if it doesn't exist.
		/// </summary>
		/// <param name="model">The <see cref="RepositoryCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="201">The repository was created successfully and the <see cref="JobResponse"/> to clone it has begun.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpPut]
		[TgsAuthorize(RepositoryRights.SetOrigin)]
		[ProducesResponseType(typeof(RepositoryResponse), 201)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async Task<IActionResult> Create([FromBody] RepositoryCreateRequest model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.Origin == null)
				return BadRequest(ErrorCode.ModelValidationFailure);

			if (model.AccessUser == null ^ model.AccessToken == null)
				return BadRequest(ErrorCode.RepoMismatchUserAndAccessToken);

			var userRights = (RepositoryRights)AuthenticationContext.GetRight(RightsType.Repository);
			if (((model.AccessUser ?? model.AccessToken) != null && !userRights.HasFlag(RepositoryRights.ChangeCredentials))
				|| ((model.CommitterEmail ?? model.CommitterName) != null && !userRights.HasFlag(RepositoryRights.ChangeCommitter)))
				return Forbid();

			#pragma warning disable CS0618 // Support for obsolete API field
			model.UpdateSubmodules ??= model.RecurseSubmodules;
			#pragma warning restore CS0618

			var currentModel = await DatabaseContext
				.RepositorySettings
				.AsQueryable()
				.Where(x => x.InstanceId == Instance.Id)
				.FirstOrDefaultAsync(cancellationToken)
				;

			if (currentModel == default)
				return Gone();

			currentModel.UpdateSubmodules = model.UpdateSubmodules ?? true;
			currentModel.AccessToken = model.AccessToken;
			currentModel.AccessUser = model.AccessUser;

			currentModel.CommitterEmail = model.CommitterEmail ?? currentModel.CommitterEmail;
			currentModel.CommitterName = model.CommitterName ?? currentModel.CommitterName;

			var cloneBranch = model.Reference;
			var origin = model.Origin;

			return await WithComponentInstance(
				async instance =>
				{
					var repoManager = instance.RepositoryManager;

					if (repoManager.CloneInProgress)
						return Conflict(new ErrorMessageResponse(ErrorCode.RepoCloning));

					if (repoManager.InUse)
						return Conflict(new ErrorMessageResponse(ErrorCode.RepoBusy));

					using var repo = await repoManager.LoadRepository(cancellationToken);

					// clone conflict
					if (repo != null)
						return Conflict(new ErrorMessageResponse(ErrorCode.RepoExists));

					var job = new Job
					{
						Description = String.Format(
							CultureInfo.InvariantCulture,
							"Clone{1} repository {0}",
							origin,
							cloneBranch != null
								? $"\"{cloneBranch}\" branch of"
								: String.Empty),
						StartedBy = AuthenticationContext.User,
						CancelRightsType = RightsType.Repository,
						CancelRight = (ulong)RepositoryRights.CancelClone,
						Instance = Instance,
					};
					var api = currentModel.ToApi();

					await DatabaseContext.Save(cancellationToken);
					await jobManager.RegisterOperation(
						job,
						async (core, databaseContextFactory, paramJob, progressReporter, ct) =>
						{
							var repoManager = core.RepositoryManager;
							using var repos = await repoManager.CloneRepository(
								origin,
								cloneBranch,
								currentModel.AccessUser,
								currentModel.AccessToken,
								progressReporter,
								currentModel.UpdateSubmodules.Value,
								ct)
								;
							if (repos == null)
								throw new JobException(ErrorCode.RepoExists);
							var instance = new Models.Instance
							{
								Id = Instance.Id,
							};
							await databaseContextFactory.UseContext(
								async databaseContext =>
								{
									databaseContext.Instances.Attach(instance);
									if (await PopulateApi(api, repos, databaseContext, instance, ct))
										await databaseContext.Save(ct);
								})
								;
						},
						cancellationToken)
						;

					api.Origin = model.Origin;
					api.Reference = model.Reference;
					api.ActiveJob = job.ToApi();

					return Created(api);
				})
				;
		}

		/// <summary>
		/// Delete the repository.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="202">Job to delete the repository created successfully.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpDelete]
		[TgsAuthorize(RepositoryRights.Delete)]
		[ProducesResponseType(typeof(RepositoryResponse), 202)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async Task<IActionResult> Delete(CancellationToken cancellationToken)
		{
			var currentModel = await DatabaseContext
				.RepositorySettings
				.AsQueryable()
				.Where(x => x.InstanceId == Instance.Id)
				.FirstOrDefaultAsync(cancellationToken)
				;

			if (currentModel == default)
				return Gone();

			currentModel.AccessToken = null;
			currentModel.AccessUser = null;

			await DatabaseContext.Save(cancellationToken);

			Logger.LogInformation("Instance {0} repository delete initiated by user {1}", Instance.Id, AuthenticationContext.User.Id.Value);

			var job = new Job
			{
				Description = "Delete repository",
				StartedBy = AuthenticationContext.User,
				Instance = Instance,
			};
			var api = currentModel.ToApi();
			await jobManager.RegisterOperation(
				job,
				(core, databaseContextFactory, paramJob, progressReporter, ct) => core.RepositoryManager.DeleteRepository(ct),
				cancellationToken)
			;
			api.ActiveJob = job.ToApi();
			return Accepted(api);
		}

		/// <summary>
		/// Get the repository's status.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Retrieved the repository settings successfully.</response>
		/// <response code="201">Retrieved the repository settings successfully, though they did not previously exist.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpGet]
		[TgsAuthorize(RepositoryRights.Read)]
		[ProducesResponseType(typeof(RepositoryResponse), 200)]
		[ProducesResponseType(typeof(RepositoryResponse), 201)]
		[ProducesResponseType(typeof(RepositoryResponse), 410)]
		public async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var currentModel = await DatabaseContext
				.RepositorySettings
				.AsQueryable()
				.Where(x => x.InstanceId == Instance.Id)
				.FirstOrDefaultAsync(cancellationToken)
				;

			if (currentModel == default)
				return Gone();

			var api = currentModel.ToApi();

			return await WithComponentInstance(
				async instance =>
				{
					var repoManager = instance.RepositoryManager;

					if (repoManager.CloneInProgress)
						return Conflict(new ErrorMessageResponse(ErrorCode.RepoCloning));

					if (repoManager.InUse)
						return Conflict(new ErrorMessageResponse(ErrorCode.RepoBusy));

					using var repo = await repoManager.LoadRepository(cancellationToken);
					if (repo != null && await PopulateApi(api, repo, DatabaseContext, Instance, cancellationToken))
					{
						// user may have fucked with the repo manually, do what we can
						await DatabaseContext.Save(cancellationToken);
						return Created(api);
					}

					return Json(api);
				})
				;
		}

		/// <summary>
		/// Perform updates to the repository.
		/// </summary>
		/// <param name="model">The <see cref="RepositoryUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Updated the repository settings successfully.</response>
		/// <response code="202">Updated the repository settings successfully and a <see cref="JobResponse"/> was created to make the requested git changes.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpPost]
		[TgsAuthorize(
			RepositoryRights.ChangeAutoUpdateSettings
			| RepositoryRights.ChangeCommitter
			| RepositoryRights.ChangeCredentials
			| RepositoryRights.ChangeTestMergeCommits
			| RepositoryRights.MergePullRequest
			| RepositoryRights.SetReference
			| RepositoryRights.SetSha
			| RepositoryRights.UpdateBranch
			| RepositoryRights.ChangeSubmoduleUpdate)]
		[ProducesResponseType(typeof(RepositoryResponse), 200)]
		[ProducesResponseType(typeof(RepositoryResponse), 202)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
#pragma warning disable CA1502, CA1505 // TODO: Decomplexify
		public async Task<IActionResult> Update([FromBody] RepositoryUpdateRequest model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.AccessUser == null ^ model.AccessToken == null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.RepoMismatchUserAndAccessToken));

			if (model.CheckoutSha != null && model.Reference != null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.RepoMismatchShaAndReference));

			if (model.CheckoutSha != null && model.UpdateFromOrigin == true)
				return BadRequest(new ErrorMessageResponse(ErrorCode.RepoMismatchShaAndUpdate));

			if (model.NewTestMerges?.Any(x => model.NewTestMerges.Any(y => x != y && x.Number == y.Number)) == true)
				return BadRequest(new ErrorMessageResponse(ErrorCode.RepoDuplicateTestMerge));

			if (model.CommitterName?.Length == 0)
				return BadRequest(new ErrorMessageResponse(ErrorCode.RepoWhitespaceCommitterName));

			if (model.CommitterEmail?.Length == 0)
				return BadRequest(new ErrorMessageResponse(ErrorCode.RepoWhitespaceCommitterEmail));

			var newTestMerges = model.NewTestMerges != null && model.NewTestMerges.Count > 0;
			var userRights = (RepositoryRights)AuthenticationContext.GetRight(RightsType.Repository);
			if (newTestMerges && !userRights.HasFlag(RepositoryRights.MergePullRequest))
				return Forbid();

			var currentModel = await DatabaseContext
				.RepositorySettings
				.AsQueryable()
				.Where(x => x.InstanceId == Instance.Id)
				.FirstOrDefaultAsync(cancellationToken)
				;

			if (currentModel == default)
				return Gone();

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
				|| CheckModified(x => x.CreateGitHubDeployments, RepositoryRights.ChangeTestMergeCommits)
				|| CheckModified(x => x.ShowTestMergeCommitters, RepositoryRights.ChangeTestMergeCommits)
				|| CheckModified(x => x.PostTestMergeComment, RepositoryRights.ChangeTestMergeCommits)
				|| CheckModified(x => x.UpdateSubmodules, RepositoryRights.ChangeSubmoduleUpdate)
				|| (model.UpdateFromOrigin == true && !userRights.HasFlag(RepositoryRights.UpdateBranch))
				|| (model.CheckoutSha != null && !userRights.HasFlag(RepositoryRights.SetSha))
				|| (model.Reference != null && model.UpdateFromOrigin != true && !userRights.HasFlag(RepositoryRights.SetReference))) // don't care if it's the same reference, we want to forbid them before starting the job
				return Forbid();

			if (model.AccessToken?.Length == 0 && model.AccessUser?.Length == 0)
			{
				// setting an empty string clears everything
				currentModel.AccessUser = null;
				currentModel.AccessToken = null;
			}

			var canRead = userRights.HasFlag(RepositoryRights.Read);

			var api = canRead ? currentModel.ToApi() : new RepositoryResponse();
			if (canRead)
			{
				var earlyOut = await WithComponentInstance(
				async instance =>
				{
					var repoManager = instance.RepositoryManager;
					if (repoManager.CloneInProgress)
						return Conflict(new ErrorMessageResponse(ErrorCode.RepoCloning));

					if (repoManager.InUse)
						return Conflict(new ErrorMessageResponse(ErrorCode.RepoBusy));

					using var repo = await repoManager.LoadRepository(cancellationToken);
					if (repo == null)
						return Conflict(new ErrorMessageResponse(ErrorCode.RepoMissing));
					await PopulateApi(api, repo, DatabaseContext, Instance, cancellationToken);

					return null;
				})
				;

				if (earlyOut != null)
					return earlyOut;
			}

			// this is just db stuf so stow it away
			await DatabaseContext.Save(cancellationToken);

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
				description = String.Format(
					CultureInfo.InvariantCulture,
					"{0}est merge(s) {1}{2}",
					description != null
						? String.Format(CultureInfo.InvariantCulture, "{0} and t", description)
						: "T",
					String.Join(
						", ",
						model.NewTestMerges.Select(
							x => String.Format(
								CultureInfo.InvariantCulture,
								"#{0}{1}",
								x.Number,
								x.TargetCommitSha != null
									? String.Format(
										CultureInfo.InvariantCulture,
										" at {0}",
										x.TargetCommitSha.Substring(0, 7))
									: String.Empty))),
					description != null
						? String.Empty
						: " in repository");

			if (description == null)
				return Json(api); // no git changes

			async Task<IActionResult> RepositoryUpdateJobOhGodPleaseSomeoneRefactorThisItsTooFuckingBig(
				IInstanceCore instance,
				IDatabaseContextFactory databaseContextFactory,
				JobProgressReporter progressReporter,
				CancellationToken ct)
			{
				var repoManager = instance.RepositoryManager;
				using var repo = await repoManager.LoadRepository(ct);
				if (repo == null)
					throw new JobException(ErrorCode.RepoMissing);

				var modelHasShaOrReference = model.CheckoutSha != null || model.Reference != null;

				var startReference = repo.Reference;
				var startSha = repo.Head;
				string postUpdateSha = null;

				if (newTestMerges && repo.RemoteGitProvider == RemoteGitProvider.Unknown)
					throw new JobException(ErrorCode.RepoUnsupportedTestMergeRemote);

				var committerName = currentModel.ShowTestMergeCommitters.Value
					? AuthenticationContext.User.Name
					: currentModel.CommitterName;

				var hardResettingToOriginReference = model.UpdateFromOrigin == true && model.Reference != null;

				var numSteps = (model.NewTestMerges?.Count ?? 0) + (model.UpdateFromOrigin == true ? 1 : 0) + (!modelHasShaOrReference ? 2 : (hardResettingToOriginReference ? 3 : 1));
				var doneSteps = 0;

				JobProgressReporter NextProgressReporter()
				{
					var tmpDoneSteps = doneSteps;
					++doneSteps;
					return (status, progress) => progressReporter(status, (progress + (100 * tmpDoneSteps)) / numSteps);
				}

				progressReporter(null, 0);

				// get a base line for where we are
				Models.RevisionInformation lastRevisionInfo = null;

				var attachedInstance = new Models.Instance
				{
					Id = Instance.Id,
				};

				Task CallLoadRevInfo(Models.TestMerge testMergeToAdd = null, string lastOriginCommitSha = null) => databaseContextFactory
					.UseContext(
						async databaseContext =>
						{
							databaseContext.Instances.Attach(attachedInstance);
							var previousRevInfo = lastRevisionInfo;
							var needsUpdate = await LoadRevisionInformation(
								repo,
								databaseContext,
								attachedInstance,
								lastOriginCommitSha,
								x => lastRevisionInfo = x,
								ct)
								;

							if (testMergeToAdd != null)
							{
								// rev info may have already loaded the user
								var mergedBy = databaseContext.Users.Local.FirstOrDefault(x => x.Id == AuthenticationContext.User.Id);
								if (mergedBy == default)
								{
									mergedBy = new User
									{
										Id = AuthenticationContext.User.Id,
									};

									databaseContext.Users.Attach(mergedBy);
								}

								testMergeToAdd.MergedBy = mergedBy;

								foreach (var activeTestMerge in previousRevInfo.ActiveTestMerges)
									lastRevisionInfo.ActiveTestMerges.Add(activeTestMerge);

								lastRevisionInfo.ActiveTestMerges.Add(new RevInfoTestMerge
								{
									TestMerge = testMergeToAdd,
								});
								lastRevisionInfo.PrimaryTestMerge = testMergeToAdd;

								needsUpdate = true;
							}

							if (needsUpdate)
								await databaseContext.Save(cancellationToken);
						});

				await CallLoadRevInfo();

				// apply new rev info, tracking applied test merges
				Task UpdateRevInfo(Models.TestMerge testMergeToAdd = null) => CallLoadRevInfo(testMergeToAdd, lastRevisionInfo.OriginCommitSha);

				try
				{
					// fetch/pull
					if (model.UpdateFromOrigin == true)
					{
						if (!repo.Tracking)
							throw new JobException(ErrorCode.RepoReferenceRequired);
						await repo.FetchOrigin(currentModel.AccessUser, currentModel.AccessToken, NextProgressReporter(), ct);
						doneSteps = 1;
						if (!modelHasShaOrReference)
						{
							var fastForward = await repo.MergeOrigin(committerName, currentModel.CommitterEmail, NextProgressReporter(), ct);
							if (!fastForward.HasValue)
								throw new JobException(ErrorCode.RepoMergeConflict);
							lastRevisionInfo.OriginCommitSha = await repo.GetOriginSha(cancellationToken);
							await UpdateRevInfo();
							if (fastForward.Value)
							{
								await repo.Sychronize(
									currentModel.AccessUser,
									currentModel.AccessToken,
									currentModel.CommitterName,
									currentModel.CommitterEmail,
									NextProgressReporter(),
									true,
									ct)
									;
								postUpdateSha = repo.Head;
							}
							else
								NextProgressReporter()(null, 100);
						}
					}

					var updateSubmodules = currentModel.UpdateSubmodules.Value;

					// checkout/hard reset
					if (modelHasShaOrReference)
					{
						var validCheckoutSha =
							model.CheckoutSha != null
							&& !repo.Head.StartsWith(model.CheckoutSha, StringComparison.OrdinalIgnoreCase);
						var validCheckoutReference =
							model.Reference != null
							&& !repo.Reference.Equals(model.Reference, StringComparison.OrdinalIgnoreCase);

						if (validCheckoutSha || validCheckoutReference)
						{
							var committish = model.CheckoutSha ?? model.Reference;
							var isSha = await repo.IsSha(committish, cancellationToken);

							if ((isSha && model.Reference != null) || (!isSha && model.CheckoutSha != null))
								throw new JobException(ErrorCode.RepoSwappedShaOrReference);

							await repo.CheckoutObject(
								committish,
								currentModel.AccessUser,
								currentModel.AccessToken,
								updateSubmodules,
								NextProgressReporter(),
								ct)
								;
							await CallLoadRevInfo(); // we've either seen origin before or what we're checking out is on origin
						}
						else
							NextProgressReporter()(null, 100);

						if (hardResettingToOriginReference)
						{
							if (!repo.Tracking)
								throw new JobException(ErrorCode.RepoReferenceNotTracking);
							await repo.ResetToOrigin(
								currentModel.AccessUser,
								currentModel.AccessToken,
								updateSubmodules,
								NextProgressReporter(),
								ct)
								;
							await repo.Sychronize(
								currentModel.AccessUser,
								currentModel.AccessToken,
								currentModel.CommitterName,
								currentModel.CommitterEmail,
								NextProgressReporter(),
								true,
								ct)
								;
							await CallLoadRevInfo();

							// repo head is on origin so force this
							// will update the db if necessary
							lastRevisionInfo.OriginCommitSha = repo.Head;
						}
					}

					// test merging
					if (newTestMerges)
					{
						if (repo.RemoteGitProvider == RemoteGitProvider.Unknown)
							throw new JobException(ErrorCode.RepoTestMergeInvalidRemote);

						// bit of sanitization
						foreach (var newTestMergeWithoutTargetCommitSha in model.NewTestMerges.Where(x => String.IsNullOrWhiteSpace(x.TargetCommitSha)))
							newTestMergeWithoutTargetCommitSha.TargetCommitSha = null;

						var gitHubClient = currentModel.AccessToken != null
							? gitHubClientFactory.CreateClient(currentModel.AccessToken)
							: gitHubClientFactory.CreateClient();

						var repoOwner = repo.RemoteRepositoryOwner;
						var repoName = repo.RemoteRepositoryName;

						// optimization: if we've already merged these exact same commits in this fashion before, just find the rev info for it and check it out
						Models.RevisionInformation revInfoWereLookingFor = null;
						bool needToApplyRemainingPrs = true;
						if (lastRevisionInfo.OriginCommitSha == lastRevisionInfo.CommitSha)
						{
							bool cantSearch = false;
							foreach (var newTestMerge in model.NewTestMerges)
							{
								if (newTestMerge.TargetCommitSha != null)
#pragma warning disable CA1308 // Normalize strings to uppercase
									newTestMerge.TargetCommitSha = newTestMerge.TargetCommitSha?.ToLowerInvariant(); // ala libgit2
#pragma warning restore CA1308 // Normalize strings to uppercase
								else
									try
									{
										// retrieve the latest sha
										var pr = await repo.GetTestMerge(newTestMerge, currentModel, ct);

										// we want to take the earliest truth possible to prevent RCEs, if this fails AddTestMerge will set it
										newTestMerge.TargetCommitSha = pr.TargetCommitSha;
									}
									catch
									{
										cantSearch = true;
										break;
									}
							}

							if (!cantSearch)
							{
								List<Models.RevisionInformation> dbPull = null;

								await databaseContextFactory.UseContext(
									async databaseContext =>
										dbPull = await databaseContext.RevisionInformations
											.AsQueryable()
											.Where(x => x.Instance.Id == Instance.Id
											&& x.OriginCommitSha == lastRevisionInfo.OriginCommitSha
											&& x.ActiveTestMerges.Count <= model.NewTestMerges.Count
											&& x.ActiveTestMerges.Count > 0)
											.Include(x => x.ActiveTestMerges)
											.ThenInclude(x => x.TestMerge)
											.ToListAsync(cancellationToken)
											)
									;

								// split here cause this bit has to be done locally
								revInfoWereLookingFor = dbPull
									.Where(x => x.ActiveTestMerges.Count == model.NewTestMerges.Count
									&& x.ActiveTestMerges.Select(y => y.TestMerge)
									.All(y => model.NewTestMerges.Any(z =>
									y.Number == z.Number
									&& y.TargetCommitSha.StartsWith(z.TargetCommitSha, StringComparison.Ordinal)
									&& (y.Comment?.Trim().ToUpperInvariant() == z.Comment?.Trim().ToUpperInvariant() || z.Comment == null))))
									.FirstOrDefault();

								if (revInfoWereLookingFor == default && model.NewTestMerges.Count > 1)
								{
									// okay try to add at least SOME prs we've seen before
									var listedNewTestMerges = model.NewTestMerges.ToList();

									var appliedTestMergeIds = new List<long>();

									Models.RevisionInformation lastGoodRevInfo = null;
									do
									{
										foreach (var newTestMergeParameters in listedNewTestMerges)
										{
											revInfoWereLookingFor = dbPull
												.Where(testRevInfo =>
												{
													if (testRevInfo.PrimaryTestMerge == null)
														return false;

													var testMergeMatch = model.NewTestMerges.Any(testTestMerge =>
													{
														var numberMatch = testRevInfo.PrimaryTestMerge.Number == testTestMerge.Number;
														if (!numberMatch)
															return false;

														var shaMatch = testRevInfo.PrimaryTestMerge.TargetCommitSha.StartsWith(
															testTestMerge.TargetCommitSha,
															StringComparison.Ordinal);
														if (!shaMatch)
															return false;

														var commentMatch = testRevInfo.PrimaryTestMerge.Comment == testTestMerge.Comment;
														return commentMatch;
													});

													if (!testMergeMatch)
														return false;

													var previousTestMergesMatch = testRevInfo
														.ActiveTestMerges
														.Select(previousRevInfoTestMerge => previousRevInfoTestMerge.TestMerge)
														.All(previousTestMerge => appliedTestMergeIds.Contains(previousTestMerge.Id));

													return previousTestMergesMatch;
												})
												.FirstOrDefault();

											if (revInfoWereLookingFor != null)
											{
												lastGoodRevInfo = revInfoWereLookingFor;
												appliedTestMergeIds.Add(revInfoWereLookingFor.PrimaryTestMerge.Id);
												listedNewTestMerges.Remove(newTestMergeParameters);
												break;
											}
										}
									}
									while (revInfoWereLookingFor != null && listedNewTestMerges.Count > 0);

									revInfoWereLookingFor = lastGoodRevInfo;
									needToApplyRemainingPrs = listedNewTestMerges.Count != 0;
									if (needToApplyRemainingPrs)
										model.NewTestMerges = listedNewTestMerges;
								}
								else if (revInfoWereLookingFor != null)
									needToApplyRemainingPrs = false;
							}
						}

						if (revInfoWereLookingFor != null)
						{
							// goteem
							Logger.LogDebug("Reusing existing SHA {0}...", revInfoWereLookingFor.CommitSha);
							await repo.ResetToSha(revInfoWereLookingFor.CommitSha, NextProgressReporter(), cancellationToken);
							lastRevisionInfo = revInfoWereLookingFor;
						}

						if (needToApplyRemainingPrs)
						{
							foreach (var newTestMerge in model.NewTestMerges)
							{
								if (lastRevisionInfo.ActiveTestMerges.Any(x => x.TestMerge.Number == newTestMerge.Number))
									throw new JobException(ErrorCode.RepoDuplicateTestMerge);

								var fullTestMergeTask = repo.GetTestMerge(newTestMerge, currentModel, ct);

								var mergeResult = await repo.AddTestMerge(
									newTestMerge,
									committerName,
									currentModel.CommitterEmail,
									currentModel.AccessUser,
									currentModel.AccessToken,
									updateSubmodules,
									NextProgressReporter(),
									ct);

								if (mergeResult == null)
									throw new JobException(
										ErrorCode.RepoTestMergeConflict,
										new JobException(
											$"Test Merge #{newTestMerge.Number} at {newTestMerge.TargetCommitSha.Substring(0, 7)} conflicted!"));

								Models.TestMerge fullTestMerge;
								try
								{
									fullTestMerge = await fullTestMergeTask;
								}
								catch (Exception ex)
								{
									Logger.LogWarning("Error retrieving metadata for test merge #{0}!", newTestMerge.Number);

									fullTestMerge = new Models.TestMerge
									{
										Author = ex.Message,
										BodyAtMerge = ex.Message,
										MergedAt = DateTimeOffset.UtcNow,
										TitleAtMerge = ex.Message,
										Comment = newTestMerge.Comment,
										Number = newTestMerge.Number,
										Url = ex.Message,
									};
								}

								// Ensure we're getting the full sha from git itself
								fullTestMerge.TargetCommitSha = newTestMerge.TargetCommitSha;

								// MergedBy will be set later
								++doneSteps;

								await UpdateRevInfo(fullTestMerge);
							}
						}
					}

					var currentHead = repo.Head;
					if (currentModel.PushTestMergeCommits.Value && (startSha != currentHead || (postUpdateSha != null && postUpdateSha != currentHead)))
					{
						await repo.Sychronize(
							currentModel.AccessUser,
							currentModel.AccessToken,
							currentModel.CommitterName,
							currentModel.CommitterEmail,
							NextProgressReporter(),
							false,
							ct)
							;
						await UpdateRevInfo();
					}

					return null;
				}
				catch
				{
					doneSteps = 0;
					numSteps = 2;

					// Forget what we've done and abort
					// DCTx2: Cancellation token is for job, operations should always run
					await repo.CheckoutObject(
						startReference ?? startSha,
						currentModel.AccessUser,
						currentModel.AccessToken,
						true,
						NextProgressReporter(),
						default)
						;
					if (startReference != null && repo.Head != startSha)
						await repo.ResetToSha(startSha, NextProgressReporter(), default);
					else
						progressReporter(null, 100);
					throw;
				}
			}

			var job = new Job
			{
				Description = description,
				StartedBy = AuthenticationContext.User,
				Instance = Instance,
				CancelRightsType = RightsType.Repository,
				CancelRight = (ulong)RepositoryRights.CancelPendingChanges,
			};

			// Time to access git, do it in a job
			await jobManager.RegisterOperation(
				job,
				(core, databaseContextFactory, paramJob, progressReporter, ct) =>
					RepositoryUpdateJobOhGodPleaseSomeoneRefactorThisItsTooFuckingBig(
						core,
						databaseContextFactory,
						progressReporter,
						ct),
				cancellationToken)
				;

			api.ActiveJob = job.ToApi();
			return Accepted(api);
		}
#pragma warning restore CA1502, CA1505

		/// <summary>
		/// Load the <see cref="RevisionInformation"/> for the current <paramref name="repository"/> state into a given <paramref name="databaseContext"/>.
		/// </summary>
		/// <param name="repository">The <see cref="Components.Repository.IRepository"/>.</param>
		/// <param name="databaseContext">The active <see cref="IDatabaseContext"/>.</param>
		/// <param name="instance">The active <see cref="Models.Instance"/>.</param>
		/// <param name="lastOriginCommitSha">The last known origin commit SHA of the <paramref name="repository"/> if any.</param>
		/// <param name="revInfoSink">An optional <see cref="Action{T}"/> to receive the loaded <see cref="RevisionInformation"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the <paramref name="databaseContext"/> was modified in a way that requires saving, <see langword="false"/> otherwise.</returns>
		async Task<bool> LoadRevisionInformation(
			Components.Repository.IRepository repository,
			IDatabaseContext databaseContext,
			Models.Instance instance,
			string lastOriginCommitSha,
			Action<Models.RevisionInformation> revInfoSink,
			CancellationToken cancellationToken)
		{
			var repoSha = repository.Head;

			IQueryable<Models.RevisionInformation> ApplyQuery(IQueryable<Models.RevisionInformation> query) => query
				.Where(x => x.CommitSha == repoSha && x.Instance.Id == instance.Id)
				.Include(x => x.CompileJobs)
				.Include(x => x.ActiveTestMerges).ThenInclude(x => x.TestMerge).ThenInclude(x => x.MergedBy);

			var revisionInfo = await ApplyQuery(databaseContext.RevisionInformations).FirstOrDefaultAsync(cancellationToken);

			// If the DB doesn't have it, check the local set
			if (revisionInfo == default)
				revisionInfo = databaseContext
					.RevisionInformations
					.Local
					.Where(x => x.CommitSha == repoSha && x.Instance.Id == instance.Id)
					.FirstOrDefault();

			var needsDbUpdate = revisionInfo == default;
			if (needsDbUpdate)
			{
				// needs insertion
				revisionInfo = new Models.RevisionInformation
				{
					Instance = instance,
					CommitSha = repoSha,
					Timestamp = await repository.TimestampCommit(repoSha, cancellationToken),
					CompileJobs = new List<CompileJob>(),
					ActiveTestMerges = new List<RevInfoTestMerge>(), // non null vals for api returns
				};

				lock (databaseContext) // cleaner this way
					databaseContext.RevisionInformations.Add(revisionInfo);
			}

			revisionInfo.OriginCommitSha ??= lastOriginCommitSha;
			if (revisionInfo.OriginCommitSha == null)
			{
				revisionInfo.OriginCommitSha = repoSha;
				Logger.LogInformation(Components.Repository.Repository.OriginTrackingErrorTemplate, repoSha);
			}

			revInfoSink?.Invoke(revisionInfo);
			return needsDbUpdate;
		}

		/// <summary>
		/// Populate a given <paramref name="apiResponse"/> with the current state of a given <paramref name="repository"/>.
		/// </summary>
		/// <param name="apiResponse">The <see cref="RepositoryResponse"/> to populate.</param>
		/// <param name="repository">The <see cref="Components.Repository.IRepository"/>.</param>
		/// <param name="databaseContext">The active <see cref="IDatabaseContext"/>.</param>
		/// <param name="instance">The active <see cref="Models.Instance"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the <paramref name="databaseContext"/> was modified in a way that requires saving, <see langword="false"/> otherwise.</returns>
		async Task<bool> PopulateApi(
			RepositoryResponse apiResponse,
			Components.Repository.IRepository repository,
			IDatabaseContext databaseContext,
			Models.Instance instance,
			CancellationToken cancellationToken)
		{
			apiResponse.RemoteGitProvider = repository.RemoteGitProvider;
			apiResponse.RemoteRepositoryOwner = repository.RemoteRepositoryOwner;
			apiResponse.RemoteRepositoryName = repository.RemoteRepositoryName;

			apiResponse.Origin = repository.Origin;
			apiResponse.Reference = repository.Reference;

			// rev info stuff
			Models.RevisionInformation revisionInfo = null;
			var needsDbUpdate = await LoadRevisionInformation(repository, databaseContext, instance, null, x => revisionInfo = x, cancellationToken);
			apiResponse.RevisionInformation = revisionInfo.ToApi();
			return needsDbUpdate;
		}
	}
}
