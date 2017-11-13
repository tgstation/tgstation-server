using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Server.Service
{
	sealed partial class ServerInstance : ITGByond
	{
		/// <summary>
		/// The instance directory to store the BYOND installation
		/// </summary>
		const string ByondDirectory = "BYOND";
		/// <summary>
		/// The instance directory to use when updating the BYOND installation
		/// </summary>
		const string StagingDirectory = "BYOND_staged";
		/// <summary>
		/// Path to the actual BYOND installation within the <see cref="StagingDirectory"/>
		/// </summary>
		const string StagingDirectoryInner = StagingDirectory + "/byond";
		/// <summary>
		/// The path in the instance directory to store the downloaded BYOND revision
		/// </summary>
		const string RevisionDownloadPath = "BYONDRevision.zip";
		/// <summary>
		/// The location of the BYOND version data of an installation
		/// </summary>
		const string VersionFile = "/byond_version.dat";
		/// <summary>
		/// The URL format string for getting BYOND version {0}.{1} zipfile
		/// </summary>
		const string ByondRevisionsURL = "https://secure.byond.com/download/build/{0}/{0}.{1}_byond.zip";
		/// <summary>
		/// The URL for getting the latest BYOND version zipfile
		/// </summary>
		const string ByondLatestURL = "https://secure.byond.com/download/build/LATEST/";
		/// <summary>
		/// The instance directory to modify the BYOND cfg before installation
		/// </summary>
		const string ByondConfigDir = StagingDirectory + "/BYOND/cfg";
		/// <summary>
		/// BYOND's DreamDaemon config file in the cfg modification directory
		/// </summary>
		const string ByondDDConfig = ByondConfigDir + "/daemon.txt";
		/// <summary>
		/// Setting to add to <see cref="ByondDDConfig"/> to suppress an invisible user prompt for running a trusted mode .dmb
		/// </summary>
		const string ByondNoPromptTrustedMode = "trusted-check 0";

		/// <summary>
		/// The status of the BYOND updater
		/// </summary>
		ByondStatus updateStat = ByondStatus.Idle;

		/// <summary>
		/// Used for multithreading safety
		/// </summary>
		object ByondLock = new object();
		/// <summary>
		/// The last error the BYOND updater encountered
		/// </summary>
		string lastError;
		/// <summary>
		/// Thread used for staging BYOND revisions
		/// </summary>
		Thread RevisionStaging;

		/// <summary>
		/// Called when the <see cref="ServerInstance"/> is setup. Prepares the BYOND updater
		/// </summary>
		void InitByond()
		{
			CleanByondStaging();
		}

		/// <summary>
		/// Cleans the BYOND staging directory
		/// </summary>
		void CleanByondStaging()
		{
			var rrdp = RelativePath(RevisionDownloadPath);
			//linger not
			if (File.Exists(rrdp))
				File.Delete(rrdp);
			Program.DeleteDirectory(RelativePath(StagingDirectory));
		}

		/// <summary>
		/// Called when the <see cref="ServerInstance"/> is shutdown
		/// </summary>
		void DisposeByond()
		{
			lock (ByondLock)
			{
				if (RevisionStaging != null)
					RevisionStaging.Abort();
				CleanByondStaging();
			}
		}

		/// <summary>
		/// Checks if the updater is considered busy
		/// </summary>
		/// <returns><see langword="true"/> if the updater is considered busy, <see langword="false"/> otherwise</returns>
		bool BusyCheck()
		{
			lock (ByondLock)
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

		/// <inheritdoc />
		public ByondStatus CurrentStatus()
		{
			lock (ByondLock)
			{
				return updateStat;
			}
		}

		/// <inheritdoc />
		public string GetError()
		{
			lock (ByondLock)
			{
				var error = lastError;
				lastError = null;
				return error;
			}
		}

		/// <inheritdoc />
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
						string DirToUse = RelativePath(type == ByondVersion.Staged ? StagingDirectoryInner : ByondDirectory);
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
		
		/// <summary>
		/// Downloads and unzips a BYOND revision. Calls <see cref="ApplyStagedUpdate"/> afterwards if the <see cref="ServerInstance"/> isn't running, otherwise, calls <see cref="RequestRestart"/>. Sets <see cref="lastError"/> on failure
		/// </summary>
		/// <param name="param">Stringified BYOND revision</param>
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

				var vi = ((string)param).Split('.');
				var major = Convert.ToInt32(vi[0]);
				var minor = Convert.ToInt32(vi[1]);
				var rrdp = RelativePath(RevisionDownloadPath);
				using (var client = new WebClient())
				{
					SendMessage(String.Format("BYOND: Updating to version {0}.{1}...", major, minor), MessageType.DeveloperInfo);

					//DOWNLOADING

					try
					{
						client.DownloadFile(String.Format(ByondRevisionsURL, major, minor), rrdp);
					}
					catch
					{
						SendMessage("BYOND: Update download failed. Does the specified version exist?", MessageType.DeveloperInfo);
						lastError = String.Format("Download of BYOND version {0}.{1} failed! Does it exist?", major, minor);
						WriteWarning(String.Format("Failed to update BYOND to version {0}.{1}!", major, minor), EventID.BYONDUpdateFail);
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
				
				ZipFile.ExtractToDirectory(rrdp, RelativePath(StagingDirectory));
				lock (ByondLock)
				{
					File.WriteAllText(RelativePath(StagingDirectoryInner + VersionFile), String.Format("{0}.{1}", major, minor));
					//IMPORTANT: SET THE BYOND CONFIG TO NOT PROMPT FOR TRUSTED MODE REEE
					Directory.CreateDirectory(RelativePath(ByondConfigDir));
					File.WriteAllText(RelativePath(ByondDDConfig), ByondNoPromptTrustedMode);
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
						SendMessage(String.Format("BYOND: Staging complete. Awaiting server restart...", major, minor), MessageType.DeveloperInfo);
						WriteInfo(String.Format("BYOND update {0}.{1} staged", major, minor), EventID.BYONDUpdateStaged);
						break;
				}
			}
			catch (ThreadAbortException)
			{
				return;
			}
			catch (Exception e)
			{
				WriteError("Revision staging errror: " + e.ToString(), EventID.BYONDUpdateFail);
				lock (ByondLock)
				{
					updateStat = ByondStatus.Idle;
					lastError = e.ToString();
					RevisionStaging = null;
				}
			}
		}
		/// <inheritdoc />
		public bool UpdateToVersion(int major, int minor)
		{
			lock (ByondLock)
			{
				if (!BusyCheck())
				{
					updateStat = ByondStatus.Starting;
					RevisionStaging = new Thread(new ParameterizedThreadStart(UpdateToVersionImpl))
					{
						IsBackground = true //don't slow me down
					};
					RevisionStaging.Start(String.Format("{0}.{1}", major, minor));
					return true;
				}
				return false; 
			}
		}

		/// <summary>
		/// Attempts to move the staged update from <see cref="StagingDirectory"/> to <see cref="ByondDirectory"/>. Sets <see cref="lastError"/> on failure
		/// </summary>
		/// <returns><see langword="true"/> on success, <see langword="false"/> on failure</returns>
		bool ApplyStagedUpdate()
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
					var rbd = RelativePath(ByondDirectory);
					Program.DeleteDirectory(rbd);
					Directory.Move(RelativePath(StagingDirectoryInner), rbd);
					Program.DeleteDirectory(RelativePath(StagingDirectory));
					lastError = null;
					SendMessage("BYOND: Update completed!", MessageType.DeveloperInfo);
					WriteInfo(String.Format("BYOND update {0} completed!", GetVersion(ByondVersion.Installed)), EventID.BYONDUpdateComplete);
					return true;
				}
				catch (Exception e)
				{
					lastError = e.ToString();
					SendMessage("BYOND: Update failed!", MessageType.DeveloperInfo);
					WriteError("BYOND update failed! Error: " + e.ToString(), EventID.BYONDUpdateFail);
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
