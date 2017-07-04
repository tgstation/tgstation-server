using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using TGServiceInterface;

namespace TGServerService
{
	partial class TGStationServer : ITGByond
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

		TGByondStatus updateStat = TGByondStatus.Idle;
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
			if (File.Exists(PrepPath(RevisionDownloadPath)))
				File.Delete(PrepPath(RevisionDownloadPath));
			Program.DeleteDirectory(PrepPath(StagingDirectory));
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
				case TGByondStatus.Starting:
				case TGByondStatus.Downloading:
				case TGByondStatus.Staging:
				case TGByondStatus.Updating:
					return true;
				case TGByondStatus.Idle:
				case TGByondStatus.Staged:
					return false;
			}
		}

		//public api
		public TGByondStatus CurrentStatus()
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
		public string GetVersion(TGByondVersion type)
		{
			try
			{
				lock (ByondLock)
				{
					if (type == TGByondVersion.Latest)
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
						string DirToUse = PrepPath(type == TGByondVersion.Staged ? StagingDirectoryInner : ByondDirectory);
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
				if (updateStat != TGByondStatus.Starting)
					return;
				updateStat = TGByondStatus.Downloading;
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
						client.DownloadFile(String.Format(ByondRevisionsURL, vi.major, vi.minor), PrepPath(RevisionDownloadPath));
					}
					catch
					{
						SendMessage("BYOND: Update download failed. Does the specified version exist?", ChatMessageType.DeveloperInfo);
						lastError = String.Format("Download of BYOND version {0}.{1} failed! Does it exist?", vi.major, vi.minor);
<<<<<<< HEAD
						TGServerService.WriteWarning(String.Format("Failed to update BYOND to version {0}.{1}!", vi.major, vi.minor), TGServerService.EventID.BYONDUpdateFail);
=======
						TGServerService.WriteWarning(lastError, TGServerService.EventID.BYONDUpdateFail, this);
>>>>>>> Instances
						lock (ByondLock)
						{
							updateStat = TGByondStatus.Idle;
						}
						return;
					}
				}
				lock (ByondLock)
				{
					updateStat = TGByondStatus.Staging;
				}

				//STAGING
				
				ZipFile.ExtractToDirectory(PrepPath(RevisionDownloadPath), PrepPath(StagingDirectory));
				lock (ByondLock)
				{
					File.WriteAllText(PrepPath(StagingDirectoryInner + VersionFile), String.Format("{0}.{1}", vi.major, vi.minor));
					//IMPORTANT: SET THE BYOND CONFIG TO NOT PROMPT FOR TRUSTED MODE REEE
					Directory.CreateDirectory(PrepPath(ByondConfigDir));
					File.WriteAllText(PrepPath(ByondConfigDir + ByondDDConfig), PrepPath(ByondNoPromptTrustedMode));
				}
				File.Delete(PrepPath(RevisionDownloadPath));

				lock (ByondLock)
				{
					updateStat = TGByondStatus.Staged;
				}

				switch (DaemonStatus())
				{
					case TGDreamDaemonStatus.Offline:
						if(ApplyStagedUpdate())
							lastError = null;
						else
							lastError = "Failed to apply update!";
						break;
					default:
						RequestRestart();
						lastError = "Update staged. Awaiting server restart...";
<<<<<<< HEAD
						SendMessage(String.Format("BYOND: Staging complete. Awaiting server restart...", vi.major, vi.minor), ChatMessageType.DeveloperInfo);
						TGServerService.WriteInfo(String.Format("BYOND update {0}.{1} staged", vi.major, vi.minor), TGServerService.EventID.BYONDUpdateStaged);
=======
						SendMessage(String.Format("BYOND: Staging complete. Awaiting server restart...", vi.major, vi.minor));
						TGServerService.WriteInfo(String.Format("BYOND update {0}.{1} staged", vi.major, vi.minor), TGServerService.EventID.BYONDUpdateStaged, this);
>>>>>>> Instances
						break;
				}
			}
			catch (ThreadAbortException)
			{
				return;
			}
			catch (Exception e)
			{
<<<<<<< HEAD
				TGServerService.WriteError("Revision staging errror: " + e.ToString(), TGServerService.EventID.BYONDUpdateFail);
=======
				TGServerService.WriteError("Revision staging errror: " + e.ToString(), TGServerService.EventID.BYONDUpdateFail, this);
>>>>>>> Instances
				lock (ByondLock)
				{
					updateStat = TGByondStatus.Idle;
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
					updateStat = TGByondStatus.Starting;
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
				if (compilerCurrentStatus == TGCompilerStatus.Compiling)
					return false;
				lock (ByondLock)
				{
					if (updateStat != TGByondStatus.Staged)
						return false;
					updateStat = TGByondStatus.Updating;
				}
				try
				{
					Program.DeleteDirectory(PrepPath(ByondDirectory));
					Directory.Move(PrepPath(StagingDirectoryInner), PrepPath(ByondDirectory));
					Program.DeleteDirectory(PrepPath(StagingDirectory));
					lastError = null;
<<<<<<< HEAD
					SendMessage("BYOND: Update completed!", ChatMessageType.DeveloperInfo);
					TGServerService.WriteInfo(String.Format("BYOND update {0} completed!", GetVersion(TGByondVersion.Installed)), TGServerService.EventID.BYONDUpdateComplete);
=======
					SendMessage("BYOND: Update completed!");
					TGServerService.WriteInfo(String.Format("BYOND update {0} completed!", GetVersion(TGByondVersion.Installed)), TGServerService.EventID.BYONDUpdateComplete, this);
>>>>>>> Instances
					return true;
				}
				catch (Exception e)
				{
					lastError = e.ToString();
<<<<<<< HEAD
					SendMessage("BYOND: Update failed!", ChatMessageType.DeveloperInfo);
					TGServerService.WriteError("BYOND update failed!", TGServerService.EventID.BYONDUpdateFail);
=======
					SendMessage("BYOND: Update failed!");
					TGServerService.WriteError("BYOND update failed!", TGServerService.EventID.BYONDUpdateFail, this);
>>>>>>> Instances
					return false;
				}
				finally
				{
					lock(ByondLock) {
						updateStat = TGByondStatus.Idle;
					}
				}
			}
		}
	}
}
