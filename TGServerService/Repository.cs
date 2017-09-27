using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using TGServiceInterface;

namespace TGServerService
{
	partial class TGStationServer : ITGRepository, IDisposable
	{
		const string RepoPath = "Repository";
		const string RepoTGS3SettingsPath = RepoPath + "/TGS3.json";
		const string RepoErrorUpToDate = "Already up to date!";
		const string SSHPushRemote = "ssh_push_target";
		const string PrivateKeyPath = "RepoKey/private_key.txt";
		const string PublicKeyPath = "RepoKey/public_key.txt";
		const string PRJobFile = "prtestjob.json";
		const string CommitMessage = "Automatic changelog compile, [ci skip]";

		object RepoLock = new object();
		bool RepoBusy = false;
		bool Cloning = false;

		Repository Repo;
		int currentProgress = -1;

		/// <summary>
		/// Repo specific information about the installation
		/// Requires RepoLock and !RepoBusy to be instantiated
		/// </summary>
		class RepoConfig : IEquatable<RepoConfig>
		{
			public readonly bool ChangelogSupport;
			public readonly string PathToChangelogPy;
			public readonly string ChangelogPyArguments;
			public readonly IList<string> PipDependancies = new List<string>();
			public readonly IList<string> ChangelogPathsToStage = new List<string>();
			public readonly IList<string> StaticDirectoryPaths = new List<string>();
			public readonly IList<string> DLLPaths = new List<string>();

			public RepoConfig()
			{
				if (!File.Exists(RepoTGS3SettingsPath))
					return;
				var rawdata = File.ReadAllText(RepoTGS3SettingsPath);
				var Deserializer = new JavaScriptSerializer();
				var json = Deserializer.Deserialize<IDictionary<string, object>>(rawdata);
				try
				{
					var details = (IDictionary<string, object>)json["changelog"];
					PathToChangelogPy = (string)details["script"];
					ChangelogPyArguments = (string)details["arguments"];
					ChangelogSupport = true;
					try
					{
						PipDependancies = LoadArray(details["pip_dependancies"]);
					}
					catch { }
					try
					{
						ChangelogPathsToStage = LoadArray(details["synchronize_paths"]);
					}
					catch { }
				}
				catch {
					ChangelogSupport = false;
				}
				try
				{
					StaticDirectoryPaths = LoadArray(json["static_directories"]);
				}
				catch { }
				try
				{
					DLLPaths = LoadArray(json["dlls"]);
				}
				catch { }
			}
			private static IList<string> LoadArray(object o)
			{
				var array = (object[])o;
				var res = new List<string>();
				foreach (var I in array)
					res.Add((string)I);
				return res;
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as RepoConfig);
			}

			public bool Equals(RepoConfig other)
			{
				return ChangelogSupport == other.ChangelogSupport
					&& PathToChangelogPy == other.PathToChangelogPy
					&& ChangelogPyArguments == other.ChangelogPyArguments
					&& PipDependancies.Equals(other.PipDependancies)
					&& ChangelogPathsToStage.Equals(other.ChangelogPathsToStage)
					&& StaticDirectoryPaths.Equals(other.StaticDirectoryPaths)
					&& DLLPaths.Equals(other.DLLPaths);
			}

