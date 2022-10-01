using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <inheritdoc />
	sealed class Repository : IRepository
	{
		/// <summary>
		/// The default username for committers.
		/// </summary>
		public const string DefaultCommitterName = "tgstation-server";

		/// <summary>
		/// The default password for committers.
		/// </summary>
		public const string DefaultCommitterEmail = "tgstation-server@users.noreply.github.com";

		/// <summary>
		/// Template error message for when tracking of the most recent origin commit fails.
		/// </summary>
		public const string OriginTrackingErrorTemplate = "Unable to determine most recent origin commit of {0}. Marking it as an origin commit. This may result in invalid git metadata until the next hard reset to an origin reference.";

		/// <summary>
		/// The branch name used for publishing testmerge commits.
		/// </summary>
		public const string RemoteTemporaryBranchName = "___TGSTempBranch";

		/// <summary>
		/// Used when a reference cannot be determined.
		/// </summary>
		const string UnknownReference = "<UNKNOWN>";

		/// <inheritdoc />
		public RemoteGitProvider? RemoteGitProvider => gitRemoteFeatures.RemoteGitProvider;

		/// <inheritdoc />
		public string RemoteRepositoryOwner => gitRemoteFeatures.RemoteRepositoryOwner;

		/// <inheritdoc />
		public string RemoteRepositoryName => gitRemoteFeatures.RemoteRepositoryName;

		/// <inheritdoc />
		public bool Tracking => Reference != null && libGitRepo.Head.IsTracking;

		/// <inheritdoc />
		public string Head => libGitRepo.Head.Tip.Sha;

		/// <inheritdoc />
		public string Reference => libGitRepo.Head.FriendlyName;

		/// <inheritdoc />
		public Uri Origin => new Uri(libGitRepo.Network.Remotes.First().Url);

		/// <summary>
		/// The <see cref="LibGit2Sharp.IRepository"/> for the <see cref="Repository"/>.
		/// </summary>
		readonly LibGit2Sharp.IRepository libGitRepo;

		/// <summary>
		/// The <see cref="ILibGit2Commands"/> for the <see cref="Repository"/>.
		/// </summary>
		readonly ILibGit2Commands commands;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="Repository"/>.
		/// </summary>
		readonly IIOManager ioMananger;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="Repository"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="ICredentialsProvider"/> for the <see cref="Repository"/>.
		/// </summary>
		readonly ICredentialsProvider credentialsProvider;

		/// <summary>
		/// The <see cref="IGitRemoteFeatures"/> for the <see cref="Repository"/>.
		/// </summary>
		readonly IGitRemoteFeatures gitRemoteFeatures;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Repository"/>.
		/// </summary>
		readonly ILogger<Repository> logger;

		/// <summary>
		/// <see cref="Action"/> to be taken when <see cref="Dispose"/> is called.
		/// </summary>
		readonly Action onDispose;

		/// <summary>
		/// If the <see cref="Repository"/> was disposed.
		/// </summary>
		bool disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="Repository"/> class.
		/// </summary>
		/// <param name="libGitRepo">The value of <see cref="libGitRepo"/>.</param>
		/// <param name="commands">The value of <see cref="commands"/>.</param>
		/// <param name="ioMananger">The value of <see cref="ioMananger"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="credentialsProvider">The value of <see cref="credentialsProvider"/>.</param>
		/// <param name="gitRemoteFeaturesFactory">The <see cref="IGitRemoteFeaturesFactory"/> to provide the value of <see cref="gitRemoteFeatures"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="onDispose">The value if <see cref="onDispose"/>.</param>
		public Repository(
			LibGit2Sharp.IRepository libGitRepo,
			ILibGit2Commands commands,
			IIOManager ioMananger,
			IEventConsumer eventConsumer,
			ICredentialsProvider credentialsProvider,
			IGitRemoteFeaturesFactory gitRemoteFeaturesFactory,
			ILogger<Repository> logger,
			Action onDispose)
		{
			this.libGitRepo = libGitRepo ?? throw new ArgumentNullException(nameof(libGitRepo));
			this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
			this.ioMananger = ioMananger ?? throw new ArgumentNullException(nameof(ioMananger));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.credentialsProvider = credentialsProvider ?? throw new ArgumentNullException(nameof(credentialsProvider));
			if (gitRemoteFeaturesFactory == null)
				throw new ArgumentNullException(nameof(gitRemoteFeaturesFactory));

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));

			gitRemoteFeatures = gitRemoteFeaturesFactory.CreateGitRemoteFeatures(this);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (onDispose)
			{
				if (disposed)
					return;

				disposed = true;
			}

			logger.LogTrace("Disposing...");
			libGitRepo.Dispose();
			onDispose();
		}

		/// <inheritdoc />
