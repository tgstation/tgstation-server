using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Server
{
	sealed partial class Instance : ITGRepository, IDisposable
	{
		/// <summary>
		/// The <see cref="Instance"/> directory for the repository
		/// </summary>
		const string RepoPath = "Repository";
		/// <summary>
		/// The branch name used for publishing testmerge commits
		/// </summary>
		const string RemoteTempBranchName = "___TGS3TempBranch";
		/// <summary>
		/// The path to the Repository's <see cref="RepoConfig"/> json
		/// </summary>
		const string RepoTGS3SettingsPath = RepoPath + "/TGS3.json";
		/// <summary>
		/// Path to the <see cref="Instance"/>'s <see cref="RepoConfig"/> json
		/// </summary>
		const string CachedTGS3SettingsPath = "TGS3.json";
		/// <summary>
		/// Error message for when a merge operation fails due to the target branch already having the source branch's commits
		/// </summary>
		const string RepoErrorUpToDate = "Already up to date!";
		/// <summary>
		/// Git remote for push operations
		/// </summary>
		const string SSHPushRemote = "ssh_push_target";
		/// <summary>
		/// The <see cref="Instance"/> directory for the repository SSH keys
		/// </summary>
		const string RepoKeyDir = "RepoKey/";
		/// <summary>
		/// The path to the private ssh-rsa key file
		/// </summary>
		const string PrivateKeyPath = RepoKeyDir + "private_key.txt";
		/// <summary>
		/// The path to the public ssh-rsa key file
		/// </summary>
		const string PublicKeyPath = RepoKeyDir + "public_key.txt";
		/// <summary>
		/// The path to the GitHub token file
		/// </summary>
		const string GitHubTokenPath = RepoKeyDir + "GitHubToken.txt";
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
		/// Used in conjunction with <see cref="RepoBusy"/> for multithreading safety
		/// </summary>
		object RepoLock = new object();
		/// <summary>
		/// Used for multithreading safety. You may not use the repo if this is not locked or <see cref="RepoBusy"/>. Either old the lock while you do work or set <see cref="RepoBusy"/> to <see langword="true"/> then release it, do your work, reaquire it, and set <see cref="RepoBusy"/> back to <see langword="false"/>
		/// </summary>
		bool RepoBusy = false;
		/// <summary>
		/// Whether or not a git operation is in progress. See <see cref="RepoLock"/>
		/// </summary>
		bool Cloning = false;

		/// <summary>
		/// The repository object
		/// </summary>
		Repository Repo;
		/// <summary>
		/// Used for reporting operation progress to the <see cref="TGS.Interface"/>
		/// </summary>
		int currentProgress = -1;

		/// <summary>
		/// Used for automatically updating the <see cref="Instance"/>
		/// </summary>
		System.Timers.Timer autoUpdateTimer = new System.Timers.Timer()
		{
			AutoReset = true
		};

		/// <summary>
		/// Initializes the repository
		/// </summary>
		void InitRepo()
		{
			Directory.CreateDirectory(RelativePath(RepoKeyDir));
			if (Exists())
				UpdateBridgeDll(false);
			if (LoadRepo() == null)
				DisableGarbageCollectionNoLock();
			//start the autoupdate timer
			autoUpdateTimer.Elapsed += AutoUpdateTimer_Elapsed;
			SetAutoUpdateInterval(Config.AutoUpdateInterval);
		}

		/// <summary>
		/// Checks if the <see cref="CachedTGS3SettingsPath"/> and <see cref="RepoTGS3SettingsPath"/> json files match. 
		/// </summary>
		/// <returns><see langword="true"/> if the jsons match, <see langword="false"/> otherwise</returns>
		bool RepoConfigsMatch()
		{
			//this should never be called while the repo is busy
			RepoConfig I = null;
			lock (RepoLock)
				if (!RepoBusy && LoadRepo() == null)
					I = new RepoConfig(RelativePath(RepoTGS3SettingsPath));
			if (I == null)
				throw new Exception("Unable to load TGS3.json from repo!");
			var J = GetCachedRepoConfig();
			return I == J;
		}

		/// <summary>
		/// Gets the <see cref="RepoConfig"/> for <see cref="CachedTGS3SettingsPath"/>
		/// </summary>
		/// <returns>The <see cref="RepoConfig"/> for <see cref="CachedTGS3SettingsPath"/></returns>
		RepoConfig GetCachedRepoConfig()
		{
			return new RepoConfig(RelativePath(CachedTGS3SettingsPath));
		}

		/// <inheritdoc />
		public bool OperationInProgress()
		{
			lock (RepoLock)
				return RepoBusy;
		}

		/// <inheritdoc />
		public int CheckoutProgress()
		{
			return currentProgress;
		}

		/// <summary>
		/// Initializes <see cref="Repo"/>
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string LoadRepo()
		{
			if (Repo != null)
				return null;
			if (!Repository.IsValid(RelativePath(RepoPath)))
				return "Repository does not exist";
			try
			{
				Repo = new Repository(RelativePath(RepoPath));
			}
			catch (Exception e)
			{
				return e.ToString();
			}
			return null;
		}

		/// <summary>
		/// Cleans up <see cref="Repo"/>
		/// </summary>
		void DisposeRepo()
		{
			if (Repo != null)
			{
				Repo.Dispose();
				Repo = null;
			}
		}

		/// <inheritdoc />
		public bool Exists()
		{
			lock (RepoLock)
				return !Cloning && Repository.IsValid(RelativePath(RepoPath));
		}

		/// <summary>
		/// Updates <see cref="currentProgress"/> with the progess of the current <see cref="Repo"/> transfer operation
		/// </summary>
		/// <param name="progress">The <see cref="TransferProgress"/> of the current <see cref="Repo"/> transfer operation</param>
		/// <returns><see langword="true"/></returns>
		bool HandleTransferProgress(TransferProgress progress)
		{
			currentProgress = ((int)(((float)progress.ReceivedObjects / progress.TotalObjects) * 100) / 2) + ((int)(((float)progress.IndexedObjects / progress.TotalObjects) * 100) / 2);
			return true;
		}

		/// <summary>
		/// Updates <see cref="currentProgress"/> with the progess of the current <see cref="Repo"/> checkout operation
		/// </summary>
		/// <param name="path">Ignored</param>
		/// <param name="completedSteps">Dividend for progress calculation</param>
		/// <param name="totalSteps">Divisor for progress calculation</param>
		void HandleCheckoutProgress(string path, int completedSteps, int totalSteps)
		{
			currentProgress = (int)(((float)completedSteps / totalSteps) * 100);
		}

		/// <summary>
		/// Backups up the <see cref="StaticDirs"/> and deletes the current <see cref="RepoPath"/> if they exist. Clones the given branch of the given remote. Sets up new <see cref="StaticDirs"/>
		/// </summary>
		/// <param name="twostrings">Remote and branch name, seperated by a ' '</param>
		void Clone(object twostrings)
		{
			//busy flag set by caller
			var ts = ((string)twostrings).Split(' ');
			var RepoURL = ts[0];
			var BranchName = ts[1];
			try
			{
				SendMessage(String.Format("REPO: {2} started: Cloning {0} branch of {1} ...", BranchName, RepoURL, Repository.IsValid(RepoPath) ? "Full reset" : "Setup"), MessageType.DeveloperInfo);
				try
				{
					DisposeRepo();
					Helpers.DeleteDirectory(RelativePath(RepoPath));
					DeletePRList();
					/*
					lock (configLock)
					{
						BackupAndDeleteStaticDirectory();
					}*/

					var Opts = new CloneOptions()
					{
						BranchName = BranchName,
						RecurseSubmodules = true,
						OnTransferProgress = HandleTransferProgress,
						OnCheckoutProgress = HandleCheckoutProgress,
						CredentialsProvider = GenerateGitCredentials,
					};

					Repository.Clone(RepoURL, RelativePath(RepoPath), Opts);
					currentProgress = -1;
					LoadRepo();

					DisableGarbageCollectionNoLock();

					//create an ssh remote for pushing
					Repo.Network.Remotes.Add(SSHPushRemote, RepoURL.Replace("git://", "ssh://").Replace("https://", "ssh://"));

					if (!Directory.Exists(RelativePath(StaticDirs)))
						InitialConfigureRepository();

					SendMessage("REPO: Clone complete!", MessageType.DeveloperInfo);
					WriteInfo("Repository {0}:{1} successfully cloned", EventID.RepoClone);
				}
				finally
				{
					currentProgress = -1;
				}
			}
			catch (Exception e)

			{
				SendMessage("REPO: Setup failed!", MessageType.DeveloperInfo);
				WriteWarning(String.Format("Failed to clone {2}:{0}: {1}", BranchName, e.ToString(), RepoURL), EventID.RepoCloneFail);
			}
			finally
			{
				lock (RepoLock)
				{
					RepoBusy = false;
					Cloning = false;
				}
			}
		}

		/// <summary>
		/// Turns off the gc.auto git config setting
		/// </summary>

		void DisableGarbageCollectionNoLock()
		{
			Repo.Config.Set("gc.auto", false);
		}

		/// <summary>
		/// Copies the Static directory to the first available Static_BACKUP path in the <see cref="Instance"/> then deleted the old directory
		/// </summary>
		void BackupAndDeleteStaticDirectory()
		{
			var rsd = RelativePath(StaticDirs);
			if (Directory.Exists(rsd))
			{
				int count = 1;

				var rsbd = RelativePath(StaticBackupDir);
				string path = Path.GetDirectoryName(rsbd);
				string newFullPath = rsbd;

				while (File.Exists(newFullPath) || Directory.Exists(newFullPath))
				{
					string tempDirName = string.Format("{0}({1})", rsbd, count++);
					newFullPath = Path.Combine(path, tempDirName);
				}

				Helpers.CopyDirectory(rsd, newFullPath);
			}
			Helpers.DeleteDirectory(rsd);
		}

		/// <summary>
		/// Updates the <see cref="CachedTGS3SettingsPath"/> with the <see cref="RepoTGS3SettingsPath"/>
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		public string UpdateTGS3Json()
		{
			try
			{
				if (File.Exists(RelativePath(RepoTGS3SettingsPath)))
					File.Copy(RelativePath(RepoTGS3SettingsPath), RelativePath(CachedTGS3SettingsPath), true);
				else if (File.Exists(RelativePath(CachedTGS3SettingsPath)))
					File.Delete(RelativePath(CachedTGS3SettingsPath));
			}
			catch (Exception e)
			{
				return e.ToString();
			}
			return null;
		}

		/// <summary>
		/// Initial setup for the <see cref="StaticDirs"/> and <see cref="CachedTGS3SettingsPath"/>
		/// </summary>
		void InitialConfigureRepository()
		{
			Directory.CreateDirectory(RelativePath(StaticDirs));
			UpdateBridgeDll(false);
			UpdateTGS3Json();
			var Config = GetCachedRepoConfig(); //RepoBusy is set if we're here
			foreach (var I in Config.StaticDirectoryPaths)
			{
				try
				{
					var source = Path.Combine(RelativePath(RepoPath), I);
					var dest = Path.Combine(RelativePath(StaticDirs), I);
					if (Directory.Exists(source))
						Helpers.CopyDirectory(source, dest);
					else
						Directory.CreateDirectory(dest);
				}
				catch
				{
					WriteError("Could not setup static directory: " + I, EventID.RepoConfigurationFail);
				}
			}
			foreach (var I in Config.DLLPaths)
			{
				try
				{
					var source = Path.Combine(RelativePath(RepoPath), I);
					if (!File.Exists(source))
					{
						WriteWarning("Could not find DLL: " + I, EventID.RepoConfigurationFail);
						continue;
					}
					var dest = Path.Combine(RelativePath(StaticDirs), I);
					Helpers.CopyFileForceDirectories(source, dest, false);
				}
				catch
				{
					WriteError("Could not setup static DLL: " + I, EventID.RepoConfigurationFail);
				}
			}
		}

		/// <inheritdoc />
		public string Setup(string RepoURL, string BranchName)
		{
			lock (RepoLock)
			{
				if (RepoBusy)
					return "Repo is busy!";
				lock (CompilerLock)
				{
					if (!CompilerIdleNoLock())
						return "Compiler is running!";
				}
				if (RepoURL.Contains("ssh://") && !SSHAuth())
					return String.Format("SSH url specified but either {0} or {1} does not exist in the server directory!", PrivateKeyPath, PublicKeyPath);
				RepoBusy = true;
				Cloning = true;
				new Thread(new ParameterizedThreadStart(Clone))
				{
					IsBackground = true //make sure we don't hold up shutdown
				}.Start(RepoURL + ' ' + BranchName);
				return null;
			}
		}

		/// <summary>
		/// Gets the SHA or branch name of the <see cref="Repo"/>'s HEAD
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <param name="branch">If <see langword="true"/>, returns the branch name instead of the SHA</param>
		/// <param name="tracked">If <see langword="true"/>, returns the tracked branch SHA and ignores <paramref name="branch"/></param>
		/// <returns>The SHA or branch name of the <see cref="Repo"/>'s HEAD</returns>
		string GetShaOrBranch(out string error, bool branch, bool tracked)
		{
			lock (RepoLock)
			{
				if (RepoBusy)
				{
					error = "Repo is busy!";
					return null;
				}
				return GetShaOrBranchNoLock(out error, branch, tracked);
			}
		}

		/// <summary>
		/// Gets the SHA or branch name of the <see cref="Repo"/>'s HEAD. Requires <see cref="RepoLock"/>
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <param name="branch">If <see langword="true"/>, returns the branch name instead of the SHA</param>
		/// <param name="tracked">If <see langword="true"/>, returns the tracked branch SHA and ignores <paramref name="branch"/></param>
		/// <returns>The SHA or branch name of the <see cref="Repo"/>'s HEAD</returns>
		string GetShaOrBranchNoLock(out string error, bool branch, bool tracked)
		{
			var result = LoadRepo();
			if (result != null)
			{
				error = result;
				return null;
			}

			try
			{
				error = null;
				if (tracked && Repo.Head.TrackedBranch != null)
					return Repo.Head.TrackedBranch.Tip.Sha;
				return branch ? Repo.Head.FriendlyName : Repo.Head.Tip.Sha;
			}
			catch (Exception e)
			{
				error = e.ToString();
				return null;
			}
		}

		/// <inheritdoc />
		public string GetHead(bool useTracked, out string error)
		{
			return GetShaOrBranch(out error, false, useTracked);
		}

		/// <inheritdoc />
		public string GetBranch(out string error)
		{
			return GetShaOrBranch(out error, true, false);
		}

		/// <inheritdoc />
		public string GetRemote(out string error)
		{
			try
			{
				var res = LoadRepo();
				if (res != null)
				{
					error = res;
					return null;
				}
				error = null;
				return Repo.Network.Remotes["origin"].Url;
			}
			catch (Exception e)
			{
				error = e.ToString();
				return null;
			}
		}

		/// <summary>
		/// Equivalent of running `git reset --hard` on the repository. Requires <see cref="RepoLock"/>
		/// </summary>
		/// <param name="targetBranch">If not <see langword="null"/>, reset to this branch instead of HEAD</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string ResetNoLock(Branch targetBranch)
		{
			try
			{
				if (targetBranch != null)
					Repo.Reset(ResetMode.Hard, targetBranch.Tip);
				else
					Repo.Reset(ResetMode.Hard);
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public string Checkout(string sha)
		{
			if (sha == LiveTrackingBranch)
				return "I'm sorry Dave, I'm afraid I can't do that...";
			lock (RepoLock)
			{
				if (RepoBusy)
					return "Repo is busy!";
				var result = LoadRepo();
				if (result != null)
					return result;
				SendMessage("REPO: Checking out object: " + sha, MessageType.DeveloperInfo);
				try
				{
					if (Repo.Branches[sha] == null)
					{
						//see if origin has the branch
						result = Fetch();
						var trackedBranch = Repo.Branches[String.Format("origin/{0}", sha)];
						if (trackedBranch != null)
						{
							var newBranch = Repo.CreateBranch(sha, trackedBranch.Tip);
							//track it
							Repo.Branches.Update(newBranch, b => b.TrackedBranch = trackedBranch.CanonicalName);
						}
						else if (result != null)
							return result;
					}

					//if this is a tracked branch, we need to reset to remote first and delete the PR list
					ResetNoLock(Repo.Head.TrackedBranch ?? Repo.Head);
					DeletePRList();

					var Opts = new CheckoutOptions()
					{
						CheckoutModifiers = CheckoutModifiers.Force,
						OnCheckoutProgress = HandleCheckoutProgress,
					};
					Commands.Checkout(Repo, sha, Opts);
					var res = ResetNoLock(null);
					UpdateSubmodules();
					SendMessage("REPO: Checkout complete!", MessageType.DeveloperInfo);
					WriteInfo("Repo checked out " + sha, EventID.RepoCheckout);
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

		/// <summary>
		/// Merges given <paramref name="committish"/> into the current branch
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
			var Result = Repo.Merge(committish, sig, mo);
			currentProgress = -1;
			switch (Result.Status)
			{
				case MergeStatus.Conflicts:
					var status = Repo.RetrieveStatus();
					ResetNoLock(null);
					var conflictedPaths = new List<string>();
					foreach (var file in status)
						if (file.State == FileStatus.Conflicted)
							conflictedPaths.Add(file.FilePath);
					SendMessage(String.Format("REPO: Merge of {0} conflicted, aborted.", committish), MessageType.DeveloperInfo);
					return "Merge conflict occurred. Conflicting files:\n -" + String.Join("\n -", conflictedPaths);
				case MergeStatus.UpToDate:
					return RepoErrorUpToDate;
			}
			if (mergeMessage != null)
				Repo.Commit(mergeMessage, sig, sig);
			return null;
		}

		void PushTestmergeCommit()
		{
			if (Config.PushTestmergeCommits && SSHAuth())
			{
				string NewB = null;
				var targetRemote = Repo.Network.Remotes[SSHPushRemote];
				var options = new PushOptions()
				{
					CredentialsProvider = GenerateGitCredentials
				};
				try
				{
					//now try and push the commit to the remote so they can be referenced
					NewB = Repo.CreateBranch(RemoteTempBranchName).CanonicalName;
					Repo.Network.Push(targetRemote, NewB, options); //push the branch
					Repo.Branches.Remove(NewB);
					var removalString = String.Format(":{0}", NewB);
					NewB = null;
					//we need to delay the second operation a LOT otherwise we get ssh errors
					Thread.Sleep(10000);
					Repo.Network.Push(targetRemote, removalString, options);   //delete the branch
					WriteInfo("Pushed reference commit: " + Repo.Head.Tip.Sha, EventID.ReferencePush);
				}
				catch (Exception e)
				{
					WriteWarning(String.Format("Failed to push reference commit: {0}. Error: {1}", Repo.Head.Tip.Sha, e.ToString()), EventID.ReferencePush);
				}
				finally
				{
					if (NewB != null)
					{
						//Try to delete the branches regardless
						try
						{
							Repo.Branches.Remove(NewB);
						}
						catch { }
						try
						{
							Repo.Network.Push(targetRemote, String.Format(":{0}", NewB), options);
						}
						catch { }
					}
				}
			}
		}

		/// <inheritdoc />
		public string Update(bool reset)
		{
			return UpdateImpl(reset, true);
		}

		/// <summary>
		/// Fetches the origin and merges it into the current branch
		/// </summary>
		/// <param name="reset">If <see langword="true"/>, the operation will perform a hard reset instead of a merge</param>
		/// <param name="successOnUpToDate">If <see langword="true"/>, a return value of <see cref="RepoErrorUpToDate"/> will be changed to <see langword="null"/></param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string UpdateImpl(bool reset, bool successOnUpToDate)
		{
			lock (RepoLock)
			{
				if (RepoBusy)
					return "Repo is busy!";
				var result = LoadRepo();
				if (result != null)
					return result;
				try
				{
					if (Repo.Head == null || !Repo.Head.IsTracking)
						return "Cannot update while not on a tracked branch";

					var res = Fetch();
					if (res != null)
						return res;

					var originBranch = Repo.Head.TrackedBranch;
					if (!successOnUpToDate && Repo.Head.Tip.Sha == originBranch.Tip.Sha)
						return RepoErrorUpToDate;

					SendMessage(String.Format("REPO: Updating origin branch...{0}", reset ? "" : "(Merge)"), MessageType.DeveloperInfo);

					if (reset)
					{
						var error = ResetNoLock(Repo.Head.TrackedBranch);
						UpdateSubmodules();
						if (error != null)
							throw new Exception(error);
						DeletePRList();
						WriteInfo("Repo hard updated to " + originBranch.Tip.Sha, EventID.RepoHardUpdate);
						return error;
					}
					res = MergeBranch(originBranch.FriendlyName, "Merge origin into current testmerge");
					if (!LocalIsRemote())   //might be fast forward
						PushTestmergeCommit();
					if (res != null)
						throw new Exception(res);
					UpdateSubmodules();
					WriteInfo("Repo merge updated to " + originBranch.Tip.Sha, EventID.RepoMergeUpdate);
					return null;
				}
				catch (Exception E)
				{
					SendMessage("REPO: Update failed!", MessageType.DeveloperInfo);
					WriteWarning(String.Format("Repo{0} update failed", reset ? " hard" : ""), reset ? EventID.RepoHardUpdateFail : EventID.RepoMergeUpdateFail);
					return E.ToString();
				}
			}
		}

		/// <summary>
		/// Properly updates any git submodules in the repository
		/// </summary>
		private void UpdateSubmodules()
		{
			var suo = new SubmoduleUpdateOptions
			{
				Init = true
			};
			foreach (var I in Repo.Submodules)
				try
				{
					Repo.Submodules.Update(I.Name, suo);
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
						Repo.Submodules.Update(I.Name, suo);
						var msg = String.Format("I had to reclone submodule {0}. If this is happening a lot find a better hack or fix https://github.com/libgit2/libgit2/issues/3820!", I.Name);
						SendMessage(String.Format("REPO: {0}", msg), MessageType.DeveloperInfo);
						WriteWarning(msg, EventID.Submodule);
					}
					catch (Exception ex)
					{
						WriteError(String.Format("Failed to update submodule {0}! Error: {1}", I.Name, ex.ToString()), EventID.Submodule);
					}
				}
		}

		/// <summary>
		/// Creates a date and timestamped tag of the current HEAD
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string CreateBackup()
		{
			try
			{
				lock (RepoLock)
				{
					if (RepoBusy)
						return "Repo is busy!";
					var res = LoadRepo();
					if (res != null)
						return res;

					//Make sure we don't already have a backup at this commit
					var HEAD = Repo.Head.Tip.Sha;
					foreach (var T in Repo.Tags)
						if (T.Target.Sha == HEAD)
							return null;

					var tagName = "TGS-Compile-Backup-" + DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss");
					var tag = Repo.ApplyTag(tagName);

					if (tag != null)
					{
						WriteInfo("Repo backup created at tag: " + tagName + " commit: " + HEAD, EventID.RepoBackupTag);
						return null;
					}
					throw new Exception("Tag creation failed!");
				}
			}
			catch (Exception e)
			{
				WriteWarning(String.Format("Failed backup tag creation at commit {0}!", Repo.Head.Tip.Sha), EventID.RepoBackupTagFail);
				return e.ToString();
			}
		}

		/// <summary>
		/// Lists tags in the repository created by the <see cref="Instance"/>
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <returns>A dictionary of tag title -> commit SHA on success, <see langword="null"/> on failure</returns>
		public IDictionary<string, string> ListBackups(out string error)
		{
			try
			{
				lock (RepoLock)
				{
					if (RepoBusy)
					{
						error = "Repo is busy!";
						return null;
					}
					error = LoadRepo();
					if (error != null)
						return null;

					var res = new Dictionary<string, string>();
					foreach (var T in Repo.Tags)
						if (T.FriendlyName.Contains("TGS"))
							res.Add(T.FriendlyName, T.Target.Sha);
					return res;
				}
			}
			catch (Exception e)
			{
				error = e.ToString();
				return null;
			}
		}

		/// <inheritdoc />
		public string Reset(bool trackedBranch)
		{
			lock (RepoLock)
			{
				if (RepoBusy)
					return "Repo is busy!";
				var res = LoadRepo() ?? ResetNoLock(trackedBranch ? (Repo.Head.TrackedBranch ?? Repo.Head) : Repo.Head);
				if (res == null)
				{
					SendMessage(String.Format("REPO: Hard reset to {0}branch", trackedBranch ? "tracked " : ""), MessageType.DeveloperInfo);
					if (trackedBranch)
						DeletePRList();
					WriteInfo(String.Format("Repo branch reset{0}", trackedBranch ? " to tracked branch" : ""), trackedBranch ? EventID.RepoResetTracked : EventID.RepoReset);
					return null;
				}
				WriteWarning(String.Format("Failed to reset{0}: {1}", trackedBranch ? " to tracked branch" : "", res), trackedBranch ? EventID.RepoResetTrackedFail : EventID.RepoResetFail);
				return res;
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
		/// Deletes the <see cref="Instance"/>'s <see cref="PRJobFile"/>
		/// </summary>
		void DeletePRList()
		{
			if (File.Exists(RelativePath(PRJobFile)))
				try
				{
					File.Delete(RelativePath(PRJobFile));
				}
				catch (Exception e)
				{
					WriteError("Failed to delete PR list: " + e.ToString(), EventID.RepoPRListError);
				}
		}

		/// <summary>
		/// Deserializes the <see cref="PRJobFile"/>
		/// </summary>
		/// <returns>A <see cref="IDictionary{TKey, TValue}"/> of <see cref="IDictionary{TKey, TValue}"/>. The outer one is keyed by PR# the inner one is keyed by internal <see cref="string"/>s</returns>
		IDictionary<string, IDictionary<string, string>> GetCurrentPRList()
		{
			if (!File.Exists(RelativePath(PRJobFile)))
				return new Dictionary<string, IDictionary<string, string>>();
			var rawdata = File.ReadAllText(RelativePath(PRJobFile));
			return JsonConvert.DeserializeObject<IDictionary<string, IDictionary<string, string>>>(rawdata);
		}

		/// <summary>
		/// Serializes pull request info into <see cref="PRJobFile"/>
		/// </summary>
		/// <param name="list"></param>
		void SetCurrentPRList(IDictionary<string, IDictionary<string, string>> list)
		{
			var rawdata = JsonConvert.SerializeObject(list, Formatting.Indented);
			File.WriteAllText(RelativePath(PRJobFile), rawdata);
		}

		/// <inheritdoc />
		public IEnumerable<string> MergePullRequests(IEnumerable<PullRequestInfo> pullRequestInfos, bool silent)
		{
			var results = new List<string>();
			var amount = pullRequestInfos.Count();
			if (amount < 1)
				return results;

			if (!silent) {
				var messages = new List<string>();
				foreach (var I in pullRequestInfos)
				{
					var shortSha = I.Sha?.Substring(0, Math.Min(I.Sha.Length, 7));
					messages.Add(String.Format("#{0}{1}", I.Number, shortSha != null ? String.Format(" at commit {0}", shortSha) : shortSha));
				}
				SendMessage(String.Format("REPO: Merging PR{0} {1}...", amount == 1 ? "" : "s", String.Join(", ", messages)), MessageType.DeveloperInfo);
			}

			bool foundBad = false;
			foreach (var I in pullRequestInfos)
			{
				var res = MergePullRequest(I.Number, I.Sha, true);
				if (res != null)
					foundBad = true;
				results.Add(res);
			}
			return foundBad ? results : null;
		}

		/// <inheritdoc />
		public string MergePullRequest(ulong PRNumber, string atSHA, bool silent)
		{
			lock (RepoLock)
			{
				if (RepoBusy)
					return "Repo is busy!";
				var result = LoadRepo();
				if (result != null)
					return result;
				if(!silent)
					SendMessage(String.Format("REPO: Merging PR #{0}{1}...", PRNumber, atSHA != null ? String.Format(" at commit {0}", atSHA): ""), MessageType.DeveloperInfo);
				result = ResetNoLock(null);
				if (result != null)
					return result;
				try
				{
					//only supported with github
					var remoteUrl = Repo.Network.Remotes["origin"].Url;
					if (!remoteUrl.Contains("github.com"))
						return "Only supported with Github based repositories.";


					var Refspec = new List<string>();
					var PRBranchName = String.Format("pr-{0}", PRNumber);
					var LocalBranchName = String.Format("pull/{0}/headrefs/heads/{1}", PRNumber, PRBranchName);
					Refspec.Add(String.Format("pull/{0}/head:{1}", PRNumber, PRBranchName));
					var logMessage = "";
					
					//Need to delete the branch first in case of rebase
					Repo.Branches.Remove(LocalBranchName);
					Repo.Branches.Remove(PRBranchName);

					Commands.Fetch(Repo, "origin", Refspec, GenerateFetchOptions(), logMessage);  //shitty api has no failure state for this

					currentProgress = -1;

					var branch = Repo.Branches[LocalBranchName];
					if (branch == null)
					{
						SendMessage(String.Format("REPO: PR {0}could not be fetched. Does it exist?", silent ? String.Format("#{0} ", PRNumber) : ""), MessageType.DeveloperInfo);
						return String.Format("PR #{0} could not be fetched. Does it exist?", PRNumber);
					}

					//give it a better name
					branch = Repo.CreateBranch(PRBranchName, branch.Tip);
					Repo.Branches.Remove(LocalBranchName);

					if (atSHA != null)
					{
						//find the commit
						Commit commit = null;
						string error = null;
						try
						{
							commit = Repo.Lookup<Commit>(atSHA);
						}
						catch (Exception e)
						{
							error = e.ToString();
						}
						if (commit == null)
						{
							SendMessage("REPO: Commit could not be found, aborting!", MessageType.DeveloperInfo);
							return error ?? String.Format("Commit {0} could not be found in the repository!", atSHA);
						}
					}

					//so we'll know if this fails
					var Result = MergeBranch(atSHA ?? branch.CanonicalName, String.Format("[ci skip] Test merge commit for pull request #{0}{1}Server Instance: {2}", PRNumber, Environment.NewLine, Config.Name));

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
						WriteInfo(String.Format("Merged pull request #{0}", PRNumber), EventID.RepoPRMerge);
						var newPR = new Dictionary<string, string>();
						newPR.Add("commit", atSHA ?? branch.Tip.Sha);
						var PRNumberString = PRNumber.ToString();
						IDictionary<string, IDictionary<string, string>> CurrentPRs = null;
						try
						{
							CurrentPRs = GetCurrentPRList();
							CurrentPRs.Remove(PRNumberString);

							//do some excellent remote fuckery here to get the api page
							var prAPI = remoteUrl;
							prAPI = prAPI.Replace("/.git", "");
							prAPI = prAPI.Replace(".git", "");
							prAPI = prAPI.Replace("git:", "https:");
							prAPI = prAPI.Replace("github.com", "api.github.com/repos");
							prAPI += "/pulls/" + PRNumberString + ".json";
							string json;
							using (var wc = new WebClient())
							{
								wc.Headers.Add("user-agent", "TGS.Server");
								var gitHubTokenFile = RelativePath(GitHubTokenPath);
								if (File.Exists(gitHubTokenFile))
									wc.Headers.Add("Authorization", "token " + File.ReadAllText(gitHubTokenFile).Trim());
								json = wc.DownloadString(prAPI);
							}

							var dick = JsonConvert.DeserializeObject<IDictionary<string, object>>(json);
							var user = ((JObject)dick["user"]).ToObject<IDictionary<string, object>>();

							newPR.Add("author", (string)user["login"]);
							newPR.Add("title", (string)dick["title"]);
						}
						catch (Exception)
						{
							WriteError("Failed to get PR metadata for #" + PRNumberString, EventID.RepoPRListError);

							newPR.Add("author", "UNKNOWN");
							newPR.Add("title", "UNKNOWN");
						}

						try
						{
							CurrentPRs.Add(PRNumberString, newPR);
							SetCurrentPRList(CurrentPRs);
						}
						catch (Exception e)
						{
							WriteError("Failed to write PR metadata for #" + PRNumberString, EventID.RepoPRListError);
							return "PR Merged, JSON update failed: " + e.ToString();
						}

						PushTestmergeCommit();
					}
					return Result;
				}
				catch (Exception E)
				{
					SendMessage("REPO: PR merge failed!", MessageType.DeveloperInfo);
					WriteWarning(String.Format("Failed to merge pull request #{0}: {1}", PRNumber, E.ToString()), EventID.RepoPRMergeFail);
					return E.ToString();
				}
			}
		}

		/// <inheritdoc />
		public List<PullRequestInfo> MergedPullRequests(out string error)
		{
			lock (RepoLock)
			{
				if (RepoBusy)
				{
					error = "Repo is busy!";
					return null;
				}
				var result = LoadRepo();
				if (result != null)
				{
					error = result;
					return null;
				}
				try
				{
					var PRRawData = GetCurrentPRList();
					var output = new List<PullRequestInfo>();
					foreach (var I in GetCurrentPRList())
						output.Add(new PullRequestInfo(Convert.ToInt32(I.Key), I.Value["author"], I.Value["title"], I.Value["commit"]));
					error = null;
					return output;
				}
				catch (Exception e)
				{
					error = e.ToString();
					return null;
				}
			}
		}

		/// <inheritdoc />
		public string GetCommitterName()
		{
			return Config.CommitterName;
		}

		/// <inheritdoc />
		public void SetCommitterName(string newName)
		{
			Config.CommitterName = newName;
			Config.Save();
		}

		/// <inheritdoc />
		public string GetCommitterEmail()
		{
			return Config.CommitterEmail;
		}

		/// <inheritdoc />
		public void SetCommitterEmail(string newEmail)
		{
			Config.CommitterEmail = newEmail;
			Config.Save();
		}

		/// <inheritdoc />
		public string SynchronizePush()
		{
			lock (RepoLock)
			{
				if (RepoBusy)
					return "Repo is busy!";
				var Config = GetCachedRepoConfig();
				if (Config == null)
					return "Error reading changelog configuration";
				if (!Config.ChangelogSupport || !SSHAuth())
					return null;
				var res = LoadRepo();
				if (res != null)
					return res;
				return LocalIsRemote() ? Commit(Config) ?? Push() : "Can't push changelog: HEAD does not match tracked remote branch";
			}
		}

		/// <summary>
		/// Create <see cref="FetchOptions"/> that Prune and have the appropriate credentials and progress handler
		/// </summary>
		/// <returns>Properly configured <see cref="FetchOptions"/></returns>
		FetchOptions GenerateFetchOptions()
		{
			return new FetchOptions()
			{
				CredentialsProvider = GenerateGitCredentials,
				OnTransferProgress = HandleTransferProgress,
				Prune = true,
			};
		}

		/// <summary>
		/// Fetches origin
		/// </summary>
		/// <returns>null on success, error message on failure</returns>
		string Fetch()
		{
			try
			{
				string logMessage = "";
				var R = Repo.Network.Remotes["origin"];
				IEnumerable<string> refSpecs = R.FetchRefSpecs.Select(X => X.Specification);
				Commands.Fetch(Repo, R.Name, refSpecs, GenerateFetchOptions(), logMessage);
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		/// <summary>
		/// Check if the current HEAD matches the tracked remote branch HEAD
		/// </summary>
		/// <returns><see langword="true"/> if the current HEAD matches the tracked remote branch HEAD, <see langword="false"/> otherwise</returns>
		bool LocalIsRemote()
		{
			lock (RepoLock)
			{
				if (RepoBusy)
					return false;
				if (LoadRepo() != null)
					return false;
				if (Fetch() != null)
					return false;
				try
				{
					return Repo.Head.IsTracking && Repo.Head.TrackedBranch.Tip.Sha == Repo.Head.Tip.Sha;
				}
				catch
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Create a commit based on a <paramref name="Config"/>. Requires <see cref="RepoLock"/> and <see cref="LoadRepo"/>
		/// </summary>
		/// <param name="Config">A <see cref="RepoConfig"/> with <see cref="RepoConfig.PathsToStage"/></param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string Commit(RepoConfig Config)
		{
			try
			{
				// Stage the file
				foreach (var I in Config.PathsToStage)
					Commands.Stage(Repo, I);

				if (Repo.RetrieveStatus().Staged.Count() == 0)   //nothing to commit
					return null;

				// Create the committer's signature and commit
				var authorandcommitter = MakeSig();

				// Commit to the repository
				WriteInfo(String.Format("Commit {0} created from changelogs", Repo.Commit(CommitMessage, authorandcommitter, authorandcommitter)), EventID.RepoCommit);
				DeletePRList();
				return null;
			}
			catch (Exception e)
			{
				WriteWarning("Repo commit failed: " + e.ToString(), EventID.RepoCommitFail);
				return e.ToString();
			}
		}

		/// <summary>
		/// Push HEAD to <see cref="SSHPushRemote"/>. Requires repo be locked, <see cref="LoadRepo"/>, <see cref="LocalIsRemote"/>, and <see cref="SSHAuth"/>
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string Push()
		{
			try
			{
				var options = new PushOptions()
				{
					CredentialsProvider = GenerateGitCredentials,
				};
				Repo.Network.Push(Repo.Network.Remotes[SSHPushRemote], Repo.Head.CanonicalName, options);
				WriteInfo("Repo pushed up to commit: " + Repo.Head.Tip.Sha, EventID.RepoPush);
				return null;
			}
			catch (Exception e)
			{
				WriteWarning("Repo push failed: " + e.ToString(), EventID.RepoPushFail);
				return e.ToString();
			}
		}

		/// <summary>
		/// Check if the <see cref="Instance"/> is configured for SSH pushing
		/// </summary>
		/// <returns><see langword="true"/> if the see cref="Instance"/> is configured for SSH pushing, <see langword="false"/> otherwise</returns>
		bool SSHAuth()
		{
			return File.Exists(RelativePath(PrivateKeyPath)) && File.Exists(RelativePath(PublicKeyPath));
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
				PrivateKey = RelativePath(PrivateKeyPath),
				PublicKey = RelativePath(PublicKeyPath),
				Passphrase = "",
			};
		}

		/// <inheritdoc />
		public string GenerateChangelog(out string error)
		{
			return GenerateChangelogImpl(out error);
		}

		/// <summary>
		/// Updates the html changelog
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error on failure</param>
		/// <param name="recurse">If <see langword="true"/>, prevents a recursive call to this function after updating pip dependencies</param>
		/// <returns>The output of the python script</returns>
		public string GenerateChangelogImpl(out string error, bool recurse = false)
		{
			var RConfig = GetCachedRepoConfig();
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

			lock (RepoLock)
			{
				if (RepoBusy)
				{
					error = "Repo is busy!";
					return null;
				}
				if (!File.Exists(Path.Combine(RelativePath(RepoPath), ChangelogPy)))
				{
					error = "Missing changelog generation script!";
					return null;
				}

				var pp = Server.Config.PythonPath;
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
						python.StartInfo.WorkingDirectory = new DirectoryInfo(RelativePath(RepoPath)).FullName;
						python.StartInfo.RedirectStandardOutput = true;
						python.StartInfo.RedirectStandardError = true;
						python.Start();
						using (StreamReader reader = python.StandardOutput)
							result = reader.ReadToEnd();
						using (StreamReader reader = python.StandardError)
							result += reader.ReadToEnd();
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
						foreach(var I in RConfig.PipDependancies)
							using (var pip = new Process())
							{
								pip.StartInfo.FileName = PipFile;
								pip.StartInfo.Arguments = "install " + I;
								pip.StartInfo.UseShellExecute = false;
								pip.StartInfo.RedirectStandardOutput = true;
								pip.StartInfo.RedirectStandardError = true;
								pip.Start();
								using (StreamReader reader = pip.StandardOutput)
									result += "\r\n---BEGIN-PIP-OUTPUT---\r\n" + reader.ReadToEnd();
								using (StreamReader reader = pip.StandardError)
									result += reader.ReadToEnd();
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
					WriteInfo("Changelog generated" + error, EventID.RepoChangelog);
					return result;
				}
				catch (Exception e)
				{
					error = e.ToString();
					WriteWarning("Changelog generation failed: " + error, EventID.RepoChangelogFail);
					return null;
				}
			}
		}

		/// <inheritdoc />
		public void SetAutoUpdateInterval(ulong newInterval)
		{
			lock (autoUpdateTimer)
			{
				autoUpdateTimer.Stop();
				if (newInterval > 0) {
					autoUpdateTimer.Interval = newInterval * 60 * 1000;	//convert from minutes to ms
					autoUpdateTimer.Start();
				}
			}
			Config.AutoUpdateInterval = newInterval;
			Config.Save();
		}

		/// <inheritdoc />
		public bool AutoUpdatePreserve(bool? newValue)
		{
			var oldValue = Config.AutoUpdateKeepsPullRequests;
			if (newValue.HasValue)
			{
				Config.AutoUpdateKeepsPullRequests = newValue.Value;
				Config.Save();
			}
			return oldValue;
		}

		/// <inheritdoc />
		public ulong AutoUpdateInterval()
		{
			return Config.AutoUpdateInterval;
		}
		
		/// <summary>
		/// Runs on the configured <see cref="InstanceConfig.AutoUpdateInterval"/> of <see cref="Config"/> and tries to <see cref="Update(bool)"/> and <see cref="Compile(bool)"/> the <see cref="Instance"/>
		/// </summary>
		/// <param name="sender">A <see cref="System.Timers.Timer"/></param>
		/// <param name="e">The event arguments</param>
		private void AutoUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			List<PullRequestInfo> prs = null;

			if (Config.AutoUpdateKeepsPullRequests)
			{
				prs = MergedPullRequests(out string error);
				if (error != null)
					return;
			}
			if (UpdateImpl(true, false) == null)
			{
				if (Config.AutoUpdateKeepsPullRequests)
				{
					var results = MergePullRequests(prs, false);
					if (results != null && results.Any())
						return;
				}
				Compile(true);
			}
		}

		/// <summary>
		/// Updates <see cref="LiveTrackingBranch"/> with the staged <see cref="Compile(bool)"/>'s SHA
		/// </summary>
		/// <param name="newSha">The commit SHA that was staged</param>
		void UpdateLiveSha(string newSha)
		{
			if (LoadRepo() != null)
				return;
			var B = Repo.Branches[LiveTrackingBranch];
			if (B != null)
				Repo.Branches.Remove(B);
			Repo.CreateBranch(LiveTrackingBranch, newSha);		
		}

		/// <summary>
		/// Gets the current staged or live commit SHA
		/// </summary>
		/// <returns>The current staged or live commit SHA</returns>
		public string LiveSha()
		{
			var B = Repo.Branches[LiveTrackingBranch];
			return B != null ? B.Tip.Sha : "UNKNOWN";
		}

		/// <inheritdoc />
		public bool PushTestmergeCommits()
		{
			return Config.PushTestmergeCommits;
		}

		/// <inheritdoc />
		public void SetPushTestmergeCommits(bool newValue)
		{
			Config.PushTestmergeCommits = newValue;
			Config.Save();
		}
	}
}
