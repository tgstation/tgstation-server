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
		const string RepoConfig = RepoPath + "/config";
		const string RepoData = RepoPath + "/data";
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

		//public api
		public bool OperationInProgress()
		{
			lock(RepoLock)
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
				SendMessage(String.Format("REPO: {2} started: Cloning {0} branch of {1} ...", BranchName, RepoURL, Repository.IsValid(RepoPath) ? "Full reset" : "Setup"));
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

					lock (configLock)
					{
						Program.CopyDirectory(RepoConfig, StaticConfigDir);
					}
					Program.CopyDirectory(RepoData, StaticDataDir, null, true);
					File.Copy(RepoPath + LibMySQLFile, StaticDirs + LibMySQLFile, true);
					SendMessage("REPO: Clone complete!");
					TGServerService.WriteInfo("Repository {0}:{1} successfully cloned", TGServerService.EventID.RepoClone);
				}
				finally
				{
					currentProgress = -1;
				}
			}
			catch(Exception e)

			{
				SendMessage("REPO: Setup failed!");
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
		string GetShaOrBranch(out string error, bool branch)
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
		public string GetHead(out string error)
		{
			return GetShaOrBranch(out error, false);
		}

		//public api
		public string GetBranch(out string error)
		{
			return GetShaOrBranch(out error, true);
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
				SendMessage("REPO: Checking out object: " + sha);
				try
				{
					var Opts = new CheckoutOptions()
					{
						CheckoutModifiers = CheckoutModifiers.Force,
						OnCheckoutProgress = HandleCheckoutProgress,
					};
					Commands.Checkout(Repo, sha, Opts);
					var res = ResetNoLock(null);
					SendMessage("REPO: Checkout complete!");
					TGServerService.WriteInfo("Repo checked out " + sha, TGServerService.EventID.RepoCheckout);
					return res;
				}
				catch (Exception e)
				{
					SendMessage("REPO: Checkout failed!");
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
					SendMessage("REPO: Merge conflicted, aborted.");
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
				SendMessage(String.Format("REPO: Updating origin branch...({0})", reset ? "Hard Reset" : "Merge"));
				try
				{
					if (Repo.Head == null || !Repo.Head.IsTracking)
						return "Cannot update while not on a tracked branch";
					string logMessage = "";

					var R = Repo.Network.Remotes["origin"];
					IEnumerable<string> refSpecs = R.FetchRefSpecs.Select(X => X.Specification);
					var fos = new FetchOptions()
					{
						CredentialsProvider = GenerateGitCredentials,
					};
					fos.OnTransferProgress += HandleTransferProgress;
					Commands.Fetch(Repo, R.Name, refSpecs, fos, logMessage);

					var originBranch = Repo.Head.TrackedBranch;
					if (reset)
					{
						var error = ResetNoLock(Repo.Head.TrackedBranch);
						if (error != null)
							throw new Exception(error);
						DeletePRList();
						TGServerService.WriteInfo("Repo hard updated to " + originBranch.Tip.Sha, TGServerService.EventID.RepoHardUpdate);
						return error;
					}
					var res = MergeBranch(originBranch.FriendlyName);
					if (res != null)
						throw new Exception(res);
					TGServerService.WriteInfo("Repo merge updated to " + originBranch.Tip.Sha, TGServerService.EventID.RepoMergeUpdate);
					return null;
				}
				catch (Exception E)
				{
					SendMessage("REPO: Update failed!");
					TGServerService.WriteWarning(String.Format("Repo{0} update failed", reset ? " hard" : ""), reset ? TGServerService.EventID.RepoHardUpdateFail : TGServerService.EventID.RepoMergeUpdateFail);
					return E.ToString();
				}
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
				if (res == null)
				{
					SendMessage(String.Format("REPO: Hard reset to {0}branch", trackedBranch ? "tracked " : ""));
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
				SendMessage(String.Format("REPO: {2}erging PR #{0}...", PRNumber, impliedUpdate ? "Test m" : "M"));
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
					var fo = new FetchOptions() { OnTransferProgress = HandleTransferProgress, Prune = true };

					var branch = Repo.Branches[LocalBranchName];
					if(branch != null)
						//Need to delete the branch first in case of rebase
						Repo.Branches.Remove(branch);


					Commands.Fetch(Repo, "origin", Refspec, fo, logMessage);  //shitty api has no failure state for this

					currentProgress = -1;

					var Config = Properties.Settings.Default;


					branch = Repo.Branches[LocalBranchName];
					if (branch == null)
					{
						SendMessage("REPO: PR could not be fetched. Does it exist?");
						return String.Format("PR #{0} could not be fetched. Does it exist?", PRNumber);
					}

					//so we'll know if this fails
					var Result = MergeBranch(LocalBranchName);

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
						catch(Exception e)
						{
							TGServerService.WriteError("Failed to update PR list", TGServerService.EventID.RepoPRListError);
							return "PR Merged, JSON update failed: " + e.ToString();
						}
					}
					return Result;
				}
				catch (Exception E)
				{
					SendMessage("REPO: PR merge failed!");
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

		//public api
		string Commit()
		{
			lock (RepoLock)
			{
				var result = LoadRepo();
				if (result != null)
					return result;
				try
				{
					// Stage the file
					Commands.Stage(Repo, "html/changelog.html");
					Commands.Stage(Repo, "html/changelogs/*");

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
			const string ChangelogPy = RepoPath + "/tools/ss13_genchangelog.py";
			const string ChangelogHtml = RepoPath + "/html/changelog.html";
			const string ChangelogDir = RepoPath + "/html/changelogs";
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
				if (!File.Exists(ChangelogHtml))
				{
					error = "Missing changelog html!";
					return null;
				}
				if (!Directory.Exists(ChangelogDir))
				{
					error = "Missing auto changelog directory!";
					return null;
				}

				var Config = Properties.Settings.Default;

				var PythonFile = Config.PythonPath + "/python.exe";
				if (!File.Exists(PythonFile))
				{
					error = "Cannot locate python 2.7!";
					return null;
				}
				try
				{
					string result;
					int exitCode;
					using (var python = new Process())
					{
						python.StartInfo.FileName = PythonFile;
						python.StartInfo.Arguments = String.Format("{0} {1} {2}", ChangelogPy, ChangelogHtml, ChangelogDir);
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
						if (recurse)
						{
							error = "Script failed!";
							return result;
						}
						//update pip deps and try again

						string PipFile = Config.PythonPath + "/scripts/pip.exe";
						bool runningBSoup = false;
						while (true)
							using (var pip = new Process())
							{
								pip.StartInfo.FileName = PipFile;
								pip.StartInfo.Arguments = !runningBSoup ? "install PyYaml" : "install beautifulsoup4";
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

								if (runningBSoup)
									break;
								else
									runningBSoup = true;
							}
						//and recurse
						return GenerateChangelogImpl(out error, true);
					}
					error = null;
					TGServerService.WriteWarning("Changelog generated" + error, TGServerService.EventID.RepoChangelog);
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
