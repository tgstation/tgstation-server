using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	sealed class RepositoryManager : IRepositoryManager, IDisposable
	{
		/// <summary>
		/// The name of the primary remote used in all operations
		/// </summary>
		public const string DefaultRemote = "origin";
		/// <summary>
		/// Path to the repository directory in the <see cref="Instance"/> directory
		/// </summary>
		public const string RepoPath = "Repository";
		/// <summary>
		/// Message returned when an operation is aborted due to <see cref="longOperationInProgress"/> being set
		/// </summary>
		const string BusyMessage = "Repository is busy with another operation!";
		/// <summary>
		/// Message returned when <see cref="LoadRepository(bool)"/> fails
		/// </summary>
		const string NoRepoMessage = "Repository is not in a valid state!";
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
		/// The <see cref="IServerConfig"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IServerConfig ServerConfig;
		/// <summary>
		/// The <see cref="IRepoConfigProvider"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IRepoConfigProvider RepoConfigProvider;

		/// <summary>
		/// The <see cref="Repository"/> the <see cref="RepositoryManager"/> manages
		/// </summary>
		IRepository repository;
		/// <summary>
		/// <see cref="Octokit.GitHubClient"/> used for retrieving pull request information from GitHub
		/// </summary>
		Octokit.IGitHubClient ghClient;
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
		/// <param name="serverConfig">The value of <see cref="ServerConfig"/></param>
		/// <param name="repoConfigProvider">The value of <see cref="RepoConfigProvider"/></param>
		public RepositoryManager(IInstanceLogger logger, IChatManager chat, IInstanceConfig config, IIOManager io, IRepositoryProvider repositoryProvider, IServerConfig serverConfig, IRepoConfigProvider repoConfigProvider)
		{
			Logger = logger;
			Chat = chat;
			Config = config;
			IO = io;
			RepositoryProvider = repositoryProvider;
			ServerConfig = serverConfig;
			RepoConfigProvider = repoConfigProvider;

			Chat.OnPopulateCommandInfo += (a, b) => b.CommandInfo.Repo = this;

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
			lock (this)
			{
				if (longOperationInProgress)
					return BusyMessage;
				if (!LoadRepository())
					return NoRepoMessage;
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
						Reset(null);
						Chat.SendMessage("REPO: Merge conflicted, aborted.", MessageType.DeveloperInfo);
						return "Merge conflict occurred.";
					case MergeStatus.UpToDate:
						return RepoErrorUpToDate;
				}
				if (mergeMessage != null)
					repository.Commit(mergeMessage, sig, sig);
				return null;
			}
		}

		/// <summary>
		/// Equivalent of running `git reset --hard` on the repository
		/// </summary>
		/// <param name="targetBranch">If not <see langword="null"/>, reset to this branch instead of HEAD</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string Reset(Branch targetBranch)
		{
			lock (this)
			{
				if (longOperationInProgress)
					return BusyMessage;
				if (!LoadRepository())
					return NoRepoMessage;
				try
				{
					if (targetBranch != null)
						repository.Reset(ResetMode.Hard, targetBranch.Tip);
					else
						repository.Reset(ResetMode.Hard);
					return null;
				}
				catch (Exception e)
				{
					return e.ToString();
				}
			}
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
							IO.DeleteDirectory(String.Format("{0}/.git/modules/{1}", RepoPath, I.Path)).Wait();
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
				if (!bypassSanityCheck && longOperationInProgress)    //sanity check
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
						IO.DeleteFile(PRJobFile).Wait();
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
					return JsonConvert.DeserializeObject<List<PullRequestInfo>>(IO.ReadAllText(PRJobFile).Result);
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
				IO.WriteAllText(PRJobFile, JsonConvert.SerializeObject(infos)).Wait();
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

		/// <inheritdoc />
		public string UpdateImpl(bool reset, bool successOnUpToDate)
		{
			lock (this)
			{
				if (longOperationInProgress)
					return BusyMessage;
				if (!LoadRepository())
					return NoRepoMessage;
				try
				{
					if (repository.Head == null || !repository.Head.IsTracking)
						return "Cannot update while not on a tracked branch";

					var res = RepositoryProvider.Fetch(repository);
					if (res != null)
						return res;

					var originBranch = repository.Head.TrackedBranch;
					if (!successOnUpToDate && repository.Head.Tip.Sha == originBranch.Tip.Sha)
						return RepoErrorUpToDate;

					Chat.SendMessage(String.Format("REPO: Updating origin branch...{0}", reset ? "" : "(Merge)"), MessageType.DeveloperInfo);

					if (reset)
					{
						var error = Reset(repository.Head.TrackedBranch);
						UpdateSubmodules();
						if (error != null)
							throw new Exception(error);
						DeletePRList();
						Logger.WriteInfo("Repo hard updated to " + originBranch.Tip.Sha, EventID.RepoHardUpdate);
						return error;
					}

					res = MergeBranch(originBranch.FriendlyName, "Merge origin into current testmerge");
					if (!LocalIsRemote())   //might be fast forward
						PushTestmergeCommit();
					if (res != null)
						throw new Exception(res);
					UpdateSubmodules();
					Logger.WriteInfo("Repo merge updated to " + originBranch.Tip.Sha, EventID.RepoMergeUpdate);
					return null;
				}
				catch (Exception e)
				{
					Chat.SendMessage("REPO: Update failed!", MessageType.DeveloperInfo);
					Logger.WriteWarning(String.Format("Repo{0} update failed! Error: {1}", reset ? " hard" : "", e.ToString()), reset ? EventID.RepoHardUpdateFail : EventID.RepoMergeUpdateFail);
					return e.ToString();
				}
			}
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

		/// <summary>
		/// Check if the current HEAD matches the tracked remote branch HEAD
		/// </summary>
		/// <returns><see langword="true"/> if the current HEAD matches the tracked remote branch HEAD, <see langword="false"/> otherwise</returns>
		bool LocalIsRemote()
		{
			lock (this)
			{
				if (longOperationInProgress || !LoadRepository())
					return false;
				try
				{
					return repository.Head.IsTracking && repository.Head.TrackedBranch.Tip.Sha == repository.Head.Tip.Sha;
				}
				catch
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Updates the html changelog
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error on failure</param>
		/// <param name="recurse">If <see langword="true"/>, prevents a recursive call to this function after updating pip dependencies</param>
		/// <returns>The output of the python script</returns>
		public string GenerateChangelogImpl(out string error, bool recurse = false)
		{
			var RConfig = RepoConfigProvider.GetRepoConfig();
			if (RConfig == null)
			{
				error = null;
				return "Error loading changelog config!";
			}
			if (!RConfig.ChangelogSupport)
			{
				error = null;
				return null;
			}

			string ChangelogPy = RConfig.PathToChangelogPy;
			if (!Exists())
			{
				error = "Repo does not exist!";
				return null;
			}

			lock (this)
			{
				if (longOperationInProgress)
				{
					error = BusyMessage;
					return null;
				}
				if (!IO.FileExists(Path.Combine(RepoPath, ChangelogPy)))
				{
					error = "Missing changelog generation script!";
					return null;
				}

				var pp = ServerConfig.PythonPath;
				var PythonFile = Path.Combine(pp, "python.exe");
				if (!File.Exists(PythonFile))
				{
					error = "Cannot locate python!";
					return null;
				}
				try
				{
					string result;
					int exitCode;
					using (var python = new Process())
					{
						python.StartInfo.FileName = PythonFile;
						python.StartInfo.Arguments = String.Format("{0} {1}", ChangelogPy, RConfig.ChangelogPyArguments);
						python.StartInfo.UseShellExecute = false;
						python.StartInfo.WorkingDirectory = IO.ResolvePath(RepoPath);
						python.StartInfo.RedirectStandardOutput = true;
						python.Start();
						using (StreamReader reader = python.StandardOutput)
						{
							result = reader.ReadToEnd();

						}
						python.WaitForExit();
						exitCode = python.ExitCode;
					}
					if (exitCode != 0)
					{
						if (recurse || RConfig.PipDependancies.Count == 0)
						{
							error = "Script failed!";
							return result;
						}
						//update pip deps and try again

						string PipFile = Path.Combine(pp, "scripts", "pip.exe");
						foreach (var I in RConfig.PipDependancies)
							using (var pip = new Process())
							{
								pip.StartInfo.FileName = PipFile;
								pip.StartInfo.Arguments = "install " + I;
								pip.StartInfo.UseShellExecute = false;
								pip.StartInfo.RedirectStandardOutput = true;
								pip.Start();
								using (StreamReader reader = pip.StandardOutput)
								{
									result += "\r\n---BEGIN-PIP-OUTPUT---\r\n" + reader.ReadToEnd();
								}
								pip.WaitForExit();
								if (pip.ExitCode != 0)
								{
									error = "Script and pip failed!";
									return result;
								}
							}
						//and recurse
						return GenerateChangelogImpl(out error, true);
					}
					error = null;
					Logger.WriteInfo("Changelog generated" + error, EventID.RepoChangelog);
					return result;
				}
				catch (Exception e)
				{
					error = e.ToString();
					Logger.WriteWarning("Changelog generation failed: " + error, EventID.RepoChangelogFail);
					return null;
				}
			}
		}

		/// <summary>
		/// Create a commit based on a <paramref name="Config"/>
		/// </summary>
		/// <param name="Config">A <see cref="IRepoConfig"/> with <see cref="IRepoConfig.PathsToStage"/></param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string Commit(IRepoConfig Config)
		{
			lock (this)
			{
				if (longOperationInProgress)
					return BusyMessage;
				if (!LoadRepository())
					return NoRepoMessage;
				try
				{
					// Stage the file
					foreach (var I in Config.PathsToStage)
						RepositoryProvider.Stage(repository, I);

					if (repository.RetrieveStatus().Staged.Count() == 0)   //nothing to commit
						return null;

					// Create the committer's signature and commit
					var authorandcommitter = MakeSig();

					// Commit to the repository
					Logger.WriteInfo(String.Format("Commit {0} created from changelogs", repository.Commit(CommitMessage, authorandcommitter, authorandcommitter)), EventID.RepoCommit);
					return null;
				}
				catch (Exception e)
				{
					Logger.WriteWarning("Repo commit failed: " + e.ToString(), EventID.RepoCommitFail);
					return e.ToString();
				}
			}
		}

		/// <summary>
		/// Pushes HEAD to the remote tracked branch. Only allows fast-forwards
		/// </summary>
		/// <returns></returns>
		string Push()
		{
			lock (this)
			{
				if (longOperationInProgress)
					return BusyMessage;
				if (!LoadRepository())
					return NoRepoMessage;
				if (LocalIsRemote())    //nothing to push
					return null;
				try
				{
					if (!SSHAuthAvailable())
						return String.Format("Either {0} or {1} is missing from the server directory. Unable to push!", PrivateKeyPath, PublicKeyPath);

					var options = new PushOptions()
					{
						CredentialsProvider = GenerateGitCredentials,
					};
					repository.Network.Push(repository.Network.Remotes[SSHPushRemote], repository.Head.CanonicalName, options);
					Logger.WriteInfo("Repo pushed up to commit: " + repository.Head.Tip.Sha, EventID.RepoPush);
					return null;
				}
				catch (Exception e)
				{
					Logger.WriteWarning("Repo push failed: " + e.ToString(), EventID.RepoPushFail);
					return e.ToString();
				}
			}
		}

		/// <summary>
		/// Gets the SHA or branch name of the <see cref="repository"/>'s HEAD
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <param name="branch">If <see langword="true"/>, returns the branch name instead of the SHA</param>
		/// <param name="tracked">If <see langword="true"/>, returns the tracked branch SHA and ignores <paramref name="branch"/></param>
		/// <returns>The SHA or branch name of the <see cref="repository"/>'s HEAD</returns>
		string GetShaOrBranch(out string error, bool branch, bool tracked)
		{
			lock (this)
			{
				if (longOperationInProgress)
				{
					error = BusyMessage;
					return null;
				}
				if (!LoadRepository())
				{
					error = NoRepoMessage;
					return null;
				}

				try
				{
					error = null;
					if (tracked && repository.Head.TrackedBranch != null)
						return repository.Head.TrackedBranch.Tip.Sha;
					return branch ? repository.Head.FriendlyName : repository.Head.Tip.Sha;
				}
				catch (Exception e)
				{
					error = e.ToString();
					return null;
				}
			}
		}

		/// <inheritdoc />
		public string Checkout(string objectName)
		{
			if (objectName == RemoteTempBranchName || objectName == LiveTrackingBranch)
				return "I'm sorry Dave, I'm afraid I can't do that...";
			lock (this)
			{
				if (longOperationInProgress)
					return BusyMessage;
				if (!LoadRepository())
					return NoRepoMessage;
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
					Reset(repository.Head.TrackedBranch ?? repository.Head);
					DeletePRList();

					RepositoryProvider.Checkout(repository, objectName);

					var res = Reset(null);

					UpdateSubmodules();

					Chat.SendMessage("REPO: Checkout complete!", MessageType.DeveloperInfo);
					Logger.WriteInfo(String.Format("Repo checked out {0}", objectName), EventID.RepoCheckout);
					return res;
				}
				catch (Exception e)
				{
					Chat.SendMessage("REPO: Checkout failed!", MessageType.DeveloperInfo);
					Logger.WriteWarning(String.Format("Repo checkout of {0} failed: {1}", objectName, e.ToString()), EventID.RepoCheckoutFail);
					return e.ToString();
				}
			}
		}

		/// <inheritdoc />
		public int CheckoutProgress()
		{
			return currentProgress;
		}

		/// <inheritdoc />
		public string Clone(string remote, string branch = "master")
		{
			lock (this)
			{
				if (longOperationInProgress)
					return BusyMessage;

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
						IO.DeleteDirectory(RepoPath).Wait();
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
						
						lock (this)
							longOperationInProgress = false;

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
							IO.DeleteDirectory(RepoPath).Wait();
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
		public async Task<string> CopyTo(string destination, IEnumerable<string> ignorePaths)
		{
			lock (this)
			{
				if (longOperationInProgress)
					return BusyMessage;
				if (!LoadRepository())
					return NoRepoMessage;
				longOperationInProgress = true;
			}

			try
			{
				await IO.CopyDirectory(RepoPath, destination, ignorePaths);
				IO.CopyFile(PRJobFile, Path.Combine(destination, PRJobFile), false).Wait();
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
			finally
			{
				lock(this)
					longOperationInProgress = false;
			}
		}

		/// <inheritdoc />
		public async Task<string> CopyToRestricted(string destination, IEnumerable<string> onlyPaths)
		{
			var tasks = new List<Task>();
			try
			{
				foreach (var I in onlyPaths)
				{
					var path = Path.Combine(RepoPath, I);
					var dest = Path.Combine(destination, I);
					if (IO.FileExists(path))
						tasks.Add(IO.CopyFile(path, dest, false));
					else if (IO.DirectoryExists(path))
						tasks.Add(IO.CopyDirectory(path, dest));
				}
				await Task.WhenAll(tasks);
				return null;
			}
			catch(Exception e)
			{
				try
				{
					Task.WaitAll(tasks.ToArray());
				}
				catch { }
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public string CreateBackup()
		{
			try
			{
				lock (this)
				{
					if (longOperationInProgress)
						return BusyMessage;
					if (!LoadRepository())
						return NoRepoMessage;

					//Make sure we don't already have a backup at this commit
					var HEAD = repository.Head.Tip.Sha;
					foreach (var T in repository.Tags)
						if (T.Target.Sha == HEAD)
							return null;

					var tagName = "TGS-Compile-Backup-" + DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss");
					var tag = repository.ApplyTag(tagName);

					if (tag != null)
					{
						Logger.WriteInfo("Repo backup created at tag: " + tagName + " commit: " + HEAD, EventID.RepoBackupTag);
						return null;
					}
					throw new Exception("Tag creation failed!");
				}
			}
			catch (Exception e)
			{
				Logger.WriteWarning(String.Format("Failed backup tag creation at commit {0}!", repository.Head.Tip.Sha), EventID.RepoBackupTagFail);
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public string GenerateChangelog(out string error)
		{
			return GenerateChangelogImpl(out error);
		}

		/// <inheritdoc />
		public string GetBranch(out string error)
		{
			return GetShaOrBranch(out error, true, false);
		}

		/// <inheritdoc />
		public string GetCommitterEmail()
		{
			return Config.CommitterEmail;
		}

		/// <inheritdoc />
		public string GetCommitterName()
		{
			return Config.CommitterName;
		}

		/// <inheritdoc />
		public string GetHead(bool useTracked, out string error)
		{
			return GetShaOrBranch(out error, false, useTracked);
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
			lock (this)
			{
				if (longOperationInProgress)
				{
					error = BusyMessage;
					return null;
				}
				if (!LoadRepository())
				{
					error = NoRepoMessage;
					return null;
				}
				var res = new Dictionary<string, string>();
				foreach (var T in repository.Tags)
					if (T.FriendlyName.Contains("TGS"))
						res.Add(T.FriendlyName, T.Target.Sha);
				error = null;
				return res;
			}
		}

		/// <inheritdoc />
		public List<PullRequestInfo> MergedPullRequests(out string error)
		{
			lock (this)
			{
				if (longOperationInProgress)
				{
					error = BusyMessage;
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
				if (longOperationInProgress)
					return BusyMessage;
				if (!LoadRepository())
					return NoRepoMessage;

				Chat.SendMessage(String.Format("REPO: Merging PR #{0}{1}...", PRnumber, atSHA != null ? String.Format(" at commit {0}", atSHA) : ""), MessageType.DeveloperInfo);

				var result = Reset(null);
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
			var res = Monitor.TryEnter(this);
			if (res)
			{
				res = longOperationInProgress;
				Monitor.Exit(this);
			}
			return res;
		}

		/// <inheritdoc />
		public bool PushTestmergeCommits()
		{
			return Config.PushTestmergeCommits;
		}

		/// <inheritdoc />
		public string Reset(bool tracked)
		{
			lock (this)
			{
				if (longOperationInProgress)
					return BusyMessage;
				var res = Reset(tracked ? (repository.Head.TrackedBranch ?? repository.Head) : repository.Head);
				if (res != null)
				{
					Logger.WriteWarning(String.Format("Failed to reset{0}: {1}", tracked ? " to tracked branch" : "", res), tracked ? EventID.RepoResetTrackedFail : EventID.RepoResetFail);
					return res;
				}
				Chat.SendMessage(String.Format("REPO: Hard reset to {0}branch", tracked ? "tracked " : ""), MessageType.DeveloperInfo);
				if (tracked)
					DeletePRList();
				Logger.WriteInfo(String.Format("Repo branch reset{0}", tracked ? " to tracked branch" : ""), tracked ? EventID.RepoResetTracked : EventID.RepoReset);
				return null;
			}
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
			Config.PushTestmergeCommits = newValue;
		}

		/// <inheritdoc />
		public string SynchronizePush()
		{
			var Config = GetRepoConfig();
			if (Config == null)
				return "Error reading changelog configuration";
			if (!Config.ChangelogSupport || !SSHAuthAvailable())
				return null;
			return LocalIsRemote() ? Commit(Config) ?? Push() : "Can't push changelog: HEAD does not match tracked remote branch";
		}

		/// <inheritdoc />
		public string Update(bool reset)
		{
			return UpdateImpl(reset, true);
		}

		/// <inheritdoc />
		public bool Exists()
		{
			lock (this)
				return longOperationInProgress || LoadRepository();
		}

		/// <inheritdoc />
		public IRepoConfig GetRepoConfig()
		{
			lock (this)
			{
				if (longOperationInProgress)
					return null;
				return new RepoConfig(RepoPath, IO);
			}
		}

		/// <inheritdoc />
		public void UpdateLiveSha(string newSha)
		{
			lock (this)
			{
				if (longOperationInProgress || !LoadRepository())
					return;
				var B = repository.Branches[LiveTrackingBranch];
				if (B != null)
					repository.Branches.Remove(B);
				repository.CreateBranch(LiveTrackingBranch, newSha);
			}
		}

		/// <inheritdoc />
		public string LiveSha()
		{
			lock (this)
			{
				if (!LoadRepository())
					return null;
				var B = repository.Branches[LiveTrackingBranch];
				return B?.Tip?.Sha;
			}
		}
	}
}
