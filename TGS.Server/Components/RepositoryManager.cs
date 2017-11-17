using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.IO;
using System.ServiceModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	sealed class RepositoryManager : IRepositoryManager, IDisposable
	{
		/// <summary>
		/// The name of the primary remote used in all operations
		/// </summary>
		public const string DefaultRemote = "origin";
		/// <summary>
		/// Path to the repository directory in the <see cref="Instance"/> directory
		/// </summary>
		const string RepoPath = "Repository";
		/// <summary>
		/// Message returned when an operation is aborted due to <see cref="longOperationInProgress"/> being set
		/// </summary>
		const string RepoBusyMessage = "Repository is busy with another operation!";
		/// <summary>
		/// The directory for SSH keys
		/// </summary>
		const string RepoKeyDir = "RepoKey";
		/// <summary>
		/// Error message for when a merge operation fails due to the target branch already having the source branch's commits
		/// </summary>
		const string RepoErrorUpToDate = "Already up to date!";
		/// <summary>
		/// The branch name used for publishing testmerge commits
		/// </summary>
		const string RemoteTempBranchName = "___TGS3TempBranch";
		/// <summary>
		/// Git remote for push operations
		/// </summary>
		const string SSHPushRemote = "ssh_push_target";
		/// <summary>
		/// File name for monitoring which github pull requests are currently test merged
		/// </summary>
		const string PRJobFile = "prtestjob.json";
		/// <summary>
		/// The branch that points to the current commit that is live or staged to be live in DreamDaemon
		/// </summary>
		const string LiveTrackingBranch = "___TGSLiveCommitTrackingBranch";
		/// <summary>
		/// Commit message for <see cref="RepoConfig.PathsToStage"/>
		/// </summary>
		const string CommitMessage = "Automatic changelog compile, [ci skip]";

		/// <summary>
		/// The path to the private ssh-rsa key file
		/// </summary>
		static readonly string PrivateKeyPath = Path.Combine(RepoKeyDir, "private_key.txt");
		/// <summary>
		/// The path to the public ssh-rsa key file
		/// </summary>
		static readonly string PublicKeyPath = Path.Combine(RepoKeyDir, "public_key.txt");

		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IChatManager Chat;
		/// <summary>
		/// The <see cref="IInstanceConfig"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IInstanceConfig Config;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IIOManager IO;
		/// <summary>
		/// The <see cref="IRepositoryProvider"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IRepositoryProvider RepositoryProvider;

		/// <summary>
		/// The <see cref="Repository"/> the <see cref="RepositoryManager"/> manages
		/// </summary>
		IRepository repository;
		/// <summary>
		/// <see cref="Octokit.GitHubClient"/> used for retrieving pull request information from GitHub
		/// </summary>
		Octokit.GitHubClient ghClient;
		/// <summary>
		/// The currently running long operation
		/// </summary>
		Task currentOperation;
		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="currentOperation"/>
		/// </summary>
		CancellationTokenSource currentOperationCanceller;


		/// <summary>
		/// The progress status of a long running operation on a scale of 0 - 100. -1 indicates indefinite
		/// </summary>
		int currentProgress = -1;
		/// <summary>
		/// Whether or not a long running operation is in progress. <see langword="this"/> must be <see langword="lock"/>ed in order to check or set
		/// </summary>
		bool longOperationInProgress;

		/// <summary>
		/// Construct a <see cref="RepositoryManager"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		/// <param name="config">The value of <see cref="Config"/></param>
		/// <param name="io">The value of <see cref="IO"/></param>
		/// <param name="repositoryProvider">The value of <see cref="RepositoryProvider"/></param>
		public RepositoryManager(IInstanceLogger logger, IChatManager chat, IInstanceConfig config, IIOManager io, IRepositoryProvider repositoryProvider)
		{
			Logger = logger;
			Chat = chat;
			Config = config;
			IO = io;
			RepositoryProvider = repositoryProvider;

			ghClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("TGS.Server"));

			LoadRepository();
		}

		/// <summary>
		/// Calls <see cref="StopCurrentOperation"/> and <see cref="DisposeRepo"/>
		/// </summary>
		public void Dispose()
		{
			StopCurrentOperation();
			DisposeRepo();
		}

		/// <summary>
		/// Pushes the current <see cref="repository"/> HEAD to a temporary remote branch and then deletes it. Must be called from a locked context
		/// </summary>
		void PushTestmergeCommit()
		{
			if (Config.PushTestmergeCommits && SSHAuthAvailable())
			{
				string NewB = null;
				var targetRemote = repository.Network.Remotes[SSHPushRemote];
				var options = new PushOptions()
				{
					CredentialsProvider = GenerateGitCredentials
				};
				try
				{
					//now try and push the commit to the remote so they can be referenced
					NewB = repository.CreateBranch(RemoteTempBranchName).CanonicalName;
					repository.Network.Push(targetRemote, NewB, options); //push the branch
					repository.Branches.Remove(NewB);
					var removalString = String.Format(":{0}", NewB);
					NewB = null;
					//we need to delay the second operation a LOT otherwise we get ssh errors
					Thread.Sleep(10000);
					repository.Network.Push(targetRemote, removalString, options);   //delete the branch
					Logger.WriteInfo("Pushed reference commit: " + repository.Head.Tip.Sha, EventID.ReferencePush);
				}
				catch (Exception e)
				{
					Logger.WriteWarning(String.Format("Failed to push reference commit: {0}. Error: {1}", repository.Head.Tip.Sha, e.ToString()), EventID.ReferencePush);
				}
				finally
				{
					if (NewB != null)
					{
						//Try to delete the branches regardless
						try
						{
							repository.Branches.Remove(NewB);
						}
						catch { }
						try
						{
							repository.Network.Push(targetRemote, String.Format(":{0}", NewB), options);
						}
						catch { }
					}
				}
			}
		}

		/// <summary>
		/// Calls <see cref="IDisposable.Dispose"/> on <see cref="repository"/>
		/// </summary>
		void DisposeRepo()
		{
			if (repository != null)
			{
				repository.Dispose();
				repository = null;
			}
		}

		/// <summary>
		/// Creates a commit <see cref="LibGit2Sharp.Signature"/> based off of the configured name and e-mail
		/// </summary>
		/// <returns>The created <see cref="LibGit2Sharp.Signature"/></returns>
		Signature MakeSig()
		{
			return new Signature(new Identity(Config.CommitterName, Config.CommitterEmail), DateTimeOffset.Now);
		}

		/// <summary>
		/// Merges given <paramref name="committish"/> into the current branch. Must be called from a locked context
		/// </summary>
		/// <param name="committish">The sha/branch/tag to merge</param>
		/// <param name="mergeMessage">The commit message for the merge commit</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string MergeBranch(string committish, string mergeMessage)
		{
			var mo = new MergeOptions()
			{
				OnCheckoutProgress = HandleCheckoutProgress
			};
			if (mergeMessage != null)
			{
				mo.CommitOnSuccess = false;
				mo.FastForwardStrategy = FastForwardStrategy.NoFastForward;
			}
			var sig = MakeSig();
			var Result = repository.Merge(committish, sig, mo);
			currentProgress = -1;
			switch (Result.Status)
			{
				case MergeStatus.Conflicts:
					ResetNoLock(null);
					Chat.SendMessage("REPO: Merge conflicted, aborted.", MessageType.DeveloperInfo);
					return "Merge conflict occurred.";
				case MergeStatus.UpToDate:
					return RepoErrorUpToDate;
			}
			if (mergeMessage != null)
				repository.Commit(mergeMessage, sig, sig);
			return null;
		}

		/// <summary>
		/// Properly updates any git submodules in the <see cref="repository"/>. Must be called from a locked context
		/// </summary>
		void UpdateSubmodules()
		{
			var suo = new SubmoduleUpdateOptions
			{
				Init = true
			};
			foreach (var I in repository.Submodules)
				try
				{
					repository.Submodules.Update(I.Name, suo);
				}
				catch (Exception e)
				{
					try
					{
						//workaround for https://github.com/libgit2/libgit2/issues/3820
						//kill off the modules/ folder in .git and try again
						try
						{
							Helpers.DeleteDirectory(String.Format("{0}/.git/modules/{1}", RepoPath, I.Path));
						}
						catch
						{
							throw e;
						}
						repository.Submodules.Update(I.Name, suo);
						var msg = String.Format("I had to reclone submodule {0}. If this is happening a lot find a better hack or fix https://github.com/libgit2/libgit2/issues/3820!", I.Name);
						Chat.SendMessage(String.Format("REPO: {0}", msg), MessageType.DeveloperInfo);
						Logger.WriteWarning(msg, EventID.Submodule);
					}
					catch (Exception ex)
					{
						Chat.SendMessage(String.Format("REPO: Failed to update submodule {0}!", I.Name), MessageType.DeveloperInfo);
						Logger.WriteError(String.Format("Failed to update submodule {0}! Error: {1}", I.Name, ex.ToString()), EventID.Submodule);
					}
				}
		}

		/// <summary>
		/// Stops any operation running in <see cref="currentOperation"/>
		/// </summary>
		void StopCurrentOperation()
		{
			Task co;
			CancellationTokenSource cts;
			lock (this)
			{
				co = currentOperation;
				currentOperation = null;
				cts = currentOperationCanceller;
				currentOperationCanceller = null;
			}
			if (co == null)
				return;
			cts.Cancel();
			co.Wait();
			cts.Dispose();
			co.Dispose();
		}

		/// <summary>
		/// Tries to initialize <see cref="repository"/> if it hasn't been already
		/// </summary>
		/// <param name="bypassSanityCheck">Skip checking <see cref="longOperationInProgress"/></param>
		/// <returns><see langword="true"/> if <see cref="repository"/> was initialized, <see langword="false"/> otherwise</returns>
		bool LoadRepository(bool bypassSanityCheck = false)
		{
			lock (this)
			{
				if (!bypassSanityCheck || longOperationInProgress)    //sanity check
					throw new InvalidOperationException("Cannot LoadRepository while longOperationInProgress is set");
				if (repository != null)
					return true;
				if (IO.DirectoryExists(RepoPath))
				{
					var path = IO.ResolvePath(RepoPath);
					var res = RepositoryProvider.IsValid(path);
					if (res)
					{
						repository = RepositoryProvider.LoadRepository(path);
						repository.Config.Set("gc.auto", false);
					}
					return res;
				}
				IO.CreateDirectory(RepoPath);   //nothing here yet
				return false;
			}
		}

		/// <summary>
		/// SSH credentials callback. Properly sets up SSH keys for an authorization operation
		/// </summary>
		/// <param name="url">Ignored</param>
		/// <param name="usernameFromUrl">The username to use for the operation</param>
		/// <param name="types">The <see cref="SupportedCredentialTypes"/> for the operation</param>
		/// <returns>The proper <see cref="Credentials"/> for the operation</returns>
		Credentials GenerateGitCredentials(string url, string usernameFromUrl, SupportedCredentialTypes types)
		{
			var user = usernameFromUrl ?? "git";
			if (types == SupportedCredentialTypes.UsernameQuery)
				return new UsernameQueryCredentials()
				{
					Username = user,
				};
			return new SshUserKeyCredentials()
			{
				Username = user,
				PrivateKey = IO.ResolvePath(PrivateKeyPath),
				PublicKey = IO.ResolvePath(PublicKeyPath),
				Passphrase = "",
			};
		}

		/// <summary>
		/// Deletes the <see cref="PRJobFile"/>
		/// </summary>
		void DeletePRList()
		{
			lock (this)
				if (IO.FileExists(PRJobFile))
					try
					{
						IO.DeleteFile(PRJobFile);
					}
					catch (Exception e)
					{
						Logger.WriteError("Failed to delete PR list: " + e.ToString(), EventID.RepoPRListError);
					}
		}

		/// <summary>
		/// Check if <see cref="RepoKeyDir"/> is populated with correct SSH key files
		/// </summary>
		/// <returns><see langword="true"/> if <see cref="RepoKeyDir"/> is populated with correct SSH key files, <see langword="false"/> otherwise</returns>
		bool SSHAuthAvailable()
		{
			return IO.FileExists(PrivateKeyPath) && IO.FileExists(PublicKeyPath);
		}

		/// <summary>
		/// Deserializes the <see cref="PRJobFile"/> into a <see cref="List{T}"/> of <see cref="PullRequestInfo"/>
		/// </summary>
		/// <returns>A <see cref="List{T}"/> of <see cref="PullRequestInfo"/></returns>
		List<PullRequestInfo> GetTestMergeList()
		{
			lock (this)
				try
				{
					return JsonConvert.DeserializeObject<List<PullRequestInfo>>(IO.ReadAllText(PRJobFile));
				}
				catch
				{
					return new List<PullRequestInfo>();
				}
		}

		/// <summary>
		/// Serializes pull request info into <see cref="PRJobFile"/>
		/// </summary>
		/// <param name="infos">The <see cref="List{T}"/> of <see cref="PullRequestInfo"/> to serialize</param>
		void SetTestMergeList(List<PullRequestInfo> infos)
		{
			lock (this)
				IO.WriteAllText(PRJobFile, JsonConvert.SerializeObject(infos));
		}

		/// <summary>
		/// Updates <see cref="currentProgress"/> with the progess of the current <see cref="repository"/> transfer operation
		/// </summary>
		/// <param name="progress">The <see cref="TransferProgress"/> of the current <see cref="repository"/> transfer operation</param>
		/// <returns><see langword="true"/></returns>
		bool HandleTransferProgress(TransferProgress progress)
		{
			currentProgress = ((int)(((float)progress.ReceivedObjects / progress.TotalObjects) * 100) / 2) + ((int)(((float)progress.IndexedObjects / progress.TotalObjects) * 100) / 2);
			return true;
		}

		/// <summary>
		/// Updates <see cref="currentProgress"/> with the progess of the current <see cref="repository"/> checkout operation
		/// </summary>
		/// <param name="path">Ignored</param>
		/// <param name="completedSteps">Dividend for progress calculation</param>
		/// <param name="totalSteps">Divisor for progress calculation</param>
		void HandleCheckoutProgress(string path, int completedSteps, int totalSteps)
		{
			currentProgress = (int)(((float)completedSteps / totalSteps) * 100);
		}

		/// <inheritdoc />
		public string Checkout(string objectName)
		{
			if (objectName == RemoteTempBranchName || objectName == LiveTrackingBranch)
				return "I'm sorry Dave, I'm afraid I can't do that...";
			lock (this)
			{
				if (longOperationInProgress)
					return RepoBusyMessage;
				Chat.SendMessage(String.Format("REPO: Checking out object: {0}", objectName), MessageType.DeveloperInfo);
				try
				{
					if (repository.Branches[objectName] == null)
					{
						//see if origin has the branch
						var result = RepositoryProvider.Fetch(repository);
						var trackedBranch = repository.Branches[String.Format("{0}/{1}", DefaultRemote, objectName)];
						if (trackedBranch != null)
						{
							var newBranch = repository.CreateBranch(objectName, trackedBranch.Tip);
							//track it
							repository.Branches.Update(newBranch, b => b.TrackedBranch = trackedBranch.CanonicalName);
						}
						else if (result != null)
							return result;
					}

					//if this is a tracked branch, we need to reset to remote first and delete the PR list
					ResetNoLock(repository.Head.TrackedBranch ?? repository.Head);
					DeletePRList();

					RepositoryProvider.Checkout(repository, objectName);

					var res = ResetNoLock(null);

					UpdateSubmodules();

					Chat.SendMessage("REPO: Checkout complete!", MessageType.DeveloperInfo);
					Logger.WriteInfo(String.Format("Repo checked out {0}", objectName), EventID.RepoCheckout);
					return res;
				}
				catch (Exception e)
				{
					SendMessage("REPO: Checkout failed!", MessageType.DeveloperInfo);
					WriteWarning(String.Format("Repo checkout of {0} failed: {1}", sha, e.ToString()), EventID.RepoCheckoutFail);
					return e.ToString();
				}
			}
		}

		/// <inheritdoc />
		public int CheckoutProgress()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string Clone(string remote, string branch = "master")
		{
			lock (this)
			{
				if (longOperationInProgress)
					return RepoBusyMessage;

				if (remote.Contains("ssh://") && !SSHAuthAvailable())
					return String.Format("SSH url specified but either {0} or {1} does not exist in the server directory!", PrivateKeyPath, PublicKeyPath);

				longOperationInProgress = true;

				currentOperationCanceller = new CancellationTokenSource();
				var cancellationToken = currentOperationCanceller.Token;
				currentOperation = Task.Factory.StartNew(() =>
				{
					try
					{
						cancellationToken.ThrowIfCancellationRequested();
						Chat.SendMessage(String.Format("REPO: Cloning {0} branch of {1} ...", branch, remote), MessageType.DeveloperInfo);
						DisposeRepo();
						IO.DeleteDirectory(RepoPath);
						DeletePRList();

						Repository.Clone(remote, IO.ResolvePath(RepoPath), new CloneOptions()
						{
							BranchName = branch,
							RecurseSubmodules = true,
							OnTransferProgress = HandleTransferProgress,
							OnCheckoutProgress = HandleCheckoutProgress,
							CredentialsProvider = GenerateGitCredentials,
						});

						currentProgress = -1;

						LoadRepository(true);

						//create an ssh remote for pushing
						repository.Network.Remotes.Add(SSHPushRemote, remote.Replace("git://", "ssh://").Replace("https://", "ssh://").Replace("http://", "ssh://"));

						Chat.SendMessage("REPO: Clone complete!", MessageType.DeveloperInfo);
						Logger.WriteInfo("Repository {0}:{1} successfully cloned", EventID.RepoClone);
					}
					catch (Exception e)
					{
						Chat.SendMessage(String.Format("REPO: Clone failed! {0}", e.Message), MessageType.DeveloperInfo);
						Logger.WriteWarning(String.Format("Failed to clone {2}:{0}: {1}", branch, e.ToString(), remote), EventID.RepoCloneFail);
						//try to cleanup
						try
						{
							DisposeRepo();
							IO.DeleteDirectory(RepoPath);
						}
						catch { }
					}
					finally
					{
						lock (this)
						{
							currentProgress = -1;
							currentOperation = null;
							currentOperationCanceller = null;
						}
					}
				});
				return null;
			}
		}

		/// <inheritdoc />
		public string CopyTo(string destination, IEnumerable<string> ignorePaths)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string CopyToRestricted(string destination, IEnumerable<string> onlyPaths)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool Exists()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string GenerateChangelog(out string error)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string GetBranch(out string error)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string GetCommitterEmail()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string GetCommitterName()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string GetHead(bool useTracked, out string error)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string GetRemote(out string error)
		{
			lock (this)
				try
				{
					error = null;
					return repository.Network.Remotes[DefaultRemote].Url;
				}
				catch (Exception e)
				{
					error = e.ToString();
					return null;
				}
		}

		/// <inheritdoc />
		public IDictionary<string, string> ListBackups(out string error)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public List<PullRequestInfo> MergedPullRequests(out string error)
		{
			lock (this)
			{
				if (longOperationInProgress)
				{
					error = RepoBusyMessage;
					return null;
				}

				try
				{
					error = null;
					return GetTestMergeList();
				}
				catch (Exception e)
				{
					error = e.ToString();
					return null;
				}
			}
		}

		/// <inheritdoc />
		public string MergePullRequest(int PRnumber, string atSHA = null)
		{
			lock (this)
			{
				Chat.SendMessage(String.Format("REPO: Merging PR #{0}{1}...", PRnumber, atSHA != null ? String.Format(" at commit {0}", atSHA) : ""), MessageType.DeveloperInfo);
				result = ResetNoLock(null);
				if (result != null)
					return result;

				Task<Octokit.PullRequest> task = null;
				try
				{
					//only supported with github
					var remoteUrl = repository.Network.Remotes[DefaultRemote].Url;
					if (!remoteUrl.ToLower().Contains("github.com"))
						return "Only supported with Github based repositories.";


					//first we need to get the info before allowing the merge
					Interface.Helpers.GetRepositoryRemote(remoteUrl, out string owner, out string name);
					task = ghClient.PullRequest.Get(owner, name, PRnumber);

					var Refspec = new List<string>();
					var PRBranchName = String.Format("pr-{0}", PRnumber);
					var LocalBranchName = String.Format("pull/{0}/headrefs/heads/{1}", PRnumber, PRBranchName);
					Refspec.Add(String.Format("pull/{0}/head:{1}", PRnumber, PRBranchName));

					var branch = repository.Branches[LocalBranchName];
					if (branch != null)
						//Need to delete the branch first in case of rebase
						repository.Branches.Remove(branch);

					RepositoryProvider.Fetch(repository);

					currentProgress = -1;

					branch = repository.Branches[LocalBranchName];
					if (branch == null)
					{
						Chat.SendMessage("REPO: PR could not be fetched. Does it exist?", MessageType.DeveloperInfo);
						return String.Format("PR #{0} could not be fetched. Does it exist?", PRnumber);
					}

					if (atSHA != null)
					{
						//find the commit
						Commit commit = null;
						string error = null;
						try
						{
							commit = repository.Lookup<Commit>(atSHA);
						}
						catch (Exception e)
						{
							error = e.ToString();
						}
						if (commit == null)
						{
							Chat.SendMessage("REPO: Commit could not be found, aborting!", MessageType.DeveloperInfo);
							return error ?? String.Format("Commit {0} could not be found in the repository!", atSHA);
						}
					}

					task.Wait();
					var prInfo = new PullRequestInfo(PRnumber, task.Result.User.Name, task.Result.Title, task.Result.Head.Sha);

					//so we'll know if this fails
					var Result = MergeBranch(atSHA ?? LocalBranchName, String.Format("Test merge commit for pull request #{0}{1}Server Instance: {2}", PRnumber, Environment.NewLine, Config.Name));

					if (Result == null)
						try
						{
							UpdateSubmodules();
						}
						catch (Exception e)
						{
							Result = e.ToString();
						}

					if (Result == null)
					{
						Logger.WriteInfo(String.Format("Merged pull request #{0} at commit {1}", PRnumber, prInfo.Sha), EventID.RepoPRMerge);
						try
						{
							var CurrentPRs = GetTestMergeList();
							CurrentPRs.RemoveAll(x => x.Number == PRnumber);
							CurrentPRs.Add(prInfo);
							SetTestMergeList(CurrentPRs);
						}
						catch (Exception e)
						{
							Logger.WriteError("Failed to update PR list", EventID.RepoPRListError);
							return "PR Merged, JSON update failed: " + e.ToString();
						}

						PushTestmergeCommit();
					}
					return Result;
				}
				catch (Exception E)
				{
					Chat.SendMessage("REPO: PR merge failed!", MessageType.DeveloperInfo);
					Logger.WriteWarning(String.Format("Failed to merge pull request #{0}: {1}", PRnumber, E.ToString()), EventID.RepoPRMergeFail);
					return E.ToString();
				}
				finally
				{
					if (task != null)
						task.Wait();
				}
			}
		}

		/// <inheritdoc />
		public bool OperationInProgress()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool PushTestmergeCommits()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string Reset(bool tracked)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public void SetCommitterEmail(string newEmail)
		{
			Config.CommitterEmail = newEmail;
		}

		/// <inheritdoc />
		public void SetCommitterName(string newName)
		{
			Config.CommitterName = newName;
		}

		/// <inheritdoc />
		public void SetPushTestmergeCommits(bool newValue)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string SynchronizePush()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string Update(bool reset)
		{
			throw new NotImplementedException();
		}
	}
}
