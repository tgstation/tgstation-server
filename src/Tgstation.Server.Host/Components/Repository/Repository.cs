using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <inheritdoc />
	sealed class Repository : IRepository
	{
		/// <summary>
		/// Indication of a GitHub repository
		/// </summary>
		public const string GitHubUrl = "://github.com/";

		const string UnknownReference = "<UNKNOWN>";

		/// <summary>
		/// The branch name used for publishing testmerge commits
		/// </summary>
		public const string RemoteTemporaryBranchName = "___TGSTempBranch";

		/// <inheritdoc />
		public bool IsGitHubRepository { get; }

		/// <inheritdoc />
		public string GitHubOwner { get; }

		/// <inheritdoc />
		public string GitHubRepoName { get; }

		/// <inheritdoc />
		public bool Tracking => Reference != null && repository.Head.IsTracking;

		/// <inheritdoc />
		public string Head => repository.Head.Tip.Sha;

		/// <inheritdoc />
		public string Reference => repository.Head.FriendlyName;

		/// <inheritdoc />
		public string Origin => repository.Network.Remotes.First().Url;

		/// <summary>
		/// The <see cref="LibGit2Sharp.IRepository"/> for the <see cref="Repository"/>
		/// </summary>
		readonly LibGit2Sharp.IRepository repository;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="Repository"/>
		/// </summary>
		readonly IIOManager ioMananger;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="Repository"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// <see cref="Action"/> to be taken when <see cref="Dispose"/> is called
		/// </summary>
		readonly Action onDispose;

		static void GetRepositoryOwnerName(string remote, out string owner, out string name)
		{
			//Assume standard gh format: [(git)|(https)]://github.com/owner/repo(.git)[0-1]
			//Yes use .git twice in case it was weird
			var toRemove = new string[] { ".git", "/", ".git" };
			foreach (string item in toRemove)
				if (remote.EndsWith(item, StringComparison.OrdinalIgnoreCase))
					remote = remote.Substring(0, remote.LastIndexOf(item, StringComparison.OrdinalIgnoreCase));
			var splits = remote.Split('/');
			name = splits[splits.Length - 1];
			owner = splits[splits.Length - 2].Split('.')[0];
		}

		/// <summary>
		/// Construct a <see cref="Repository"/>
		/// </summary>
		/// <param name="repository">The value of <see cref="repository"/></param>
		/// <param name="ioMananger">The value of <see cref="ioMananger"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="onDispose">The value if <see cref="onDispose"/></param>
		public Repository(LibGit2Sharp.IRepository repository, IIOManager ioMananger, IEventConsumer eventConsumer, Action onDispose)
		{
			this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
			this.ioMananger = ioMananger ?? throw new ArgumentNullException(nameof(ioMananger));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
			IsGitHubRepository = Origin.ToUpperInvariant().Contains(GitHubUrl.ToUpperInvariant());
			if (IsGitHubRepository)
			{
				GetRepositoryOwnerName(Origin, out var owner, out var name);
				GitHubOwner = owner;
				GitHubRepoName = name;
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			repository.Dispose();
			onDispose.Invoke();
		}

		/// <summary>
		/// Runs a blocking force checkout to <paramref name="committish"/>
		/// </summary>
		/// <param name="committish">The committish to checkout</param>
		void RawCheckout(string committish)
		{
			Commands.Checkout(repository, committish, new CheckoutOptions
			{
				CheckoutModifiers = CheckoutModifiers.Force
			});
			repository.RemoveUntrackedFiles();
		}

		/// <inheritdoc />
		public async Task<bool?> AddTestMerge(TestMergeParameters testMergeParameters, string committerName, string committerEmail, string username, string password, Action<int> progressReporter, CancellationToken cancellationToken)
		{
			if (testMergeParameters == null)
				throw new ArgumentNullException(nameof(testMergeParameters));

			if (committerName == null)
				throw new ArgumentNullException(nameof(committerName));
			if (committerEmail == null)
				throw new ArgumentNullException(nameof(committerEmail));

			if (!IsGitHubRepository)
				throw new InvalidOperationException("Test merging is only available on GitHub hosted origin repositories!");

			var commitMessage = String.Format(CultureInfo.InvariantCulture, "Test merge of pull request #{0}{1}{2}", testMergeParameters.Number.Value, testMergeParameters.Comment != null ? Environment.NewLine : String.Empty, testMergeParameters.Comment ?? String.Empty);


			var prBranchName = String.Format(CultureInfo.InvariantCulture, "pr-{0}", testMergeParameters.Number);
			var localBranchName = String.Format(CultureInfo.InvariantCulture, "pull/{0}/headrefs/heads/{1}", testMergeParameters.Number, prBranchName);

			var Refspec = new List<string> { String.Format(CultureInfo.InvariantCulture, "pull/{0}/head:{1}", testMergeParameters.Number, prBranchName) };
			var logMessage = String.Format(CultureInfo.InvariantCulture, "Merge remote pull request #{0}", testMergeParameters.Number);

			var originalCommit = repository.Head;

			MergeResult result = null;

			var sig = new Signature(new Identity(committerName, committerEmail), DateTimeOffset.Now);
			await Task.Factory.StartNew(() =>
			{
				try
				{
					try
					{
						var remote = repository.Network.Remotes.First();
						Commands.Fetch((LibGit2Sharp.Repository)repository, remote.Name, Refspec, new FetchOptions
						{
							Prune = true,
							OnProgress = (a) => !cancellationToken.IsCancellationRequested,
							OnTransferProgress = (a) =>
							{
								var percentage = 100 * (((float)a.IndexedObjects + a.ReceivedObjects) / (a.TotalObjects * 2));
								progressReporter?.Invoke((int)percentage);
								return !cancellationToken.IsCancellationRequested;
							},
							OnUpdateTips = (a, b, c) => !cancellationToken.IsCancellationRequested,
							CredentialsProvider = (a, b, c) => username != null ? (Credentials)new UsernamePasswordCredentials
							{
								Username = username,
								Password = password
							} : new DefaultCredentials()
						}, logMessage);
					}
					catch (UserCancelledException) { }

					cancellationToken.ThrowIfCancellationRequested();

					testMergeParameters.PullRequestRevision = repository.Lookup(testMergeParameters.PullRequestRevision ?? localBranchName).Sha;

					cancellationToken.ThrowIfCancellationRequested();

					result = repository.Merge(testMergeParameters.PullRequestRevision, sig, new MergeOptions
					{
						CommitOnSuccess = commitMessage == null,
						FailOnConflict = true,
						FastForwardStrategy = FastForwardStrategy.NoFastForward,
						SkipReuc = true
					});
				}
				finally
				{
					repository.Branches.Remove(localBranchName);
				}

				cancellationToken.ThrowIfCancellationRequested();

				if (result.Status == MergeStatus.Conflicts)
				{
					RawCheckout(originalCommit.CanonicalName ?? originalCommit.Tip.Sha);
					cancellationToken.ThrowIfCancellationRequested();
				}

				repository.RemoveUntrackedFiles();
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

			if (result.Status == MergeStatus.Conflicts)
			{
				await eventConsumer.HandleEvent(EventType.RepoMergeConflict, new List<string> { originalCommit.Tip.Sha, testMergeParameters.PullRequestRevision, originalCommit.FriendlyName ?? UnknownReference, prBranchName }, cancellationToken).ConfigureAwait(false);
				return false;
			}

			if (commitMessage != null)
				repository.Commit(commitMessage, sig, sig, new CommitOptions
				{
					PrettifyMessage = true
				});

			return true;
		}

		/// <inheritdoc />
		public async Task CheckoutObject(string committish, CancellationToken cancellationToken)
		{
			if (committish == null)
				throw new ArgumentNullException(nameof(committish));
			await eventConsumer.HandleEvent(EventType.RepoCheckout, new List<string> { committish }, cancellationToken).ConfigureAwait(false);
			await Task.Factory.StartNew(() => RawCheckout(committish), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task FetchOrigin(string username, string password, Action<int> progressReporter, CancellationToken cancellationToken) => Task.WhenAll(
			eventConsumer.HandleEvent(EventType.RepoFetch, Array.Empty<string>(), cancellationToken),
			Task.Factory.StartNew(() =>
			{
				var remote = repository.Network.Remotes.First();
				try
				{
					Commands.Fetch((LibGit2Sharp.Repository)repository, remote.Name, remote.FetchRefSpecs.Select(x => x.Specification), new FetchOptions
					{
						Prune = true,
						OnProgress = (a) => !cancellationToken.IsCancellationRequested,
						OnTransferProgress = (a) =>
						{
							var percentage = 100 * (((float)a.IndexedObjects + a.ReceivedObjects) / (a.TotalObjects * 2));
							progressReporter?.Invoke((int)percentage);
							return !cancellationToken.IsCancellationRequested;
						},
						OnUpdateTips = (a, b, c) => !cancellationToken.IsCancellationRequested,
						CredentialsProvider = (a, b, c) => username != null ? (Credentials)new UsernamePasswordCredentials
						{
							Username = username,
							Password = password
						} : new DefaultCredentials()
					}, "Fetch origin commits");
				}
				catch (UserCancelledException)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current));

		/// <summary>
		/// Force push the current repository HEAD to <see cref="Repository.RemoteTemporaryBranchName"/>;
		/// </summary>
		/// <param name="username">The username to fetch from the origin repository</param>
		/// <param name="password">The password to fetch from the origin repository</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task PushHeadToTemporaryBranch(string username, string password, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			var branch = repository.CreateBranch(RemoteTemporaryBranchName);
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				var remote = repository.Network.Remotes.First();
				try
				{
					repository.Network.Push(remote, String.Format(CultureInfo.InvariantCulture, "+{0}:{0}", branch.CanonicalName), new PushOptions
					{
						OnPackBuilderProgress = (a, b, c) => !cancellationToken.IsCancellationRequested,
						OnNegotiationCompletedBeforePush = (a) => !cancellationToken.IsCancellationRequested,
						OnPushTransferProgress = (a, b, c) => !cancellationToken.IsCancellationRequested,
						CredentialsProvider = (a, b, c) => username != null ? (Credentials)new UsernamePasswordCredentials
						{
							Username = username,
							Password = password
						} : new DefaultCredentials()
					});
				}
				catch (UserCancelledException)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
			}
			finally
			{
				repository.Branches.Remove(branch);
			}
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

		/// <inheritdoc />
		public async Task ResetToOrigin(CancellationToken cancellationToken)
		{
			if (!repository.Head.IsTracking)
				throw new InvalidOperationException("Cannot reset to origin while not on a tracked reference!");
			var trackedBranch = repository.Head.TrackedBranch;
			await eventConsumer.HandleEvent(EventType.RepoResetOrigin, new List<string> { trackedBranch.FriendlyName, trackedBranch.Tip.Sha }, cancellationToken).ConfigureAwait(false);
			await ResetToSha(trackedBranch.Tip.Sha, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task ResetToSha(string sha, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			repository.Reset(ResetMode.Hard, sha);
			cancellationToken.ThrowIfCancellationRequested();
			repository.RemoveUntrackedFiles();
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

		/// <inheritdoc />
		public async Task CopyTo(string path, CancellationToken cancellationToken)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			await ioMananger.CopyDirectory(".", path, new List<string> { ".git" }, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<bool?> MergeOrigin(string committerName, string committerEmail, CancellationToken cancellationToken)
		{
			MergeResult result = null;
			Branch trackedBranch = null;

			var oldHead = repository.Head;

			await Task.Factory.StartNew(() =>
			{
				if (!repository.Head.IsTracking)
					throw new InvalidOperationException("Cannot reset to origin while not on a tracked reference!");
				trackedBranch = repository.Head.TrackedBranch;

				result = repository.Merge(trackedBranch, new Signature(new Identity(committerName, committerEmail), DateTimeOffset.Now), new MergeOptions
				{
					CommitOnSuccess = true,
					FailOnConflict = true,
					FastForwardStrategy = FastForwardStrategy.Default,
					SkipReuc = true,
				});

				cancellationToken.ThrowIfCancellationRequested();

				if (result.Status == MergeStatus.Conflicts)
				{
					RawCheckout(oldHead.CanonicalName);
					cancellationToken.ThrowIfCancellationRequested();
				}

				repository.RemoveUntrackedFiles();
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

			if (result.Status == MergeStatus.Conflicts)
			{
				await eventConsumer.HandleEvent(EventType.RepoMergeConflict, new List<string> { oldHead.Tip.Sha, trackedBranch.Tip.Sha, oldHead.FriendlyName ?? UnknownReference, trackedBranch.FriendlyName }, cancellationToken).ConfigureAwait(false);
				return null;
			}

			return result.Status != MergeStatus.NonFastForward;
		}

		/// <inheritdoc />
		public async Task Sychronize(string username, string password, bool synchronizeTrackedBranch, CancellationToken cancellationToken)
		{
			if (username == null && password == null)
				return;

			if (username == null)
				throw new ArgumentNullException(nameof(username));
			if (password == null)
				throw new ArgumentNullException(nameof(password));

			var startHead = Head;

			if (!await eventConsumer.HandleEvent(EventType.RepoPreSynchronize, new List<string> { ioMananger.ResolvePath(".") }, cancellationToken).ConfigureAwait(false))
				return;

			if (!synchronizeTrackedBranch)
			{
				await PushHeadToTemporaryBranch(username, password, cancellationToken).ConfigureAwait(false);
				return;
			}

			if (Head == startHead || !repository.Head.IsTracking)
				return;

			await Task.Factory.StartNew(() =>
			{
				cancellationToken.ThrowIfCancellationRequested();
				var remote = repository.Network.Remotes.First();
				try
				{
					repository.Network.Push(repository.Head, new PushOptions
					{
						OnPackBuilderProgress = (a, b, c) => !cancellationToken.IsCancellationRequested,
						OnNegotiationCompletedBeforePush = (a) => !cancellationToken.IsCancellationRequested,
						OnPushTransferProgress = (a, b, c) => !cancellationToken.IsCancellationRequested,
						CredentialsProvider = (a, b, c) => username != null ? (Credentials)new UsernamePasswordCredentials
						{
							Username = username,
							Password = password
						} : new DefaultCredentials()
					});
				}
				catch (UserCancelledException)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
		}
	}
}