#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task<bool?> AddTestMerge(
			TestMergeParameters testMergeParameters,
			string committerName,
			string committerEmail,
			string username,
			string password,
			bool updateSubmodules,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken)
		{
			if (testMergeParameters == null)
				throw new ArgumentNullException(nameof(testMergeParameters));
			if (committerName == null)
				throw new ArgumentNullException(nameof(committerName));
			if (committerEmail == null)
				throw new ArgumentNullException(nameof(committerEmail));
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));

			logger.LogDebug(
				"Begin AddTestMerge: #{0} at {1} ({2}) by <{3} ({4})>",
				testMergeParameters.Number,
				testMergeParameters.TargetCommitSha?.Substring(0, 7),
				testMergeParameters.Comment,
				committerName,
				committerEmail);

			if (RemoteGitProvider == Api.Models.RemoteGitProvider.Unknown)
				throw new InvalidOperationException("Cannot test merge with an Unknown RemoteGitProvider!");

			var commitMessage = String.Format(
				CultureInfo.InvariantCulture,
				"TGS Test Merge (#{0}){1}{2}",
				testMergeParameters.Number,
				testMergeParameters.Comment != null
					? Environment.NewLine
					: String.Empty,
				testMergeParameters.Comment ?? String.Empty);

			var testMergeBranchName = String.Format(CultureInfo.InvariantCulture, "tm-{0}", testMergeParameters.Number);
			var localBranchName = String.Format(CultureInfo.InvariantCulture, gitRemoteFeatures.TestMergeLocalBranchNameFormatter, testMergeParameters.Number, testMergeBranchName);

			var refSpec = String.Format(CultureInfo.InvariantCulture, gitRemoteFeatures.TestMergeRefSpecFormatter, testMergeParameters.Number, testMergeBranchName);
			var refSpecList = new List<string> { refSpec };
			var logMessage = String.Format(CultureInfo.InvariantCulture, "Test merge #{0}", testMergeParameters.Number);

			var originalCommit = libGitRepo.Head;

			MergeResult result = null;

			var progressFactor = 1.0 / (updateSubmodules ? 3 : 2);

			var sig = new Signature(new Identity(committerName, committerEmail), DateTimeOffset.UtcNow);
			await Task.Factory.StartNew(
				() =>
				{
					try
					{
						try
						{
							logger.LogTrace("Fetching refspec {0}...", refSpec);

							var remote = libGitRepo.Network.Remotes.First();
							commands.Fetch(
								libGitRepo,
								refSpecList,
								remote,
								new FetchOptions
								{
									Prune = true,
									OnProgress = (a) => !cancellationToken.IsCancellationRequested,
									OnTransferProgress = TransferProgressHandler(
										progressReporter.CreateSection($"Fetch {refSpec}", progressFactor),
										cancellationToken),
									OnUpdateTips = (a, b, c) => !cancellationToken.IsCancellationRequested,
									CredentialsProvider = credentialsProvider.GenerateCredentialsHandler(username, password),
								},
								logMessage);
						}
						catch (UserCancelledException)
						{
						}
						catch (LibGit2SharpException ex)
						{
							credentialsProvider.CheckBadCredentialsException(ex);
						}

						cancellationToken.ThrowIfCancellationRequested();

						libGitRepo.RemoveUntrackedFiles();

						cancellationToken.ThrowIfCancellationRequested();

						var objectName = testMergeParameters.TargetCommitSha ?? localBranchName;
						var gitObject = libGitRepo.Lookup(objectName);
						if (gitObject == null)
							throw new JobException($"Could not find object to merge: {objectName}");

						testMergeParameters.TargetCommitSha = gitObject.Sha;

						cancellationToken.ThrowIfCancellationRequested();

						logger.LogTrace("Merging {0} into {1}...", testMergeParameters.TargetCommitSha[..7], Reference);

						result = libGitRepo.Merge(testMergeParameters.TargetCommitSha, sig, new MergeOptions
						{
							CommitOnSuccess = commitMessage == null,
							FailOnConflict = true,
							FastForwardStrategy = FastForwardStrategy.NoFastForward,
							SkipReuc = true,
							OnCheckoutProgress = CheckoutProgressHandler(
								progressReporter.CreateSection($"Merge {testMergeParameters.TargetCommitSha[..7]}", progressFactor)),
						});
					}
					finally
					{
						libGitRepo.Branches.Remove(localBranchName);
					}

					cancellationToken.ThrowIfCancellationRequested();

					if (result.Status == MergeStatus.Conflicts)
					{
						var revertTo = originalCommit.CanonicalName ?? originalCommit.Tip.Sha;
						logger.LogDebug("Merge conflict, aborting and reverting to {0}", revertTo);
						progressReporter.ReportProgress(0);
						RawCheckout(revertTo, progressReporter.CreateSection("Hard Reset to {revertTo}", 1.0), cancellationToken);
						cancellationToken.ThrowIfCancellationRequested();
					}

					libGitRepo.RemoveUntrackedFiles();
				},
				cancellationToken,
				DefaultIOManager.BlockingTaskCreationOptions,
				TaskScheduler.Current)
				;

			if (result.Status == MergeStatus.Conflicts)
			{
				await eventConsumer.HandleEvent(
					EventType.RepoMergeConflict,
					new List<string>
					{
						originalCommit.Tip.Sha,
						testMergeParameters.TargetCommitSha,
						originalCommit.FriendlyName ?? UnknownReference,
						testMergeBranchName,
					},
					cancellationToken)
					;
				return null;
			}

			if (result.Status != MergeStatus.UpToDate)
			{
				logger.LogTrace("Committing merge: \"{0}\"...", commitMessage);
				await Task.Factory.StartNew(
					() => libGitRepo.Commit(commitMessage, sig, sig, new CommitOptions
					{
						PrettifyMessage = true,
					}),
					cancellationToken,
					DefaultIOManager.BlockingTaskCreationOptions,
					TaskScheduler.Current)
					;

				if (updateSubmodules)
				{
					await UpdateSubmodules(
						progressReporter.CreateSection("Update Submodules", progressFactor),
						username,
						password,
						cancellationToken);
				}
			}

			await eventConsumer.HandleEvent(
				EventType.RepoAddTestMerge,
				new List<string>
				{
					testMergeParameters.Number.ToString(CultureInfo.InvariantCulture),
					testMergeParameters.TargetCommitSha,
					testMergeParameters.Comment,
				},
				cancellationToken)
				;

			return result.Status != MergeStatus.NonFastForward;
		}
