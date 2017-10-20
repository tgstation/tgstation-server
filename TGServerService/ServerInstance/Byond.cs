using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGServerService
{
	partial class ServerInstance : ITGByond
	{
		const string ByondDirectory = "BYOND";
		const string StagingDirectory = "BYOND_staged";
		const string StagingDirectoryInner = "BYOND_staged/byond";
		const string RevisionDownloadPath = "BYONDRevision.zip";
		const string VersionFile = "/byond_version.dat";
		const string ByondRevisionsURL = "https://secure.byond.com/download/build/{0}/{0}.{1}_byond.zip";
		const string ByondLatestURL = "https://secure.byond.com/download/build/LATEST/";

		const string ByondConfigDir = "BYOND_staged/BYOND/cfg";
		const string ByondDDConfig = "/daemon.txt";
		const string ByondNoPromptTrustedMode = "trusted-check 0";

		ByondStatus updateStat = ByondStatus.Idle;
		object ByondLock = new object();
		string lastError;

		Thread RevisionStaging;

		//Just cleanup
		void InitByond()
		{
			CleanByondStaging();
		}

		void CleanByondStaging()
		{
			//linger not
			if (File.Exists(RevisionDownloadPath))
				File.Delete(RevisionDownloadPath);
			Program.DeleteDirectory(StagingDirectory);
		}

		//Kill the thread and cleanup again
		void DisposeByond()
		{
			lock (ByondLock)
			{
				if (RevisionStaging != null)
					RevisionStaging.Abort();
				CleanByondStaging();
			}
		}

		//requires ByondLock to be locked
		bool BusyCheckNoLock()
		{
			switch (updateStat)
			{
				default:
				case ByondStatus.Starting:
				case ByondStatus.Downloading:
				case ByondStatus.Staging:
				case ByondStatus.Updating:
					return true;
				case ByondStatus.Idle:
				case ByondStatus.Staged:
					return false;
			}
		}

		//public api
		public ByondStatus CurrentStatus()
		{
			lock (ByondLock)
			{
				return updateStat;
			}
		}

		//public api
		public string GetError()
		{
			lock (ByondLock)
			{
				var error = lastError;
				lastError = null;
				return error;
			}
		}

		//public api
		public string GetVersion(ByondVersion type)
		{
			try
			{
				lock (ByondLock)
				{
					if (type == ByondVersion.Latest)
					{
						//get the latest version from the website
						HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ByondLatestURL);
						var results = new List<string>();
						using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
						{
							using (StreamReader reader = new StreamReader(response.GetResponseStream()))
							{
								string html = reader.ReadToEnd();

								Regex regex = new Regex("\\\"([^\"]*)\\\"");
								MatchCollection matches = regex.Matches(html);
								foreach (Match match in matches)
									if (match.Success && match.Value.Contains("_byond.exe"))
										results.Add(match.Value.Replace("\"", "").Replace("_byond.exe", ""));
							}
						}
						results.Sort();
						results.Reverse();
						return results.Count > 0 ? results[0] : null;
					}
					else
					{
						string DirToUse = type == ByondVersion.Staged ? StagingDirectoryInner : ByondDirectory;
						if (Directory.Exists(DirToUse))
						{
							string file = DirToUse + VersionFile;
							if (File.Exists(file))
								return File.ReadAllText(file);
						}
					}
					return null;
				}
			}
			catch (Exception e)
			{
				return "Error: " + e.ToString();
			}
		}

		//literally just for passing 2 ints to the thread function
		class VersionInfo
		{
			public int major, minor;
		}

		//does the downloading and unzipping
		//calls ApplyStagedUpdate() after if the server isn't running
		public void UpdateToVersionImpl(object param)
		{
			lock (ByondLock) { 
				if (updateStat != ByondStatus.Starting)
					return;
				updateStat = ByondStatus.Downloading;
			}

			try
			{
				CleanByondStaging();

				var vi = (VersionInfo)param;
				using (var client = new WebClient())
				{
					SendMessage(String.Format("BYOND: Updating to version {0}.{1}...", vi.major, vi.minor), ChatMessageType.DeveloperInfo);

					//DOWNLOADING

					try
					{
						client.DownloadFile(String.Format(ByondRevisionsURL, vi.major, vi.minor), RevisionDownloadPath);
					}
					catch
					{
						SendMessage("BYOND: Update download failed. Does the specified version exist?", ChatMessageType.DeveloperInfo);
						lastError = String.Format("Download of BYOND version {0}.{1} failed! Does it exist?", vi.major, vi.minor);
						Service.WriteWarning(String.Format("Failed to update BYOND to version {0}.{1}!", vi.major, vi.minor), EventID.BYONDUpdateFail);
						lock (ByondLock)
						{
							updateStat = ByondStatus.Idle;
						}
						return;
					}
				}
				lock (ByondLock)
				{
					updateStat = ByondStatus.Staging;
				}

				//STAGING
				
				ZipFile.ExtractToDirectory(RevisionDownloadPath, StagingDirectory);
				lock (ByondLock)
				{
					File.WriteAllText(StagingDirectoryInner + VersionFile, String.Format("{0}.{1}", vi.major, vi.minor));
					//IMPORTANT: SET THE BYOND CONFIG TO NOT PROMPT FOR TRUSTED MODE REEE
					Directory.CreateDirectory(ByondConfigDir);
					File.WriteAllText(ByondConfigDir + ByondDDConfig, ByondNoPromptTrustedMode);
				}
				File.Delete(RevisionDownloadPath);

				lock (ByondLock)
				{
					updateStat = ByondStatus.Staged;
				}

				switch (DaemonStatus())
				{
					case DreamDaemonStatus.Offline:
						if(ApplyStagedUpdate())
							lastError = null;
						else
							lastError = "Failed to apply update!";
						break;
					default:
						RequestRestart();
						lastError = "Update staged. Awaiting server restart...";
						SendMessage(String.Format("BYOND: Staging complete. Awaiting server restart...", vi.major, vi.minor), ChatMessageType.DeveloperInfo);
						Service.WriteInfo(String.Format("BYOND update {0}.{1} staged", vi.major, vi.minor), EventID.BYONDUpdateStaged);
						break;
				}
			}
			catch (ThreadAbortException)
			{
				return;
			}
			catch (Exception e)
			{
				Service.WriteError("Revision staging errror: " + e.ToString(), EventID.BYONDUpdateFail);
				lock (ByondLock)
				{
					updateStat = ByondStatus.Idle;
					lastError = e.ToString();
					RevisionStaging = null;
				}
			}
		}
		//public api for kicking off the update thread
		public bool UpdateToVersion(int ma, int mi)
		{
			lock (ByondLock)
			{
				if (!BusyCheckNoLock())
				{
					updateStat = ByondStatus.Starting;
					RevisionStaging = new Thread(new ParameterizedThreadStart(UpdateToVersionImpl))
					{
						IsBackground = true //don't slow me down
					};
					RevisionStaging.Start(new VersionInfo { major = ma, minor = mi });
					return true;
				}
				return false; 
			}
		}
		//tries to apply the staged update
		public bool ApplyStagedUpdate()
		{
			lock (CompilerLock)
			{
				if (compilerCurrentStatus == CompilerStatus.Compiling)
					return false;
				lock (ByondLock)
				{
					if (updateStat != ByondStatus.Staged)
						return false;
					updateStat = ByondStatus.Updating;
				}
				try
				{
					Program.DeleteDirectory(ByondDirectory);
					Directory.Move(StagingDirectoryInner, ByondDirectory);
					Program.DeleteDirectory(StagingDirectory);
					lastError = null;
					SendMessage("BYOND: Update completed!", ChatMessageType.DeveloperInfo);
					Service.WriteInfo(String.Format("BYOND update {0} completed!", GetVersion(ByondVersion.Installed)), EventID.BYONDUpdateComplete);
					return true;
				}
				catch (Exception e)
				{
					lastError = e.ToString();
					SendMessage("BYOND: Update failed!", ChatMessageType.DeveloperInfo);
					Service.WriteError("BYOND update failed!", EventID.BYONDUpdateFail);
					return false;
				}
				finally
				{
					lock(ByondLock) {
						updateStat = ByondStatus.Idle;
					}
				}
			}
		}
	}
}
