using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LibGit2Sharp;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Service for performing the complex <see cref="Job"/> of updating the repository on user request.
	/// </summary>
	sealed class RepositoryUpdateService
	{
		/// <summary>
		/// The current <see cref="RepositorySettings"/> for the <see cref="RepositoryUpdateService"/>.
		/// </summary>
		readonly Models.RepositorySettings currentModel;

		/// <summary>
		/// The <see cref="User"/> that initiated the repository update.
		/// </summary>
		readonly User initiatingUser;

		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="RepositoryUpdateService"/>.
		/// </summary>
		readonly ILogger<RepositoryUpdateService> logger;

		/// <summary>
		/// The <see cref="EntityId.Id"/> of the associated <see cref="Instance"/>.
		/// </summary>
		readonly long instanceId;

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryUpdateService"/> class.
		/// </summary>
		/// <param name="currentModel">The value of <see cref="currentModel"/>.</param>
		/// <param name="initiatingUser">The value of <see cref="initiatingUser"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="instanceId">The value of <see cref="instanceId"/>.</param>
		public RepositoryUpdateService(
			Models.RepositorySettings currentModel,
			User initiatingUser,
			ILogger<RepositoryUpdateService> logger,
			long instanceId)
		{
			this.currentModel = currentModel ?? throw new ArgumentNullException(nameof(currentModel));
			this.initiatingUser = initiatingUser ?? throw new ArgumentNullException(nameof(initiatingUser));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.instanceId = instanceId;
		}

		/// <summary>
		/// Load the <see cref="RevisionInformation"/> for the current <paramref name="repository"/> state into a given <paramref name="databaseContext"/>.
		/// </summary>
		/// <param name="repository">The <see cref="IRepository"/>.</param>
		/// <param name="databaseContext">The active <see cref="IDatabaseContext"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="instance">The active <see cref="Models.Instance"/>.</param>
		/// <param name="lastOriginCommitSha">The last known origin commit SHA of the <paramref name="repository"/> if any.</param>
		/// <param name="revInfoSink">An optional <see cref="Action{T}"/> to receive the loaded <see cref="RevisionInformation"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the <paramref name="databaseContext"/> was modified in a way that requires saving, <see langword="false"/> otherwise.</returns>
		public static async ValueTask<bool> LoadRevisionInformation(
			IRepository repository,
			IDatabaseContext databaseContext,
			ILogger logger,
			Models.Instance instance,
			string? lastOriginCommitSha,
			Action<Models.RevisionInformation>? revInfoSink,
			CancellationToken cancellationToken)
		{
			var repoSha = repository.Head;

			IQueryable<Models.RevisionInformation> ApplyQuery(IQueryable<Models.RevisionInformation> query) => query
				.Where(x => x.CommitSha == repoSha && x.InstanceId == instance.Id)
				.Include(x => x.CompileJobs)
				.Include(x => x.ActiveTestMerges!)
					.ThenInclude(x => x.TestMerge)
						.ThenInclude(x => x.MergedBy);

			var revisionInfo = await ApplyQuery(databaseContext.RevisionInformations).FirstOrDefaultAsync(cancellationToken);

			// If the DB doesn't have it, check the local set
			if (revisionInfo == default)
				revisionInfo = databaseContext
					.RevisionInformations
					.Local
					.Where(x => x.CommitSha == repoSha && x.InstanceId == instance.Id)
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

			revisionInfo!.OriginCommitSha ??= lastOriginCommitSha;
			if (revisionInfo.OriginCommitSha == null)
			{
				revisionInfo.OriginCommitSha = repoSha;
				logger.LogInformation(Repository.OriginTrackingErrorTemplate, repoSha);
			}

			revInfoSink?.Invoke(revisionInfo);
			return needsDbUpdate;
		}

		/// <summary>
		/// The job entrypoint used by <see cref="Controllers.RepositoryController"/> to update the repository's current HEAD.
		/// </summary>
		/// <param name="model">The <see cref="RepositoryUpdateRequest"/>.</param>
		/// <param name="instance">The <see cref="IInstanceCore"/> the job is running on.</param>
		/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the operation.</param>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> for the job.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
#pragma warning disable CA1502, CA1506 // TODO: Decomplexify
		public async ValueTask RepositoryUpdateJob(
			RepositoryUpdateRequest model,
			IInstanceCore? instance,
			IDatabaseContextFactory databaseContextFactory,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken)
#pragma warning restore CA1502, CA1506
		{
			ArgumentNullException.ThrowIfNull(model);
			ArgumentNullException.ThrowIfNull(instance);
			ArgumentNullException.ThrowIfNull(databaseContextFactory);
			ArgumentNullException.ThrowIfNull(progressReporter);

			var repoManager = instance.RepositoryManager;
			using var repo = await repoManager.LoadRepository(cancellationToken) ?? throw new JobException(ErrorCode.RepoMissing);
			var modelHasShaOrReference = model.CheckoutSha != null || model.Reference != null;

			var startReference = repo.Reference;
			var startSha = repo.Head;
			string? postUpdateSha = null;

			var newTestMerges = model.NewTestMerges != null && model.NewTestMerges.Count > 0;

			if (newTestMerges && repo.RemoteGitProvider == RemoteGitProvider.Unknown)
				throw new JobException(ErrorCode.RepoUnsupportedTestMergeRemote);

			var committerName = (currentModel.ShowTestMergeCommitters!.Value
				? initiatingUser.Name
				: currentModel.CommitterName)!;

			var hardResettingToOriginReference = model.UpdateFromOrigin == true && model.Reference != null;

			var numSteps = (model.NewTestMerges?.Count ?? 0) + (model.UpdateFromOrigin == true ? 1 : 0) + (!modelHasShaOrReference ? 2 : (hardResettingToOriginReference ? 3 : 1));
			var progressFactor = 1.0 / numSteps;

			JobProgressReporter NextProgressReporter(string? stage) => progressReporter.CreateSection(stage, progressFactor);

			progressReporter.ReportProgress(0);

			// get a base line for where we are
			Models.RevisionInformation? lastRevisionInfo = null;

			var attachedInstance = new Models.Instance
			{
				Id = instanceId,
			};

			ValueTask CallLoadRevInfo(Models.TestMerge? testMergeToAdd = null, string? lastOriginCommitSha = null) => databaseContextFactory
				.UseContext(
					async databaseContext =>
					{
						databaseContext.Instances.Attach(attachedInstance);
						var previousRevInfo = lastRevisionInfo!;
						var needsUpdate = await LoadRevisionInformation(
							repo,
							databaseContext,
							logger,
							attachedInstance,
							lastOriginCommitSha,
							x => lastRevisionInfo = x,
							cancellationToken);

						if (testMergeToAdd != null)
						{
							// rev info may have already loaded the user
							var mergedBy = databaseContext.Users.Local.FirstOrDefault(x => x.Id == initiatingUser.Id);
							if (mergedBy == default)
							{
								mergedBy = new User
								{
									Id = initiatingUser.Id,
								};

								databaseContext.Users.Attach(mergedBy);
							}

							testMergeToAdd.MergedBy = mergedBy;
							testMergeToAdd.MergedAt = DateTimeOffset.UtcNow;

							var activeTestMerges = lastRevisionInfo!.ActiveTestMerges!;
							foreach (var activeTestMerge in previousRevInfo.ActiveTestMerges!)
								activeTestMerges.Add(activeTestMerge);

							activeTestMerges.Add(new RevInfoTestMerge(testMergeToAdd, lastRevisionInfo));
							lastRevisionInfo.PrimaryTestMerge = testMergeToAdd;

							needsUpdate = true;
						}

						if (needsUpdate)
							await databaseContext.Save(cancellationToken);
					});

			await CallLoadRevInfo();

			// apply new rev info, tracking applied test merges
			ValueTask UpdateRevInfo(Models.TestMerge? testMergeToAdd = null) => CallLoadRevInfo(testMergeToAdd, lastRevisionInfo!.OriginCommitSha);

			try
			{
				// fetch/pull
				if (model.UpdateFromOrigin == true)
				{
					if (!repo.Tracking)
						throw new JobException(ErrorCode.RepoReferenceRequired);
					using (var fetchReporter = NextProgressReporter("Fetch Origin"))
						await repo.FetchOrigin(
							fetchReporter,
							currentModel.AccessUser,
							currentModel.AccessToken,
							false,
							cancellationToken);

					if (!modelHasShaOrReference)
					{
						bool? fastForward;
						using (var mergeReporter = NextProgressReporter("Merge Origin"))
							fastForward = await repo.MergeOrigin(
								mergeReporter,
								committerName,
								currentModel.CommitterEmail!,
								false,
								cancellationToken);

						if (!fastForward.HasValue)
							throw new JobException(ErrorCode.RepoMergeConflict);

						lastRevisionInfo!.OriginCommitSha = await repo.GetOriginSha(cancellationToken);
						await UpdateRevInfo();
						if (fastForward.Value)
						{
							using var syncReporter = NextProgressReporter("Sychronize");
							await repo.Synchronize(
								syncReporter,
								currentModel.AccessUser,
								currentModel.AccessToken,
								currentModel.CommitterName!,
								currentModel.CommitterEmail!,
								true,
								false,
								cancellationToken);
							postUpdateSha = repo.Head;
						}
						else
							NextProgressReporter(null).Dispose();
					}
				}

				var updateSubmodules = currentModel.UpdateSubmodules!.Value;

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
						var committish = model.CheckoutSha ?? model.Reference!;
						var isSha = await repo.IsSha(committish, cancellationToken);

						if ((isSha && model.Reference != null) || (!isSha && model.CheckoutSha != null))
							throw new JobException(ErrorCode.RepoSwappedShaOrReference);

						using (var checkoutReporter = NextProgressReporter("Checkout"))
							await repo.CheckoutObject(
								committish,
								currentModel.AccessUser,
								currentModel.AccessToken,
								updateSubmodules,
								false,
								checkoutReporter,
								cancellationToken);
						await CallLoadRevInfo(); // we've either seen origin before or what we're checking out is on origin
					}
					else
						NextProgressReporter(null).Dispose();

					if (hardResettingToOriginReference)
					{
						if (!repo.Tracking)
							throw new JobException(ErrorCode.RepoReferenceNotTracking);
						using (var resetReporter = NextProgressReporter("Reset to Origin"))
							await repo.ResetToOrigin(
								resetReporter,
								currentModel.AccessUser,
								currentModel.AccessToken,
								updateSubmodules,
								false,
								cancellationToken);

						using (var syncReporter = NextProgressReporter("Synchronize"))
							await repo.Synchronize(
								syncReporter,
								currentModel.AccessUser,
								currentModel.AccessToken,
								currentModel.CommitterName!,
								currentModel.CommitterEmail!,
								true,
								false,
								cancellationToken);

						await CallLoadRevInfo();

						// repo head is on origin so force this
						// will update the db if necessary
						lastRevisionInfo!.OriginCommitSha = repo.Head;
					}
				}

				// test merging
				if (newTestMerges)
				{
					if (repo.RemoteGitProvider == RemoteGitProvider.Unknown)
						throw new JobException(ErrorCode.RepoTestMergeInvalidRemote);

					// bit of sanitization
					var newTestMergeModels = model.NewTestMerges!;
					foreach (var newTestMergeWithoutTargetCommitSha in newTestMergeModels.Where(x => String.IsNullOrWhiteSpace(x.TargetCommitSha)))
						newTestMergeWithoutTargetCommitSha.TargetCommitSha = null;

					var repoOwner = repo.RemoteRepositoryOwner;
					var repoName = repo.RemoteRepositoryName;

					// optimization: if we've already merged these exact same commits in this fashion before, just find the rev info for it and check it out
					Models.RevisionInformation? revInfoWereLookingFor = null;
					bool needToApplyRemainingPrs = true;
					if (lastRevisionInfo!.OriginCommitSha == lastRevisionInfo.CommitSha)
					{
						bool cantSearch = false;
						foreach (var newTestMerge in newTestMergeModels)
						{
							if (newTestMerge.TargetCommitSha != null)
#pragma warning disable CA1308 // Normalize strings to uppercase
								newTestMerge.TargetCommitSha = newTestMerge.TargetCommitSha?.ToLowerInvariant(); // ala libgit2
#pragma warning restore CA1308 // Normalize strings to uppercase
							else
								try
								{
									// retrieve the latest sha
									var pr = await repo.GetTestMerge(newTestMerge, currentModel, cancellationToken);

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
							List<Models.RevisionInformation>? dbPull = null;

							await databaseContextFactory.UseContext(
								async databaseContext =>
									dbPull = await databaseContext.RevisionInformations
										.AsQueryable()
										.Where(x => x.InstanceId == instanceId
											&& x.OriginCommitSha == lastRevisionInfo.OriginCommitSha
											&& x.ActiveTestMerges!.Count <= newTestMergeModels.Count
											&& x.ActiveTestMerges!.Count > 0)
										.Include(x => x.ActiveTestMerges!)
											.ThenInclude(x => x.TestMerge)
										.ToListAsync(cancellationToken));

							// split here cause this bit has to be done locally
							revInfoWereLookingFor = dbPull!
								.Where(x => x.ActiveTestMerges!.Count == newTestMergeModels.Count
									&& x.ActiveTestMerges
										.Select(y => y.TestMerge)
										.All(y => newTestMergeModels
											.Any(z =>
												y.Number == z.Number
												&& y.TargetCommitSha!.StartsWith(z.TargetCommitSha!, StringComparison.Ordinal)
												&& (y.Comment?.Trim().ToUpperInvariant() == z.Comment?.Trim().ToUpperInvariant() || z.Comment == null))))
								.FirstOrDefault();

							if (revInfoWereLookingFor == default && newTestMergeModels.Count > 1)
							{
								// okay try to add at least SOME prs we've seen before
								var listedNewTestMerges = newTestMergeModels.ToList();

								var appliedTestMergeIds = new List<long>();

								Models.RevisionInformation? lastGoodRevInfo = null;
								do
								{
									foreach (var newTestMergeParameters in listedNewTestMerges)
									{
										revInfoWereLookingFor = dbPull!
											.Where(testRevInfo =>
											{
												if (testRevInfo.PrimaryTestMerge == null)
													return false;

												var testMergeMatch = newTestMergeModels.Any(testTestMerge =>
												{
													var numberMatch = testRevInfo.PrimaryTestMerge.Number == testTestMerge.Number;
													if (!numberMatch)
														return false;

													var shaMatch = testRevInfo.PrimaryTestMerge.TargetCommitSha!.StartsWith(
														testTestMerge.TargetCommitSha!,
														StringComparison.Ordinal);
													if (!shaMatch)
														return false;

													var commentMatch = testRevInfo.PrimaryTestMerge.Comment == testTestMerge.Comment;
													return commentMatch;
												});

												if (!testMergeMatch)
													return false;

												var previousTestMergesMatch = testRevInfo
													.ActiveTestMerges!
													.Select(previousRevInfoTestMerge => previousRevInfoTestMerge.TestMerge)
													.All(previousTestMerge => appliedTestMergeIds.Contains(previousTestMerge.Id));

												return previousTestMergesMatch;
											})
											.FirstOrDefault();

										if (revInfoWereLookingFor != null)
										{
											lastGoodRevInfo = revInfoWereLookingFor;
											appliedTestMergeIds.Add(revInfoWereLookingFor.PrimaryTestMerge!.Id);
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
						var commitSha = revInfoWereLookingFor.CommitSha!;
						logger.LogDebug("Reusing existing SHA {sha}...", commitSha);
						using var resetReporter = NextProgressReporter($"Reset to {commitSha[..7]}");
						await repo.ResetToSha(commitSha, resetReporter, cancellationToken);
						lastRevisionInfo = revInfoWereLookingFor;
					}

					if (needToApplyRemainingPrs)
					{
						foreach (var newTestMerge in newTestMergeModels)
						{
							if (lastRevisionInfo.ActiveTestMerges!.Any(x => x.TestMerge.Number == newTestMerge.Number))
								throw new JobException(ErrorCode.RepoDuplicateTestMerge);

							var fullTestMergeTask = repo.GetTestMerge(newTestMerge, currentModel, cancellationToken);

							TestMergeResult mergeResult;
							using (var testMergeReporter = NextProgressReporter($"Test merge #{newTestMerge.Number}"))
								mergeResult = await repo.AddTestMerge(
									newTestMerge,
									committerName,
									currentModel.CommitterEmail!,
									currentModel.AccessUser,
									currentModel.AccessToken,
									updateSubmodules,
									testMergeReporter,
									cancellationToken);

							if (mergeResult.Status == MergeStatus.Conflicts)
								throw new JobException(
									ErrorCode.RepoTestMergeConflict,
									new JobException(
										$"Test Merge #{newTestMerge.Number} at {newTestMerge.TargetCommitSha![..7]} conflicted! Conflicting files:{Environment.NewLine}{String.Join(Environment.NewLine, mergeResult.ConflictingFiles!.Select(file => $"\t- /{file}"))}"));

							Models.TestMerge fullTestMerge;
							try
							{
								fullTestMerge = await fullTestMergeTask;
							}
							catch (Exception ex)
							{
								logger.LogWarning("Error retrieving metadata for test merge #{testMergeNumber}!", newTestMerge.Number);

								fullTestMerge = new Models.TestMerge
								{
									Author = ex.Message,
									BodyAtMerge = ex.Message,
									TitleAtMerge = ex.Message,
									Comment = newTestMerge.Comment,
									Number = newTestMerge.Number,
									Url = ex.Message,
								};
							}

							// Ensure we're getting the full sha from git itself
							fullTestMerge.TargetCommitSha = newTestMerge.TargetCommitSha;

							await UpdateRevInfo(fullTestMerge);
						}
					}
				}

				var currentHead = repo.Head;
				if (currentModel.PushTestMergeCommits!.Value && (startSha != currentHead || (postUpdateSha != null && postUpdateSha != currentHead)))
				{
					using (var syncReporter = NextProgressReporter("Synchronize"))
						await repo.Synchronize(
							syncReporter,
							currentModel.AccessUser,
							currentModel.AccessToken,
							currentModel.CommitterName!,
							currentModel.CommitterEmail!,
							false,
							false,
							cancellationToken);

					await UpdateRevInfo();
				}
			}
			catch
			{
				numSteps = 2;

				// Forget what we've done and abort
				progressReporter.ReportProgress(0.0);

				var secondStep = startReference != null && repo.Head != startSha;

				// DCTx2: Cancellation token is for job, operations should always run
				using (var checkoutReporter = progressReporter.CreateSection($"Checkout {startReference ?? startSha[..7]}", secondStep ? 0.5 : 1.0))
					await repo.CheckoutObject(
						startReference ?? startSha,
						currentModel.AccessUser,
						currentModel.AccessToken,
						true,
						false,
						checkoutReporter,
						default);

				if (secondStep)
					using (var resetReporter = progressReporter.CreateSection($"Hard reset to SHA {startSha[..7]}", 0.5))
						await repo.ResetToSha(startSha, resetReporter, default);

				throw;
			}
		}

		/// <summary>
		/// The job entrypoint used by <see cref="Controllers.RepositoryController"/> to reclone a repository.
		/// </summary>
		/// <param name="instance">The <see cref="IInstanceCore"/> the job is running on.</param>
		/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the operation.</param>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> for the job.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		public async ValueTask RepositoryRecloneJob(
			IInstanceCore? instance,
			IDatabaseContextFactory databaseContextFactory,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(instance);
			ArgumentNullException.ThrowIfNull(databaseContextFactory);
			ArgumentNullException.ThrowIfNull(progressReporter);

			progressReporter.StageName = "Loading Old Repository";

			Uri origin;
			string? oldReference;
			string oldSha;
			ValueTask deleteTask;
			using (var deleteReporter = progressReporter.CreateSection("Deleting Old Repository", 0.1))
			{
				using (var oldRepo = await instance.RepositoryManager.LoadRepository(cancellationToken))
				{
					if (oldRepo == null)
						throw new JobException(ErrorCode.RepoMissing);

					origin = oldRepo.Origin;
					oldSha = oldRepo.Head;
					oldReference = oldRepo.Reference;
					if (oldReference == Repository.NoReference)
						oldReference = null;

					deleteTask = instance.RepositoryManager.DeleteRepository(cancellationToken);
				}

				await deleteTask;
			}

			IRepository newRepo;
			try
			{
				using var cloneReporter = progressReporter.CreateSection("Cloning New Repository", 0.8);
				newRepo = await instance.RepositoryManager.CloneRepository(
					origin,
					oldReference,
					currentModel.AccessUser,
					currentModel.AccessToken,
					cloneReporter,
					true, // TODO: Make configurable maybe...
					cancellationToken)
					?? throw new JobException("A race condition occurred while recloning the repository. Somehow, it was fully cloned instantly after being deleted!"); // I'll take lines of code that should never be hit for $10k
			}
			catch (Exception ex) when (ex is not JobException)
			{
				logger.LogWarning("Reclone failed, clearing credentials!");

				// need to clear credentials here
				await databaseContextFactory.UseContextTaskReturn(context =>
				{
					context.RepositorySettings.Attach(currentModel);
					currentModel.AccessUser = null;
					currentModel.AccessToken = null;
					return context.Save(CancellationToken.None); // DCT: Must always run
				});

				throw;
			}

			using (newRepo)
			using (var checkoutReporter = progressReporter.CreateSection("Checking out previous Detached Commit", 0.1))
			{
				await newRepo.CheckoutObject(
					oldSha,
					currentModel.AccessUser,
					currentModel.AccessToken,
					false,
					oldReference != null,
					checkoutReporter,
					cancellationToken);
			}
		}
	}
}