#pragma warning restore CA1506

		/// <inheritdoc />
		public async Task CheckoutObject(
			string committish,
			string username,
			string password,
			bool updateSubmodules,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken)
		{
			if (committish == null)
				throw new ArgumentNullException(nameof(committish));
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));
			logger.LogDebug("Checkout object: {0}...", committish);
			await eventConsumer.HandleEvent(EventType.RepoCheckout, new List<string> { committish }, cancellationToken);
			await Task.Factory.StartNew(
				() =>
				{
					libGitRepo.RemoveUntrackedFiles();
					RawCheckout(
						committish,
						progressReporter.CreateSection(null, updateSubmodules ? 2.0 / 3 : 1.0),
						cancellationToken);
				},
				cancellationToken,
				DefaultIOManager.BlockingTaskCreationOptions,
				TaskScheduler.Current)
				;

			if (updateSubmodules)
				await UpdateSubmodules(
					progressReporter.CreateSection(null, 1.0 / 3),
					username,
					password,
					cancellationToken);
		}

		/// <inheritdoc />
		public async Task FetchOrigin(string username, string password, JobProgressReporter progressReporter, CancellationToken cancellationToken)
		{
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));
			logger.LogDebug("Fetch origin...");
			await eventConsumer.HandleEvent(EventType.RepoFetch, Enumerable.Empty<string>(), cancellationToken);
			await Task.Factory.StartNew(
				() =>
				{
					var remote = libGitRepo.Network.Remotes.First();
					try
					{
						commands.Fetch(
							libGitRepo,
							remote
								.FetchRefSpecs
								.Select(x => x.Specification),
							remote,
							new FetchOptions
							{
								Prune = true,
								OnProgress = (a) => !cancellationToken.IsCancellationRequested,
								OnTransferProgress = TransferProgressHandler(progressReporter.CreateSection("Fetch Origin", 1.0), cancellationToken),
								OnUpdateTips = (a, b, c) => !cancellationToken.IsCancellationRequested,
								CredentialsProvider = credentialsProvider.GenerateCredentialsHandler(username, password),
							},
							"Fetch origin commits");
					}
					catch (UserCancelledException)
					{
						cancellationToken.ThrowIfCancellationRequested();
					}
					catch (LibGit2SharpException ex)
					{
						credentialsProvider.CheckBadCredentialsException(ex);
					}
				},
				cancellationToken,
				DefaultIOManager.BlockingTaskCreationOptions,
				TaskScheduler.Current)
				;
		}

		/// <inheritdoc />
		public async Task ResetToOrigin(
			string username,
			string password,
			bool updateSubmodules,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken)
		{
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));
			if (!Tracking)
				throw new JobException(ErrorCode.RepoReferenceRequired);
			logger.LogTrace("Reset to origin...");
			var trackedBranch = libGitRepo.Head.TrackedBranch;
			await eventConsumer.HandleEvent(EventType.RepoResetOrigin, new List<string> { trackedBranch.FriendlyName, trackedBranch.Tip.Sha }, cancellationToken);
			await ResetToSha(
				trackedBranch.Tip.Sha,
				progressReporter.CreateSection(null, updateSubmodules ? 2.0 / 3 : 1.0),
				cancellationToken)
				;

			if (updateSubmodules)
				await UpdateSubmodules(
					progressReporter.CreateSection(null, 1.0 / 3),
					username,
					password,
					cancellationToken);
		}

		/// <inheritdoc />
		public Task ResetToSha(string sha, JobProgressReporter progressReporter, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				if (sha == null)
					throw new ArgumentNullException(nameof(sha));
				if (progressReporter == null)
					throw new ArgumentNullException(nameof(progressReporter));

				logger.LogDebug("Reset to sha: {0}", sha.Substring(0, 7));

				libGitRepo.RemoveUntrackedFiles();
				cancellationToken.ThrowIfCancellationRequested();

				var gitObject = libGitRepo.Lookup(sha, ObjectType.Commit);
				cancellationToken.ThrowIfCancellationRequested();

				if (gitObject == null)
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot reset to non-existent SHA: {0}", sha));

				libGitRepo.Reset(ResetMode.Hard, gitObject.Peel<Commit>(), new CheckoutOptions
				{
					OnCheckoutProgress = CheckoutProgressHandler(progressReporter.CreateSection($"Reset to {gitObject.Sha}", 1.0)),
				});
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public async Task CopyTo(string path, CancellationToken cancellationToken)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			logger.LogTrace("Copying to {0}...", path);
			await ioMananger.CopyDirectory(ioMananger.ResolvePath(), path, new List<string> { ".git" }, cancellationToken);
		}

		/// <inheritdoc />
		public Task<string> GetOriginSha(CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				if (!Tracking)
					throw new JobException(ErrorCode.RepoReferenceRequired);

				cancellationToken.ThrowIfCancellationRequested();

				return libGitRepo.Head.TrackedBranch.Tip.Sha;
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public async Task<bool?> MergeOrigin(
			string committerName,
			string committerEmail,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken)
		{
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));

			MergeResult result = null;
			Branch trackedBranch = null;

			var oldHead = libGitRepo.Head;
			var oldTip = oldHead.Tip;

			await Task.Factory.StartNew(
				() =>
				{
					if (!Tracking)
						throw new JobException(ErrorCode.RepoReferenceRequired);

					libGitRepo.RemoveUntrackedFiles();

					cancellationToken.ThrowIfCancellationRequested();

					trackedBranch = libGitRepo.Head.TrackedBranch;
					logger.LogDebug(
						"Merge origin/{0}: <{1} ({2})>",
						trackedBranch.FriendlyName,
						committerName,
						committerEmail);
					result = libGitRepo.Merge(trackedBranch, new Signature(committerName, committerEmail, DateTimeOffset.UtcNow), new MergeOptions
					{
						CommitOnSuccess = true,
						FailOnConflict = true,
						FastForwardStrategy = FastForwardStrategy.Default,
						SkipReuc = true,
						OnCheckoutProgress = CheckoutProgressHandler(progressReporter.CreateSection("Merge Origin", 1.0)),
					});

					cancellationToken.ThrowIfCancellationRequested();

					if (result.Status == MergeStatus.Conflicts)
					{
						logger.LogDebug("Merge conflict, aborting and reverting to {0}", oldHead.FriendlyName);
						progressReporter.ReportProgress(0);
						libGitRepo.Reset(ResetMode.Hard, oldTip, new CheckoutOptions
						{
							OnCheckoutProgress = CheckoutProgressHandler(progressReporter.CreateSection($"Hard Reset to {oldHead.FriendlyName}", 1.0)),
						});
						cancellationToken.ThrowIfCancellationRequested();
					}

					libGitRepo.RemoveUntrackedFiles();
				},
				cancellationToken,
				DefaultIOManager.BlockingTaskCreationOptions,
				TaskScheduler.Current)
				;

			if (result.Status == MergeStatus.Conflicts)
			{
				await eventConsumer.HandleEvent(EventType.RepoMergeConflict, new List<string> { oldTip.Sha, trackedBranch.Tip.Sha, oldHead.FriendlyName ?? UnknownReference, trackedBranch.FriendlyName }, cancellationToken);
				return null;
			}

			return result.Status == MergeStatus.FastForward;
		}

		/// <inheritdoc />
		public async Task<bool> Sychronize(
			string username,
			string password,
			string committerName,
			string committerEmail,
			JobProgressReporter progressReporter,
			bool synchronizeTrackedBranch,
			CancellationToken cancellationToken)
		{
			if (committerName == null)
				throw new ArgumentNullException(nameof(committerName));
			if (committerEmail == null)
				throw new ArgumentNullException(nameof(committerEmail));
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));

			if (username == null && password == null)
			{
				logger.LogTrace("Not synchronizing due to lack of credentials!");
				return false;
			}

			logger.LogTrace("Begin Synchronize...");

			if (username == null)
				throw new ArgumentNullException(nameof(username));
			if (password == null)
				throw new ArgumentNullException(nameof(password));

			var startHead = Head;

			logger.LogTrace("Configuring <{0} ({1})> as author/committer", committerName, committerEmail);
			await Task.Factory.StartNew(
				() =>
				{
					libGitRepo.Config.Set("user.name", committerName);
					cancellationToken.ThrowIfCancellationRequested();
					libGitRepo.Config.Set("user.email", committerEmail);
				},
				cancellationToken,
				DefaultIOManager.BlockingTaskCreationOptions,
				TaskScheduler.Current);

			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				await eventConsumer.HandleEvent(
					EventType.RepoPreSynchronize,
					new List<string>
					{
						ioMananger.ResolvePath(),
					},
					cancellationToken)
					;
			}
			finally
			{
				logger.LogTrace("Resetting and cleaning untracked files...");
				await Task.Factory.StartNew(
					() =>
					{
						libGitRepo.Reset(ResetMode.Hard, libGitRepo.Head.Tip, new CheckoutOptions
						{
							OnCheckoutProgress = CheckoutProgressHandler(progressReporter.CreateSection("Hard reset and remove untracked files", 0.1)),
						});
						cancellationToken.ThrowIfCancellationRequested();
						libGitRepo.RemoveUntrackedFiles();
					},
					cancellationToken,
					DefaultIOManager.BlockingTaskCreationOptions,
					TaskScheduler.Current)
					;
			}

			var remainingProgressFactor = 0.9;
			if (!synchronizeTrackedBranch)
			{
				await PushHeadToTemporaryBranch(
					username,
					password,
					progressReporter.CreateSection("Push to temporary branch", remainingProgressFactor),
					cancellationToken);
				return false;
			}

			var sameHead = Head == startHead;
			if (sameHead || !Tracking)
			{
				logger.LogTrace("Aborted synchronize due to {0}!", sameHead ? "lack of changes" : "not being on tracked reference");
				return false;
			}

			logger.LogInformation("Synchronizing with origin...");

			return await Task.Factory.StartNew(
				() =>
				{
					var remote = libGitRepo.Network.Remotes.First();
					try
					{
						libGitRepo.Network.Push(
							libGitRepo.Head,
							GeneratePushOptions(
								progressReporter.CreateSection("Push to origin", remainingProgressFactor),
								username,
								password,
								cancellationToken));
						return true;
					}
					catch (NonFastForwardException)
					{
						logger.LogInformation("Synchronize aborted, non-fast forward!");
						return false;
					}
					catch (UserCancelledException e)
					{
						cancellationToken.ThrowIfCancellationRequested();
						throw new InvalidOperationException("Caught UserCancelledException without cancellationToken triggering", e);
					}
					catch (LibGit2SharpException e)
					{
						logger.LogWarning(e, "Unable to make synchronization push!");
						return false;
					}
				},
				cancellationToken,
				DefaultIOManager.BlockingTaskCreationOptions,
				TaskScheduler.Current)
				;
		}

		/// <inheritdoc />
		public Task<bool> IsSha(string committish, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				// check if it's a tag
				var gitObject = libGitRepo.Lookup(committish, ObjectType.Tag);
				if (gitObject != null)
					return false;
				cancellationToken.ThrowIfCancellationRequested();

				// check if it's a branch
				if (libGitRepo.Branches[committish] != null)
					return false;
				cancellationToken.ThrowIfCancellationRequested();

				// err on the side of references, if we can't look it up, assume its a reference
				if (libGitRepo.Lookup<Commit>(committish) != null)
					return true;
				return false;
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task<bool> ShaIsParent(string sha, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				var targetCommit = libGitRepo.Lookup<Commit>(sha);
				if (targetCommit == null)
				{
					logger.LogTrace("Commit {0} not found in repository", sha);
					return false;
				}

				cancellationToken.ThrowIfCancellationRequested();
				var startSha = Head;
				var mergeResult = libGitRepo.Merge(
					targetCommit,
					new Signature(
						DefaultCommitterName,
						DefaultCommitterEmail,
						DateTimeOffset.UtcNow),
					new MergeOptions
					{
						FastForwardStrategy = FastForwardStrategy.FastForwardOnly,
						FailOnConflict = true,
					});

				if (mergeResult.Status == MergeStatus.UpToDate)
					return true;

				commands.Checkout(
					libGitRepo,
					new CheckoutOptions
					{
						CheckoutModifiers = CheckoutModifiers.Force,
					},
					startSha);

				return false;
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task<Models.TestMerge> GetTestMerge(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken) => gitRemoteFeatures.GetTestMerge(
				parameters,
				repositorySettings,
				cancellationToken);

		/// <inheritdoc />
		public Task<DateTimeOffset> TimestampCommit(string sha, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				if (sha == null)
					throw new ArgumentNullException(nameof(sha));

				var commit = libGitRepo.Lookup<Commit>(sha);
				if (commit == null)
					throw new JobException($"Commit {sha} does not exist in the repository!");

				return commit.Committer.When;
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <summary>
		/// Runs a blocking force checkout to <paramref name="committish"/>.
		/// </summary>
		/// <param name="committish">The committish to checkout.</param>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		void RawCheckout(string committish, JobProgressReporter progressReporter, CancellationToken cancellationToken)
		{
			logger.LogTrace("Checkout: {0}", committish);

			var stage = $"Checkout {committish}";
			progressReporter = progressReporter.CreateSection(stage, 1.0);
			progressReporter.ReportProgress(0);
			cancellationToken.ThrowIfCancellationRequested();

			var checkoutOptions = new CheckoutOptions
			{
				CheckoutModifiers = CheckoutModifiers.Force,
				OnCheckoutProgress = CheckoutProgressHandler(progressReporter),
			};

			void RunCheckout() => commands.Checkout(
				libGitRepo,
				checkoutOptions,
				committish);

			try
			{
				RunCheckout();
			}
			catch (NotFoundException)
			{
				// Maybe (likely) a remote?
				var remoteName = $"origin/{committish}";
				var remoteBranch = libGitRepo.Branches.FirstOrDefault(
					branch => branch.FriendlyName.Equals(remoteName, StringComparison.Ordinal));
				cancellationToken.ThrowIfCancellationRequested();

				if (remoteBranch == default)
					throw;

				logger.LogDebug("Creating local branch for {0}...", remoteBranch.FriendlyName);
				var branch = libGitRepo.CreateBranch(committish, remoteBranch.Tip);

				libGitRepo.Branches.Update(branch, branchUpdate => branchUpdate.TrackedBranch = remoteBranch.CanonicalName);

				cancellationToken.ThrowIfCancellationRequested();

				RunCheckout();
			}

			cancellationToken.ThrowIfCancellationRequested();

			libGitRepo.RemoveUntrackedFiles();
		}

		/// <summary>
		/// Force push the current repository HEAD to <see cref="RemoteTemporaryBranchName"/>;.
		/// </summary>
		/// <param name="username">The username to fetch from the origin repository.</param>
		/// <param name="password">The password to fetch from the origin repository.</param>
		/// <param name="progressReporter"><see cref="JobProgressReporter"/> of the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task PushHeadToTemporaryBranch(string username, string password, JobProgressReporter progressReporter, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				logger.LogInformation("Pushing changes to temporary remote branch...");
				var branch = libGitRepo.CreateBranch(RemoteTemporaryBranchName);
				try
				{
					cancellationToken.ThrowIfCancellationRequested();
					var remote = libGitRepo.Network.Remotes.First();
					try
					{
						var forcePushString = String.Format(CultureInfo.InvariantCulture, "+{0}:{0}", branch.CanonicalName);
						libGitRepo.Network.Push(remote, forcePushString, GeneratePushOptions(progressReporter.CreateSection(null, 0.9), username, password, cancellationToken));
						var removalString = String.Format(CultureInfo.InvariantCulture, ":{0}", branch.CanonicalName);
						libGitRepo.Network.Push(remote, removalString, GeneratePushOptions(progressReporter.CreateSection(null, 0.1), username, password, cancellationToken));
					}
					catch (UserCancelledException)
					{
						cancellationToken.ThrowIfCancellationRequested();
					}
					catch (LibGit2SharpException e)
					{
						logger.LogWarning(e, "Unable to push to temporary branch!");
					}
				}
				finally
				{
					libGitRepo.Branches.Remove(branch);
				}
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <summary>
		/// Generate a standard set of <see cref="PushOptions"/>.
		/// </summary>
		/// <param name="progressReporter"><see cref="JobProgressReporter"/> of the operation.</param>
		/// <param name="username">The username for the <see cref="credentialsProvider"/>.</param>
		/// <param name="password">The password for the <see cref="credentialsProvider"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A new set of <see cref="PushOptions"/>.</returns>
		PushOptions GeneratePushOptions(JobProgressReporter progressReporter, string username, string password, CancellationToken cancellationToken)
		{
			var subProgressReporter = progressReporter.CreateSection(null, 0.5);

			return new PushOptions
			{
				OnPackBuilderProgress = (stage, current, total) =>
				{
					var baseProgress = stage == PackBuilderStage.Counting ? 0 : 0.5;
					progressReporter.ReportProgress(baseProgress + (0.5 * ((double)current / total)));
					return !cancellationToken.IsCancellationRequested;
				},
				OnNegotiationCompletedBeforePush = (a) =>
				{
					subProgressReporter = progressReporter.CreateSection(null, 0.5);
					return !cancellationToken.IsCancellationRequested;
				},
				OnPushTransferProgress = (a, sentBytes, totalBytes) =>
				{
					progressReporter.ReportProgress((double)sentBytes / totalBytes);
					return !cancellationToken.IsCancellationRequested;
				},
				CredentialsProvider = credentialsProvider.GenerateCredentialsHandler(username, password),
			};
		}

		/// <summary>
		/// Recusively update all <see cref="Submodule"/>s in the <see cref="libGitRepo"/>.
		/// </summary>
		/// <param name="progressReporter"><see cref="JobProgressReporter"/> of the operation.</param>
		/// <param name="username">The username for the <see cref="credentialsProvider"/>.</param>
		/// <param name="password">The password for the <see cref="credentialsProvider"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task UpdateSubmodules(JobProgressReporter progressReporter, string username, string password, CancellationToken cancellationToken)
		{
			var submoduleCount = libGitRepo.Submodules.Count();
			if (submoduleCount == 0)
			{
				logger.LogTrace("No submodules, skipping update");
				return;
			}

			logger.LogTrace("Updating submodules with{0} credentials...", username == null ? "out" : String.Empty);

			var factor = 1.0 / submoduleCount / 2;
			foreach (var submodule in libGitRepo.Submodules)
			{
				var submoduleUpdateOptions = new SubmoduleUpdateOptions
				{
					Init = true,
					OnTransferProgress = TransferProgressHandler(
						progressReporter.CreateSection($"Fetch submodule {submodule.Name}", factor),
						cancellationToken),
					OnProgress = output => !cancellationToken.IsCancellationRequested,
					OnUpdateTips = (a, b, c) => !cancellationToken.IsCancellationRequested,
					CredentialsProvider = credentialsProvider.GenerateCredentialsHandler(username, password),
					OnCheckoutProgress = CheckoutProgressHandler(
						progressReporter.CreateSection($"Checkout submodule {submodule.Name}", factor)),
				};

				logger.LogDebug("Updating submodule {0}...", submodule.Name);
				Task RawSubModuleUpdate() => Task.Factory.StartNew(
					() => libGitRepo.Submodules.Update(submodule.Name, submoduleUpdateOptions),
					cancellationToken,
					DefaultIOManager.BlockingTaskCreationOptions,
					TaskScheduler.Current);
				try
				{
					await RawSubModuleUpdate();
				}
				catch (LibGit2SharpException ex)
				{
					// workaround for https://github.com/libgit2/libgit2/issues/3820
					// kill off the modules/ folder in .git and try again
					progressReporter.ReportProgress(null);
					credentialsProvider.CheckBadCredentialsException(ex);
					logger.LogWarning(ex, "Initial update of submodule {0} failed. Deleting submodule directories and re-attempting...", submodule.Name);

					await Task.WhenAll(
						ioMananger.DeleteDirectory($".git/modules/{submodule.Path}", cancellationToken),
						ioMananger.DeleteDirectory(submodule.Path, cancellationToken))
						;

					logger.LogTrace("Second update attempt for submodule {0}...", submodule.Name);
					try
					{
						await RawSubModuleUpdate();
					}
					catch (UserCancelledException)
					{
						cancellationToken.ThrowIfCancellationRequested();
					}
					catch (LibGit2SharpException ex2)
					{
						credentialsProvider.CheckBadCredentialsException(ex2);
						logger.LogTrace(ex2, "Retried update of submodule {0} failed!", submodule.Name);
						throw new AggregateException(ex, ex2);
					}
				}

				await eventConsumer.HandleEvent(EventType.RepoSubmoduleUpdate, new List<string> { submodule.Name }, cancellationToken);
			}
		}

		/// <summary>
		/// Converts a given <paramref name="progressReporter"/> to a <see cref="LibGit2Sharp.Handlers.CheckoutProgressHandler"/>.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> of the operation.</param>
		/// <returns>A <see cref="LibGit2Sharp.Handlers.CheckoutProgressHandler"/> based on <paramref name="progressReporter"/>.</returns>
		CheckoutProgressHandler CheckoutProgressHandler(JobProgressReporter progressReporter) => (a, completedSteps, totalSteps) =>
		{
			double? percentage;

			// short circuit initialization where totalSteps is 0
			if (completedSteps == 0)
				percentage = 0;
			else if (totalSteps < completedSteps || totalSteps == 0)
				percentage = null;
			else
			{
				percentage = ((double)completedSteps) / totalSteps;
				if (percentage < 0)
					percentage = null;
			}

			if (percentage == null)
				logger.LogDebug(
					"Bad checkout progress values (Please tell Dominion)! Completeds: {completed}, Total: {total}",
					completedSteps,
					totalSteps);

			progressReporter.ReportProgress(percentage);
		};

		/// <summary>
		/// Generate a <see cref="LibGit2Sharp.Handlers.TransferProgressHandler"/> from a given <paramref name="progressReporter"/> and <paramref name="cancellationToken"/>.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> of the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A new <see cref="LibGit2Sharp.Handlers.TransferProgressHandler"/> based on <paramref name="progressReporter"/>.</returns>
		TransferProgressHandler TransferProgressHandler(JobProgressReporter progressReporter, CancellationToken cancellationToken) => (transferProgress) =>
		{
			double? percentage;
			var totalObjectsToProcess = transferProgress.TotalObjects * 2;
			var processedObjects = transferProgress.IndexedObjects + transferProgress.ReceivedObjects;
			if (totalObjectsToProcess < processedObjects || totalObjectsToProcess == 0)
				percentage = null;
			else
			{
				percentage = (double)processedObjects / totalObjectsToProcess;
				if (percentage < 0)
					percentage = null;
			}

			if (percentage == null)
				logger.LogDebug(
					"Bad transfer progress values (Please tell Cyberboss)! Indexed: {indexed}, Received: {received}, Total: {total}",
					transferProgress.IndexedObjects,
					transferProgress.ReceivedObjects,
					transferProgress.TotalObjects);

			progressReporter.ReportProgress(percentage);
			return !cancellationToken.IsCancellationRequested;
		};
	}
}