			public override int GetHashCode()
			{
				var hashCode = 1890628544;
				hashCode = hashCode * -1521134295 + ChangelogSupport.GetHashCode();
				hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PathToChangelogPy);
				hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ChangelogPyArguments);
				hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(PipDependancies);
				hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(ChangelogPathsToStage);
				hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(StaticDirectoryPaths);
				hashCode = hashCode * -1521134295 + EqualityComparer<IList<string>>.Default.GetHashCode(DLLPaths);
				return hashCode;
			}

			public static bool operator ==(RepoConfig config1, RepoConfig config2)
			{
				return EqualityComparer<RepoConfig>.Default.Equals(config1, config2);
			}

			public static bool operator !=(RepoConfig config1, RepoConfig config2)
			{
				return !(config1 == config2);
			}
		}

		RepoConfig _CurrentRepoConfig;

		void InitRepo()
		{
			if(Exists())
				UpdateInterfaceDll(false);
		}

		RepoConfig LoadRepoConfig()
		{
			if (_CurrentRepoConfig == null)
				lock (RepoLock)
				{
					if (RepoBusy)
						return null;
					if (LoadRepo() == null)
						_CurrentRepoConfig = new RepoConfig();
				}
			return _CurrentRepoConfig;
		}

		//public api
		public bool OperationInProgress()
		{
			lock (RepoLock)
			{
				return RepoBusy;
			}
		}

		//public api
		public int CheckoutProgress()
		{
			return currentProgress;
		}

		//Sets up the repo object
		string LoadRepo()
		{
			if (Repo != null)
				return null;
			if (!Repository.IsValid(RepoPath))
				return "Repository does not exist";
			try
			{
				Repo = new Repository(RepoPath);
			}
			catch (Exception e)
			{
				return e.ToString();
			}
			return null;
		}

		//Cleans up the repo object
		void DisposeRepo()
		{
			if (Repo != null)
			{
				Repo.Dispose();
				Repo = null;
			}
		}

		//public api
		public bool Exists()
		{
			lock (RepoLock)
			{
				return !Cloning && Repository.IsValid(RepoPath);
			}
		}

		//Updates the currentProgress var
		//no locks required because who gives a shit, it's a fucking 32-bit integer
		bool HandleTransferProgress(TransferProgress progress)
		{
			currentProgress = (int)(((float)progress.ReceivedObjects / progress.TotalObjects) * 100) / 2;
			currentProgress += (int)(((float)progress.IndexedObjects / progress.TotalObjects) * 100) / 2;
			return true;
		}

		//see above
		void HandleCheckoutProgress(string path, int completedSteps, int totalSteps)
		{
			currentProgress = (int)(((float)completedSteps / totalSteps) * 100);
		}

		//For the thread parameter
		private class TwoStrings
		{
			public string a, b;
		}

		//This is the thread that resets za warldo
		//clones, checksout, sets up static dir
		void Clone(object twostrings)
		{
			//busy flag set by caller
			var ts = (TwoStrings)twostrings;
			var RepoURL = ts.a;
			var BranchName = ts.b;
			try
			{
				SendMessage(String.Format("REPO: {2} started: Cloning {0} branch of {1} ...", BranchName, RepoURL, Repository.IsValid(RepoPath) ? "Full reset" : "Setup"), ChatMessageType.DeveloperInfo);
				try
				{
					DisposeRepo();
					Program.DeleteDirectory(RepoPath);
					DeletePRList();
					lock (configLock)
					{
						if (Directory.Exists(StaticDirs))
						{
							int count = 1;

							string path = Path.GetDirectoryName(StaticBackupDir);
							string newFullPath = StaticBackupDir;

							while (File.Exists(newFullPath) || Directory.Exists(newFullPath))
							{
								string tempDirName = string.Format("{0}({1})", StaticBackupDir, count++);
								newFullPath = Path.Combine(path, tempDirName);
							}

							Program.CopyDirectory(StaticDirs, newFullPath);
						}
						Program.DeleteDirectory(StaticDirs);
					}

					var Opts = new CloneOptions()
					{
						BranchName = BranchName,
						RecurseSubmodules = true,
						OnTransferProgress = HandleTransferProgress,
						OnCheckoutProgress = HandleCheckoutProgress,
						CredentialsProvider = GenerateGitCredentials,
					};

					Repository.Clone(RepoURL, RepoPath, Opts);
					currentProgress = -1;
					LoadRepo();

					//create an ssh remote for pushing
					Repo.Network.Remotes.Add(SSHPushRemote, RepoURL.Replace("git://", "ssh://").Replace("https://", "ssh://"));

					InitialConfigureRepository();

					SendMessage("REPO: Clone complete!", ChatMessageType.DeveloperInfo);
					TGServerService.WriteInfo("Repository {0}:{1} successfully cloned", TGServerService.EventID.RepoClone);
				}
				finally
				{
					currentProgress = -1;
				}
			}
			catch (Exception e)

			{
				SendMessage("REPO: Setup failed!", ChatMessageType.DeveloperInfo);
				TGServerService.WriteWarning(String.Format("Failed to clone {2}:{0}: {1}", BranchName, e.ToString(), RepoURL), TGServerService.EventID.RepoCloneFail);
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

		void InitialConfigureRepository()
		{
			Directory.CreateDirectory(StaticDirs);
			UpdateInterfaceDll(false);
			var Config = new RepoConfig();	//RepoBusy is set if we're here
			foreach(var I in Config.StaticDirectoryPaths)
			{
				try
				{
					var source = Path.Combine(RepoPath, I);
					var dest = Path.Combine(StaticDirs, I);
					if (Directory.Exists(source))
						Program.CopyDirectory(source, dest);
					else
						Directory.CreateDirectory(dest);
				}
				catch
				{
					TGServerService.WriteWarning("Could not setup static directory: " + I, TGServerService.EventID.RepoConfigurationFail);
				}
			}
			foreach(var I in Config.DLLPaths)
			{
				try
				{
					var source = Path.Combine(RepoPath, I);
					if (!File.Exists(source))
					{
						TGServerService.WriteWarning("Could not find DLL: " + I, TGServerService.EventID.RepoConfigurationFail);
						continue;
					}
					var dest = Path.Combine(StaticDirs, I);
					Program.CopyFileForceDirectories(source, dest, false);
				}
				catch
				{
					TGServerService.WriteWarning("Could not setup static DLL: " + I, TGServerService.EventID.RepoConfigurationFail);
				}
			}
			_CurrentRepoConfig = Config;
		}

		//kicks off the cloning thread
		//public api
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
				if (DaemonStatus() != TGDreamDaemonStatus.Offline)
					return "DreamDaemon is running!";
				if (RepoURL.Contains("ssh://") && !SSHAuth())
					return String.Format("SSH url specified but either {0} or {1} does not exist in the server directory!", PrivateKeyPath, PublicKeyPath);
				RepoBusy = true;
				Cloning = true;
				new Thread(new ParameterizedThreadStart(Clone))
				{
					IsBackground = true //make sure we don't hold up shutdown
				}.Start(new TwoStrings { a = RepoURL, b = BranchName });
				return null;
			}
		}

		//Gets what HEAD is pointing to
		string GetShaOrBranch(out string error, bool branch, bool tracked)
		{
			lock (RepoLock)
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
		}

		//moist shleppy noises
		//public api
		public string GetHead(bool useTracked, out string error)
		{
			return GetShaOrBranch(out error, false, useTracked);
		}

		//public api
		public string GetBranch(out string error)
		{
			return GetShaOrBranch(out error, true, false);
		}

		//public api
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

		//calls git reset --hard on HEAD
		//requires RepoLock
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

		//public api
		public string Checkout(string sha)
		{
			lock (RepoLock)
			{
				var result = LoadRepo();
				if (result != null)
					return result;
				SendMessage("REPO: Checking out object: " + sha, ChatMessageType.DeveloperInfo);
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
					var Opts = new CheckoutOptions()
					{
						CheckoutModifiers = CheckoutModifiers.Force,
						OnCheckoutProgress = HandleCheckoutProgress,
					};
					Commands.Checkout(Repo, sha, Opts);
					var res = ResetNoLock(null);
					UpdateSubmodules();
					_CurrentRepoConfig = new RepoConfig();
					SendMessage("REPO: Checkout complete!", ChatMessageType.DeveloperInfo);
					TGServerService.WriteInfo("Repo checked out " + sha, TGServerService.EventID.RepoCheckout);
					return res;
				}
				catch (Exception e)
				{
					SendMessage("REPO: Checkout failed!", ChatMessageType.DeveloperInfo);
					TGServerService.WriteWarning(String.Format("Repo checkout of {0} failed: {1}", sha, e.ToString()), TGServerService.EventID.RepoCheckoutFail);
					return e.ToString();
				}
			}
		}

		//Merges a thing into HEAD, not even necessarily a branch
		string MergeBranch(string branchname)
		{
			var mo = new MergeOptions()
			{
				OnCheckoutProgress = HandleCheckoutProgress
			};
			var Result = Repo.Merge(branchname, MakeSig());
			currentProgress = -1;
			switch (Result.Status)
			{
				case MergeStatus.Conflicts:
					ResetNoLock(null);
					SendMessage("REPO: Merge conflicted, aborted.", ChatMessageType.DeveloperInfo);
					return "Merge conflict occurred.";
				case MergeStatus.UpToDate:
					return RepoErrorUpToDate;
			}
			return null;
		}

		//public api
		public string Update(bool reset)
		{
			lock (RepoLock)
			{
				var result = LoadRepo();
				if (result != null)
					return result;
				SendMessage(String.Format("REPO: Updating origin branch...({0})", reset ? "Hard Reset" : "Merge"), ChatMessageType.DeveloperInfo);
				try
				{
					if (Repo.Head == null || !Repo.Head.IsTracking)
						return "Cannot update while not on a tracked branch";

					var res = Fetch();
					if (res != null)
						return res;

					var originBranch = Repo.Head.TrackedBranch;
					if (reset)
					{
						var error = ResetNoLock(Repo.Head.TrackedBranch);
						UpdateSubmodules();
						if (error != null)
							throw new Exception(error);
						DeletePRList();
						TGServerService.WriteInfo("Repo hard updated to " + originBranch.Tip.Sha, TGServerService.EventID.RepoHardUpdate);
						return error;
					}
					res = MergeBranch(originBranch.FriendlyName);
					if (res != null)
						throw new Exception(res);
					UpdateSubmodules();
					_CurrentRepoConfig = new RepoConfig();
					TGServerService.WriteInfo("Repo merge updated to " + originBranch.Tip.Sha, TGServerService.EventID.RepoMergeUpdate);
					return null;
				}
				catch (Exception E)
				{
					SendMessage("REPO: Update failed!", ChatMessageType.DeveloperInfo);
					TGServerService.WriteWarning(String.Format("Repo{0} update failed", reset ? " hard" : ""), reset ? TGServerService.EventID.RepoHardUpdateFail : TGServerService.EventID.RepoMergeUpdateFail);
					return E.ToString();
				}
			}
		}

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
					//workaround for https://github.com/libgit2/libgit2/issues/3820
					//kill off the modules/ folder in .git and try again
					try
					{
						Program.DeleteDirectory(String.Format("{0}/.git/modules/{1}", RepoPath, I.Path));
					}
					catch
					{
						throw e;
					}
					Repo.Submodules.Update(I.Name, suo);
					var msg = String.Format("I had to reclone submodule {0}. If this is happening a lot find a better hack or fix https://github.com/libgit2/libgit2/issues/3820!", I.Name);
					SendMessage(String.Format("REPO: {0}", msg), ChatMessageType.DeveloperInfo);
					TGServerService.WriteWarning(msg, TGServerService.EventID.SubmoduleReclone);
				}
		}

		string CreateBackup()
		{
			try
			{
				lock (RepoLock)
				{
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
						TGServerService.WriteInfo("Repo backup created at tag: " + tagName + " commit: " + HEAD, TGServerService.EventID.RepoBackupTag);
						return null;
					}
					throw new Exception("Tag creation failed!");
				}
			}
			catch (Exception e)
			{
				TGServerService.WriteWarning(String.Format("Failed backup tag creation at commit {0}!", Repo.Head.Tip.Sha), TGServerService.EventID.RepoBackupTagFail);
				return e.ToString();
			}
		}

		public IDictionary<string, string> ListBackups(out string error)
		{
			try
			{
				lock (RepoLock)
				{
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

		//public api
		public string Reset(bool trackedBranch)
		{
			lock (RepoLock)
			{
				var res = LoadRepo() ?? ResetNoLock(trackedBranch ? (Repo.Head.TrackedBranch ?? Repo.Head) : Repo.Head);
				_CurrentRepoConfig = new RepoConfig();
				if (res == null)
				{
					SendMessage(String.Format("REPO: Hard reset to {0}branch", trackedBranch ? "tracked " : ""), ChatMessageType.DeveloperInfo);
					if (trackedBranch)
						DeletePRList();
					TGServerService.WriteInfo(String.Format("Repo branch reset{0}", trackedBranch ? " to tracked branch" : ""), trackedBranch ? TGServerService.EventID.RepoResetTracked : TGServerService.EventID.RepoReset);
					return null;
				}
				TGServerService.WriteWarning(String.Format("Failed to reset{0}: {1}", trackedBranch ? " to tracked branch" : "", res), trackedBranch ? TGServerService.EventID.RepoResetTrackedFail : TGServerService.EventID.RepoResetFail);
				return res;
			}
		}

		//Makes the LibGit2Sharp sig we'll use for committing based on the configured stuff
		Signature MakeSig()
		{
			var Config = Properties.Settings.Default;
			return new Signature(new Identity(Config.CommitterName, Config.CommitterEmail), DateTimeOffset.Now);
		}

		//I wonder...
		void DeletePRList()
		{
			if (File.Exists(PRJobFile))
				try
				{
					File.Delete(PRJobFile);
				}
				catch (Exception e)
				{
					TGServerService.WriteError("Failed to delete PR list: " + e.ToString(), TGServerService.EventID.RepoPRListError);
				}
		}

		//json_decode(file2text())
		IDictionary<string, IDictionary<string, string>> GetCurrentPRList()
		{
			if (!File.Exists(PRJobFile))
				return new Dictionary<string, IDictionary<string, string>>();
			var rawdata = File.ReadAllText(PRJobFile);
			var Deserializer = new JavaScriptSerializer();
			return Deserializer.Deserialize<IDictionary<string, IDictionary<string, string>>>(rawdata);
		}

		//text2file(json_encode())
		void SetCurrentPRList(IDictionary<string, IDictionary<string, string>> list)
		{
			var Serializer = new JavaScriptSerializer();
			var rawdata = Serializer.Serialize(list);
			File.WriteAllText(PRJobFile, rawdata);
		}

		//public api
		public string MergePullRequest(int PRNumber)
		{
			return MergePullRequestImpl(PRNumber, false);
		}
		string MergePullRequestImpl(int PRNumber, bool impliedUpdate)
		{
			lock (RepoLock)
			{
				var result = LoadRepo();
				if (result != null)
					return result;
				SendMessage(String.Format("REPO: {1}erging PR #{0}...", PRNumber, impliedUpdate ? "Test m" : "M"), ChatMessageType.DeveloperInfo);
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

					var branch = Repo.Branches[LocalBranchName];
					if (branch != null)
						//Need to delete the branch first in case of rebase
						Repo.Branches.Remove(branch);

					Commands.Fetch(Repo, "origin", Refspec, GenerateFetchOptions(), logMessage);  //shitty api has no failure state for this

					currentProgress = -1;

					var Config = Properties.Settings.Default;


					branch = Repo.Branches[LocalBranchName];
					if (branch == null)
					{
						SendMessage("REPO: PR could not be fetched. Does it exist?", ChatMessageType.DeveloperInfo);
						return String.Format("PR #{0} could not be fetched. Does it exist?", PRNumber);
					}

					//so we'll know if this fails
					var Result = MergeBranch(LocalBranchName);

					if (Result == null)
						try
						{
							_CurrentRepoConfig = new RepoConfig();
							UpdateSubmodules();
						}
						catch (Exception e)
						{
							Result = e.ToString();
						}

					if (Result == null)
					{
						TGServerService.WriteInfo(String.Format("Merged pull request #{0}", PRNumber), TGServerService.EventID.RepoPRMerge);
						try
						{
							var CurrentPRs = GetCurrentPRList();
							var PRNumberString = PRNumber.ToString();
							CurrentPRs.Remove(PRNumberString);
							var newPR = new Dictionary<string, string>();

							//do some excellent remote fuckery here to get the api page
							var prAPI = remoteUrl;
							prAPI = prAPI.Replace("/.git", "");
							prAPI = prAPI.Replace(".git", "");
							prAPI = prAPI.Replace("github.com", "api.github.com/repos");
							prAPI += "/pulls/" + PRNumberString + ".json";
							string json;
							using (var wc = new WebClient())
							{
								wc.Headers.Add("user-agent", "TGStationServerService");
								json = wc.DownloadString(prAPI);
							}

							var Deserializer = new JavaScriptSerializer();
							var dick = Deserializer.DeserializeObject(json) as IDictionary<string, object>;
							var user = dick["user"] as IDictionary<string, object>;

							newPR.Add("commit", branch.Tip.Sha);
							newPR.Add("author", (string)user["login"]);
							newPR.Add("title", (string)dick["title"]);
							CurrentPRs.Add(PRNumberString, newPR);
							SetCurrentPRList(CurrentPRs);
						}
						catch (Exception e)
						{
							TGServerService.WriteError("Failed to update PR list", TGServerService.EventID.RepoPRListError);
							return "PR Merged, JSON update failed: " + e.ToString();
						}
					}
					return Result;
				}
				catch (Exception E)
				{
					SendMessage("REPO: PR merge failed!", ChatMessageType.DeveloperInfo);
					TGServerService.WriteWarning(String.Format("Failed to merge pull request #{0}: {1}", PRNumber, E.ToString()), TGServerService.EventID.RepoPRMergeFail);
					return E.ToString();
				}
			}
		}

		//public api
		public IList<PullRequestInfo> MergedPullRequests(out string error)
		{
			lock (RepoLock)
			{
				var result = LoadRepo();
				if (result != null)
				{
					error = result;
					return null;
				}
				try
				{
					var PRRawData = GetCurrentPRList();
					IList<PullRequestInfo> output = new List<PullRequestInfo>();
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

		//public api
		public string GetCommitterName()
		{
			lock (RepoLock)
			{
				return Properties.Settings.Default.CommitterName;
			}
		}

		//public api
		public void SetCommitterName(string newName)
		{
			lock (RepoLock)
			{
				Properties.Settings.Default.CommitterName = newName;
			}
		}

		//public api
		public string GetCommitterEmail()
		{
			lock (RepoLock)
			{
				return Properties.Settings.Default.CommitterEmail;
			}
		}

		//public api
		public void SetCommitterEmail(string newEmail)
		{
			lock (RepoLock)
			{
				Properties.Settings.Default.CommitterEmail = newEmail;
			}
		}

		public string PushChangelog()
		{
			var Config = LoadRepoConfig();
			if (Config == null)
				return "Error reading changelog configuration";
			if(!Config.ChangelogSupport || !SSHAuth())
				return null;
			return LocalIsRemote() ? Commit(Config) ?? Push() : "Can't push changelog: HEAD does not match tracked remote branch";
		}

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

		bool LocalIsRemote()
		{
			lock (RepoLock)
			{
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

		string Commit(RepoConfig Config)
		{
			lock (RepoLock)
			{
				var result = LoadRepo();
				if (result != null)
					return result;
				try
				{
					// Stage the file
					foreach(var I in Config.ChangelogPathsToStage)
						Commands.Stage(Repo, I);

					var status = Repo.RetrieveStatus();
					var sum = status.Added.Count() + status.Removed.Count() + status.Modified.Count();

					if (sum == 0)   //nothing to commit
						return null;

					// Create the committer's signature and commit
					var authorandcommitter = MakeSig();

					// Commit to the repository
					TGServerService.WriteInfo(String.Format("Commit {0} created from changelogs", Repo.Commit(CommitMessage, authorandcommitter, authorandcommitter)), TGServerService.EventID.RepoCommit);
					DeletePRList();
					return null;
				}
				catch (Exception e)
				{
					TGServerService.WriteError("Repo commit failed: " + e.ToString(), TGServerService.EventID.RepoCommitFail);
					return e.ToString();
				}
			}
		}

		//public api
		string Push()
		{
			if (LocalIsRemote())    //nothing to push
				return null;
			lock (RepoLock)
			{
				var result = LoadRepo();
				if (result != null)
					return result;
				try
				{
					if (!SSHAuth())
						return String.Format("Either {0} or {1} is missing from the server directory. Unable to push!", PrivateKeyPath, PublicKeyPath);

					var options = new PushOptions()
					{
						CredentialsProvider = GenerateGitCredentials,
					};
					Repo.Network.Push(Repo.Network.Remotes[SSHPushRemote], Repo.Head.CanonicalName, options);
					TGServerService.WriteError("Repo pushed up to commit: " + Repo.Head.Tip.Sha, TGServerService.EventID.RepoPush);
					return null;
				}
				catch (Exception e)
				{
					TGServerService.WriteError("Repo push failed: " + e.ToString(), TGServerService.EventID.RepoPushFail);
					return e.ToString();
				}
			}
		}

		bool SSHAuth()
		{
			return File.Exists(PrivateKeyPath) && File.Exists(PublicKeyPath);
		}

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
				PrivateKey = PrivateKeyPath,
				PublicKey = PublicKeyPath,
				Passphrase = "",
			};
		}

		//public api
		public string GenerateChangelog(out string error)
		{
			return GenerateChangelogImpl(out error);
		}

		//impl proc just for single level recursion
		public string GenerateChangelogImpl(out string error, bool recurse = false)
		{
			var RConfig = LoadRepoConfig();
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

			string ChangelogPy = Path.Combine(RepoPath, RConfig.PathToChangelogPy);
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
				if (!File.Exists(ChangelogPy))
				{
					error = "Missing changelog generation script!";
					return null;
				}

				var Config = Properties.Settings.Default;

				var PythonFile = Config.PythonPath + "/python.exe";
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

						string PipFile = Config.PythonPath + "/scripts/pip.exe";
						foreach(var I in RConfig.PipDependancies)
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
					TGServerService.WriteInfo("Changelog generated" + error, TGServerService.EventID.RepoChangelog);
					return result;
				}
				catch (Exception e)
				{
					error = e.ToString();
					TGServerService.WriteWarning("Changelog generation failed: " + error, TGServerService.EventID.RepoChangelogFail);
					return null;
				}
			}
		}

		//public api
		public bool SetPythonPath(string path)
		{
			if (!Directory.Exists(path))
				return false;
			Properties.Settings.Default.PythonPath = Path.GetFullPath(path);
			return true;
		}

		public string PythonPath()
		{
			return Properties.Settings.Default.PythonPath;
		}
	}
}
