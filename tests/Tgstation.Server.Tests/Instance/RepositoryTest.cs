using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Instance
{
	sealed class RepositoryTest : JobsRequiredTest
	{
		readonly IRepositoryClient repositoryClient;

		public RepositoryTest(IRepositoryClient repositoryClient, IJobsClient jobsClient)
			: base(jobsClient)
		{
			this.repositoryClient = repositoryClient ?? throw new ArgumentNullException(nameof(repositoryClient));
		}

		public async Task RunPreWatchdog(CancellationToken cancellationToken)
		{
			const string GitHubRef = "TGS4_GITHUB_REF";
			var branchSourceEnvVars = new List<string>
			{
				"TGS4_TEST_BRANCH",
				"APPVEYOR_REPO_BRANCH",
				"TRAVIS_BRANCH",
				GitHubRef
			};

			string workingBranch = null;
			foreach (var envVarName in branchSourceEnvVars)
			{
				var envVar = Environment.GetEnvironmentVariable(envVarName);
				if (!String.IsNullOrWhiteSpace(envVar))
				{
					if(envVarName == GitHubRef)
						envVar = envVar.Substring("refs/heads/".Length);

					workingBranch = envVar;
					Console.WriteLine($"TEST: Set working branch to '{workingBranch}' from env var '{envVarName}'");
					break;
				}
			}

			if (workingBranch == null)
			{
				workingBranch = "master";
				Console.WriteLine($"TEST: Set working branch to default '{workingBranch}'");
			}

			var initalRepo = await repositoryClient.Read(cancellationToken);
			Assert.IsNotNull(initalRepo);
			Assert.IsNull(initalRepo.Origin);
			Assert.IsNull(initalRepo.Reference);
			Assert.IsNull(initalRepo.RevisionInformation);
			Assert.IsNull(initalRepo.ActiveJob);

			const string Origin = "https://github.com/tgstation/tgstation-server";
			initalRepo.Origin = Origin;
			initalRepo.Reference = workingBranch;

			var clone = await repositoryClient.Clone(initalRepo, cancellationToken).ConfigureAwait(false);
			await ApiAssert.ThrowsException<ConflictException>(() => repositoryClient.Read(cancellationToken), ErrorCode.RepoCloning);
			Assert.IsNotNull(clone);
			Assert.AreEqual(Origin, clone.Origin);
			Assert.AreEqual(workingBranch, clone.Reference);
			Assert.IsNull(clone.RevisionInformation);
			Assert.IsNotNull(clone.ActiveJob);

			await WaitForJobProgressThenCancel(clone.ActiveJob, 20, cancellationToken).ConfigureAwait(false);

			var secondRead = await repositoryClient.Read(cancellationToken).ConfigureAwait(false);
			Assert.IsNotNull(secondRead);
			Assert.IsNull(secondRead.ActiveJob);

			clone = await repositoryClient.Clone(initalRepo, cancellationToken).ConfigureAwait(false);

			await WaitForJob(clone.ActiveJob, 600, false, null, cancellationToken).ConfigureAwait(false);
			var cloned = await repositoryClient.Read(cancellationToken);

			Assert.AreEqual(Origin, cloned.Origin);
			Assert.AreEqual(workingBranch, cloned.Reference);
			Assert.IsNotNull(cloned.RevisionInformation);
			Assert.IsNotNull(cloned.RevisionInformation.ActiveTestMerges);
			Assert.AreEqual(0, cloned.RevisionInformation.ActiveTestMerges.Count);
			Assert.IsNotNull(cloned.RevisionInformation.CommitSha);
			Assert.IsNotNull(cloned.RevisionInformation.OriginCommitSha);
			Assert.IsNotNull(cloned.RevisionInformation.CompileJobs);
			Assert.AreEqual(0, cloned.RevisionInformation.CompileJobs.Count);
			Assert.IsNotNull(cloned.RevisionInformation.OriginCommitSha);
			Assert.IsNull(cloned.RevisionInformation.PrimaryTestMerge);
			Assert.AreEqual(cloned.RevisionInformation.CommitSha, cloned.RevisionInformation.OriginCommitSha);

			cloned.Origin = "https://github.com/tgstation/tgstation";
			await ApiAssert.ThrowsException<ApiConflictException>(() => repositoryClient.Update(cloned, cancellationToken), ErrorCode.RepoCantChangeOrigin);
			cloned.Origin = Origin;

			// checkout V3 and back
			cloned.Reference = "V3";
			var updated = await Checkout(cloned, false, true, cancellationToken);

			// Specific SHA
			updated.CheckoutSha = "f43f5bd";
			await ApiAssert.ThrowsException<ApiConflictException>(() => Checkout(updated, false, false, cancellationToken), ErrorCode.RepoMismatchShaAndReference);
			updated.Reference = null;
			updated = await Checkout(updated, false, false, cancellationToken);

			// Fake SHA
			updated.Reference = null;
			updated.CheckoutSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
			updated = await Checkout(updated, true, false, cancellationToken);

			// Fake ref
			updated.Reference = "Tgs4IntegrationTestFakeBranchNeverNameABranchThis";
			updated = await Checkout(updated, true, true, cancellationToken);

			// Back
			updated.Reference = workingBranch;
			updated = await Checkout(updated, false, true, cancellationToken);

			var testPRString = Environment.GetEnvironmentVariable("TGS4_TEST_PULL_REQUEST_NUMBER");
			if (String.IsNullOrWhiteSpace(testPRString))
				testPRString = Environment.GetEnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER");
			if (String.IsNullOrWhiteSpace(testPRString))
				testPRString = Environment.GetEnvironmentVariable("TRAVIS_PULL_REQUEST");

			if (String.IsNullOrWhiteSpace(testPRString))
				if (workingBranch == "dev")
					testPRString = "957";
				else
					testPRString = "958";

			if (!int.TryParse(testPRString, out var prNumber))
				Assert.Inconclusive($"Invalid PR #: {testPRString}");
			await TestMergeTests(updated, prNumber, cancellationToken);
		}

		async Task<Repository> Checkout(Repository updated, bool expectFailure, bool isRef, CancellationToken cancellationToken)
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

		async Task TestMergeTests(Repository repository, int prNumber, CancellationToken cancellationToken)
		{
			repository.NewTestMerges = new List<TestMergeParameters>
			{
				new TestMergeParameters
				{
					Number = prNumber
				}
			};

			var orignCommit = repository.RevisionInformation.OriginCommitSha;

			var numberOnlyMerging = await repositoryClient.Update(repository, cancellationToken);
			Assert.IsNotNull(numberOnlyMerging.ActiveJob);
			Assert.IsTrue(numberOnlyMerging.ActiveJob.Description.Contains(prNumber.ToString()));

			await WaitForJob(numberOnlyMerging.ActiveJob, 20, false, null,cancellationToken);

			var withMerge = await repositoryClient.Read(cancellationToken);
			Assert.AreEqual(repository.Reference, withMerge.Reference);
			Assert.AreEqual(1, withMerge.RevisionInformation.ActiveTestMerges.Count);
			Assert.AreEqual(prNumber, withMerge.RevisionInformation.ActiveTestMerges.First().Number);
			Assert.AreEqual(prNumber, withMerge.RevisionInformation.PrimaryTestMerge.Number);
			var prRevision = withMerge.RevisionInformation.PrimaryTestMerge.PullRequestRevision;
			Assert.IsNotNull(prRevision);
			Assert.IsNotNull(withMerge.RevisionInformation.PrimaryTestMerge.MergedBy);
			Assert.IsNotNull(withMerge.RevisionInformation.PrimaryTestMerge.MergedAt);
			Assert.IsNotNull(withMerge.RevisionInformation.PrimaryTestMerge.Author);
			Assert.IsNull(withMerge.RevisionInformation.PrimaryTestMerge.Comment);
			Assert.IsNotNull(withMerge.RevisionInformation.PrimaryTestMerge.TitleAtMerge);
			Assert.IsNotNull(withMerge.RevisionInformation.PrimaryTestMerge.BodyAtMerge);
			if (withMerge.RevisionInformation.PrimaryTestMerge.Url != "REMOTE API ERROR: RATE LIMITED")
				Assert.AreEqual($"https://github.com/tgstation/tgstation-server/pull/{prNumber}", withMerge.RevisionInformation.PrimaryTestMerge.Url);
			Assert.AreEqual(orignCommit, withMerge.RevisionInformation.OriginCommitSha);
			Assert.AreNotEqual(orignCommit, withMerge.RevisionInformation.CommitSha);

			// Reset, do it again with a comment and specific sha
			withMerge.UpdateFromOrigin = true;
			withMerge.Reference = repository.Reference;
			withMerge.NewTestMerges = new List<TestMergeParameters>
			{
				new TestMergeParameters
				{
					Number = prNumber,
					Comment = "asdffdsa",
					PullRequestRevision = prRevision
				}
			};

			var mergingAgain = await repositoryClient.Update(withMerge, cancellationToken);
			Assert.IsNotNull(mergingAgain.ActiveJob);
			await WaitForJob(mergingAgain.ActiveJob, 30, false, null, cancellationToken);

			var final = await repositoryClient.Read(cancellationToken);
			Assert.AreEqual("asdffdsa", final.RevisionInformation.PrimaryTestMerge.Comment);
			Assert.AreEqual(prNumber, final.RevisionInformation.PrimaryTestMerge.Number);
			Assert.AreEqual(prRevision, final.RevisionInformation.PrimaryTestMerge.PullRequestRevision);
		}

		public async Task RunPostTest(CancellationToken cancellationToken)
		{


			var deleting = await repositoryClient.Delete(cancellationToken);
			Assert.IsNotNull(deleting.ActiveJob);

			await WaitForJob(deleting.ActiveJob, 60, false, null, cancellationToken).ConfigureAwait(false);
			var deleted = await repositoryClient.Read(cancellationToken).ConfigureAwait(false);

			Assert.IsNull(deleted.Origin);
			Assert.IsNull(deleted.Reference);
			Assert.IsNull(deleted.RevisionInformation);
		}
	}
}
