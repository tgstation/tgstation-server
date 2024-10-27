using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class RepositoryTest : JobsRequiredTest
	{
		readonly IRepositoryClient repositoryClient;

		public RepositoryTest(IRepositoryClient repositoryClient, IJobsClient jobsClient)
			: base(jobsClient)
		{
			this.repositoryClient = repositoryClient ?? throw new ArgumentNullException(nameof(repositoryClient));
		}

		public async Task<Task<JobResponse>> RunLongClone(CancellationToken cancellationToken)
		{
			var workingBranch = "master";

			const string Origin = "https://github.com/tgstation/tgstation";
			var cloneRequest = new RepositoryCreateRequest
			{
				Origin = new Uri(Origin),
				Reference = workingBranch,
			};

			var clone = await repositoryClient.Clone(cloneRequest, cancellationToken);

			return Rest();

			async Task<JobResponse> Rest()
			{
				await Task.Yield();
				await ApiAssert.ThrowsException<ConflictException, RepositoryResponse>(() => repositoryClient.Read(cancellationToken), ErrorCode.RepoCloning);
				Assert.IsNotNull(clone);
				Assert.AreEqual(cloneRequest.Origin, clone.Origin);
				Assert.AreEqual(workingBranch, clone.Reference);
				Assert.IsNull(clone.RevisionInformation);
				Assert.IsNotNull(clone.ActiveJob);

				// throwing this small jobs consistency test in here
				await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
				var activeJobs = await JobsClient.ListActive(null, cancellationToken);
				var allJobs = await JobsClient.List(null, cancellationToken);

				Assert.IsTrue(activeJobs.Any(x => x.Id == clone.ActiveJob.Id));
				Assert.IsTrue(allJobs.Any(x => x.Id == clone.ActiveJob.Id));

				var targetActiveJob = activeJobs.First(x => x.Id == clone.ActiveJob.Id);

				if (!targetActiveJob.Progress.HasValue)
				{
					// give it a few more seconds
					targetActiveJob = await WaitForJobProgress(targetActiveJob, 30, cancellationToken);
					allJobs = await JobsClient.List(null, cancellationToken);
				}

				Assert.IsTrue(targetActiveJob.Progress.HasValue);
				Assert.IsTrue(allJobs.First(x => x.Id == clone.ActiveJob.Id).Progress.HasValue);

				return clone.ActiveJob;
			}
		}

		public async Task AbortLongCloneAndCloneSomethingQuick(Task<JobResponse> longCloneJob, CancellationToken cancellationToken)
		{
			await WaitForJobProgressThenCancel(await longCloneJob, 40, cancellationToken);

			var initalRepo = await repositoryClient.Read(cancellationToken);
			Assert.IsNotNull(initalRepo);
			Assert.IsNull(initalRepo.Origin);
			Assert.IsNull(initalRepo.Reference);
			Assert.IsNull(initalRepo.RevisionInformation);
			Assert.IsNull(initalRepo.ActiveJob);

			var secondRead = await repositoryClient.Read(cancellationToken);
			Assert.IsNotNull(secondRead);
			Assert.IsNull(secondRead.ActiveJob);

			const string Origin = "https://github.com/Cyberboss/common_core";
			var cloneRequest = new RepositoryCreateRequest
			{
				Origin = new Uri(Origin),
			};

			var clone = await repositoryClient.Clone(cloneRequest, cancellationToken);

			await WaitForJob(clone.ActiveJob, 60, false, null, cancellationToken);
			var readAfterClone = await repositoryClient.Read(cancellationToken);

			Assert.AreEqual(cloneRequest.Origin, readAfterClone.Origin);
			Assert.AreEqual("master", readAfterClone.Reference);
			Assert.IsNotNull(readAfterClone.RevisionInformation);
			Assert.IsNotNull(readAfterClone.RevisionInformation.ActiveTestMerges);
			Assert.AreEqual(0, readAfterClone.RevisionInformation.ActiveTestMerges.Count);
			Assert.IsNotNull(readAfterClone.RevisionInformation.CommitSha);
			Assert.IsNotNull(readAfterClone.RevisionInformation.OriginCommitSha);
			Assert.IsNotNull(readAfterClone.RevisionInformation.CompileJobs);
			Assert.AreEqual(0, readAfterClone.RevisionInformation.CompileJobs.Count);
			Assert.IsNotNull(readAfterClone.RevisionInformation.OriginCommitSha);
			Assert.IsNull(readAfterClone.RevisionInformation.PrimaryTestMerge);
			Assert.AreEqual(readAfterClone.RevisionInformation.CommitSha, readAfterClone.RevisionInformation.OriginCommitSha);
			Assert.AreNotEqual(default, readAfterClone.RevisionInformation.Timestamp);

			// Specific SHA
			await ApiAssert.ThrowsException<ApiConflictException, RepositoryResponse>(() => Checkout(new RepositoryUpdateRequest { Reference = "master", CheckoutSha = "286bb75" }, false, false, cancellationToken), ErrorCode.RepoMismatchShaAndReference);
			var updated = await Checkout(new RepositoryUpdateRequest { CheckoutSha = "286bb75" }, false, false, cancellationToken);

			// Fake SHA
			updated = await Checkout(new RepositoryUpdateRequest { CheckoutSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }, true, false, cancellationToken);

			// Fake ref
			updated = await Checkout(new RepositoryUpdateRequest { Reference = "TgsIntegrationTestFakeBranchNeverNameABranchThis" }, true, true, cancellationToken);

			// Back
			updated = await Checkout(new RepositoryUpdateRequest { Reference = "master" }, false, true, cancellationToken);

			await RecloneTest(cancellationToken);

			// enable the good shit if possible
			if (!String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN"))
				&& !(Boolean.TryParse(Environment.GetEnvironmentVariable("TGS_TEST_OD_EXCLUSIVE"), out var odExclusive) && odExclusive))
				await repositoryClient.Update(new RepositoryUpdateRequest
				{
					CreateGitHubDeployments = true,
					PostTestMergeComment = true,
					PushTestMergeCommits = true,
					AccessUser = "Cyberboss",
					AccessToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN"),
				}, cancellationToken);

			var prNumber = 2;
			await TestMergeTests(updated, prNumber, cancellationToken);
		}

		async ValueTask RecloneTest(CancellationToken cancellationToken)
		{
			var initialState = await repositoryClient.Read(cancellationToken);
			Assert.IsNotNull(initialState.Reference);
			Assert.IsNotNull(initialState.RevisionInformation);
			Assert.IsNotNull(initialState.RevisionInformation.CommitSha);
			Assert.IsNotNull(initialState.RevisionInformation.OriginCommitSha);

			var reclone = await repositoryClient.Reclone(cancellationToken);
			await WaitForJob(reclone.ActiveJob, 70, false, null, cancellationToken);

			var newState = await repositoryClient.Read(cancellationToken);
			Assert.AreEqual(initialState.Reference, newState.Reference);
			Assert.AreEqual(initialState.RevisionInformation.CommitSha, newState.RevisionInformation.CommitSha);
			Assert.AreEqual(initialState.RevisionInformation.OriginCommitSha, newState.RevisionInformation.OriginCommitSha);
		}

		async ValueTask<RepositoryResponse> Checkout(RepositoryUpdateRequest updated, bool expectFailure, bool isRef, CancellationToken cancellationToken)
		{
			var newRef = isRef ? updated.Reference : updated.CheckoutSha;
			var checkingOut = await repositoryClient.Update(updated, cancellationToken);
			Assert.IsNotNull(checkingOut.ActiveJob);

			await WaitForJob(checkingOut.ActiveJob, 30, expectFailure, null, cancellationToken);
			var result = await repositoryClient.Read(cancellationToken);
			if (!expectFailure)
				if (isRef)
					Assert.AreEqual(newRef, result.Reference);
				else
					Assert.IsTrue(result.RevisionInformation.CommitSha.StartsWith(newRef, StringComparison.OrdinalIgnoreCase));

			Assert.AreEqual(result.RevisionInformation.CommitSha, result.RevisionInformation.OriginCommitSha);

			return result;
		}

		async Task TestMergeTests(RepositoryResponse repository, int prNumber, CancellationToken cancellationToken)
		{
			var orignCommit = repository.RevisionInformation.OriginCommitSha;

			var numberOnlyMerging = await repositoryClient.Update(new RepositoryUpdateRequest
			{
				NewTestMerges = new List<TestMergeParameters>
				{
					new TestMergeParameters
					{
						Number = prNumber
					}
				}
			}, cancellationToken);
			Assert.IsNotNull(numberOnlyMerging.ActiveJob);
			Assert.IsTrue(numberOnlyMerging.ActiveJob.Description.Contains(prNumber.ToString()));

			await WaitForJob(numberOnlyMerging.ActiveJob, 20, false, null, cancellationToken);

			var withMerge = await repositoryClient.Read(cancellationToken);
			Assert.AreEqual(repository.Reference, withMerge.Reference);
			Assert.AreEqual(1, withMerge.RevisionInformation.ActiveTestMerges.Count);
			Assert.AreEqual(prNumber, withMerge.RevisionInformation.ActiveTestMerges.First().Number);
			Assert.AreEqual(prNumber, withMerge.RevisionInformation.PrimaryTestMerge.Number);
			var prRevision = withMerge.RevisionInformation.PrimaryTestMerge.TargetCommitSha;
			Assert.IsNotNull(prRevision);
			Assert.IsNotNull(withMerge.RevisionInformation.PrimaryTestMerge.MergedBy);
			Assert.IsNotNull(withMerge.RevisionInformation.PrimaryTestMerge.MergedAt);
			Assert.IsNotNull(withMerge.RevisionInformation.PrimaryTestMerge.Author);
			Assert.IsNull(withMerge.RevisionInformation.PrimaryTestMerge.Comment);
			Assert.IsNotNull(withMerge.RevisionInformation.PrimaryTestMerge.TitleAtMerge);
			Assert.IsNotNull(withMerge.RevisionInformation.PrimaryTestMerge.BodyAtMerge);
			if (withMerge.RevisionInformation.PrimaryTestMerge.Url != "GITHUB API ERROR: RATE LIMITED")
				Assert.AreEqual($"https://github.com/Cyberboss/common_core/pull/{prNumber}", withMerge.RevisionInformation.PrimaryTestMerge.Url);
			Assert.AreEqual(orignCommit, withMerge.RevisionInformation.OriginCommitSha);
			Assert.AreNotEqual(orignCommit, withMerge.RevisionInformation.CommitSha);

			// Reset, do it again with a comment and specific sha
			var updateRequ = new RepositoryUpdateRequest
			{
				UpdateFromOrigin = true,
				Reference = repository.Reference,
				NewTestMerges = new List<TestMergeParameters>
				{
					new TestMergeParameters
					{
						Number = prNumber,
						Comment = "asdffdsa",
						TargetCommitSha = prRevision
					}
				}
			};

			var mergingAgain = await repositoryClient.Update(updateRequ, cancellationToken);
			Assert.IsNotNull(mergingAgain.ActiveJob);
			await WaitForJob(mergingAgain.ActiveJob, 60, false, null, cancellationToken);

			var final = await repositoryClient.Read(cancellationToken);
			Assert.AreEqual("asdffdsa", final.RevisionInformation.PrimaryTestMerge.Comment);
			Assert.AreEqual(prNumber, final.RevisionInformation.PrimaryTestMerge.Number);
			Assert.AreEqual(prRevision, final.RevisionInformation.PrimaryTestMerge.TargetCommitSha);
		}

		public async Task RunPostTest(CancellationToken cancellationToken)
		{
			var deleting = await repositoryClient.Delete(cancellationToken);
			Assert.IsNotNull(deleting.ActiveJob);

			await WaitForJob(deleting.ActiveJob, 60, false, null, cancellationToken);
			var deleted = await repositoryClient.Read(cancellationToken);

			Assert.IsNull(deleted.Origin);
			Assert.IsNull(deleted.Reference);
			Assert.IsNull(deleted.RevisionInformation);
		}
	}
}
