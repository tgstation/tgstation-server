using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	sealed class ByondManager : ITGByond, IDisposable
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
		const string VersionFile = "byond_version.dat";
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
		/// The <see cref="IInstanceLogger"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IIOManager IOManager;

		/// <summary>
		/// The <see cref="IChatBroadcaster"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IChatBroadcaster ChatBroadcaster;

		/// <summary>
		/// The <see cref="ITGDreamDaemon"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly ITGDreamDaemon DreamDaemon;

		/// <summary>
		/// The status of the BYOND updater
		/// </summary>
		ByondStatus updateStat = ByondStatus.Idle;

		/// <summary>
		/// The last error the BYOND updater encountered
		/// </summary>
		string lastError;

		/// <summary>
		/// Thread used for staging BYOND revisions
		/// </summary>
		Thread RevisionStaging;

		/// <summary>
		/// Construct a <see cref="ByondManager"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="ioManager">The value of <see cref="IOManager"/></param>
		/// <param name="chatBroadcaster">The value of <see cref="ChatBroadcaster"/></param>
		/// <param name="dreamDaemon">The value of <see cref="DreamDaemon"/></param>
		public ByondManager(IInstanceLogger logger, IIOManager ioManager, IChatBroadcaster chatBroadcaster, ITGDreamDaemon dreamDaemon)
		{
			Logger = logger;
			IOManager = ioManager;
			ChatBroadcaster = chatBroadcaster;
			DreamDaemon = dreamDaemon;
			CleanStaging();
		}

		/// <summary>
		/// Cleans the <see cref="StagingDirectory"/> and <see cref="RevisionDownloadPath"/>
		/// </summary>
		void CleanStaging()
		{
			//linger not
			IOManager.DeleteFile(RevisionDownloadPath);
			IOManager.DeleteDirectory(StagingDirectory);
		}

		/// <summary>
		/// Cleans up the <see cref="ByondManager"/>
		/// </summary>
		void Cleanup()
		{
			if (RevisionStaging != null)
				RevisionStaging.Abort();
			CleanStaging();
		}
		
		/// <summary>
		/// Checks if the updater is considered busy
		/// </summary>
		/// <returns><see langword="true"/> if the updater is considered busy, <see langword="false"/> otherwise</returns>
		bool BusyCheck()
		{
			lock (this)
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

		public ByondStatus CurrentStatus()
		{
			return updateStat;
		}

		/// <inheritdoc />
		public string GetError()
		{
			lock(this)
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
				lock (this)
				{
					if (type == ByondVersion.Latest)
					{
						//get the latest version from the website
						var request = (HttpWebRequest)WebRequest.Create(ByondLatestURL);
						var results = new List<string>();
						using (var response = (HttpWebResponse)request.GetResponse())
							using (var reader = new StreamReader(response.GetResponseStream()))
							{
								string html = reader.ReadToEnd();

								Regex regex = new Regex("\\\"([^\"]*)\\\"");
								MatchCollection matches = regex.Matches(html);
								foreach (Match match in matches)
									if (match.Success && match.Value.Contains("_byond.exe"))
										results.Add(match.Value.Replace("\"", "").Replace("_byond.exe", ""));
							}

						results.Sort();
						results.Reverse();
						return results.Count > 0 ? results[0] : null;
					}
					else
					{
						var DirToUse = type == ByondVersion.Staged ? StagingDirectoryInner : ByondDirectory;
						if (IOManager.DirectoryExists(DirToUse))
						{
							var file = Path.Combine(DirToUse, VersionFile);
							lock (this)
								if (IOManager.FileExists(file))
									return IOManager.ReadAllText(file);
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
		/// Downloads and unzips a BYOND revision. Calls <see cref="ApplyStagedUpdate"/> afterwards if the <see cref="Instance"/> isn't running, otherwise, calls <see cref="ITGDreamDaemon.RequestRestart"/>. Sets <see cref="lastError"/> on failure
		/// </summary>
		/// <param name="param">Stringified BYOND revision</param>
		void UpdateToVersionImpl(object param)
		{
			lock (this)
			{
				if (updateStat != ByondStatus.Starting)
					return;
				updateStat = ByondStatus.Downloading;
			}

			try
			{
				CleanStaging();

				var vi = ((string)param).Split('.');
				var major = Convert.ToInt32(vi[0]);
				var minor = Convert.ToInt32(vi[1]);
				using (var client = new WebClient())
				{
					ChatBroadcaster.SendMessage(String.Format("BYOND: Updating to version {0}.{1}...", major, minor), MessageType.DeveloperInfo);

					//DOWNLOADING

					try
					{
						client.DownloadFile(String.Format(ByondRevisionsURL, major, minor), RevisionDownloadPath);
					}
					catch
					{
						ChatBroadcaster.SendMessage("BYOND: Update download failed. Does the specified version exist?", MessageType.DeveloperInfo);
						Logger.WriteWarning(String.Format("Failed to update BYOND to version {0}.{1}!", major, minor), EventID.BYONDUpdateFail);
						lock (this)
						{
							lastError = String.Format("Download of BYOND version {0}.{1} failed! Does it exist?", major, minor);
							updateStat = ByondStatus.Idle;
						}
						return;
					}
				}
				lock (this)
					updateStat = ByondStatus.Staging;

				//STAGING

				ZipFile.ExtractToDirectory(IOManager.ResolvePath(RevisionDownloadPath), IOManager.ResolvePath(StagingDirectory));
				lock (this)
					IOManager.WriteAllText(Path.Combine(StagingDirectoryInner, VersionFile), String.Format("{0}.{1}", major, minor));
				
				//IMPORTANT: SET THE BYOND CONFIG TO NOT PROMPT FOR TRUSTED MODE REEE
				IOManager.CreateDirectory(ByondConfigDir);
					IOManager.WriteAllText(ByondDDConfig, ByondNoPromptTrustedMode);

				IOManager.DeleteFile(RevisionDownloadPath);

				lock (this)
					updateStat = ByondStatus.Staged;

				switch (DreamDaemon.DaemonStatus())
				{
					case DreamDaemonStatus.Offline:
						if (ApplyStagedUpdate())
							lastError = null;
						else
							lastError = "Failed to apply update!";
						break;
					default:
						DreamDaemon.RequestRestart();
						lastError = "Update staged. Awaiting server restart...";
						ChatBroadcaster.SendMessage(String.Format("BYOND: Staging complete. Awaiting server restart...", major, minor), MessageType.DeveloperInfo);
						Logger.WriteInfo(String.Format("BYOND update {0}.{1} staged", major, minor), EventID.BYONDUpdateStaged);
						break;
				}
			}
			catch (ThreadAbortException)
			{
				return;
			}
			catch (Exception e)
			{
				Logger.WriteError("Revision staging errror: " + e.ToString(), EventID.BYONDUpdateFail);
				lock (this)
				{
					lastError = e.ToString();
					updateStat = ByondStatus.Idle;
				}
			}
			finally
			{
				lock (this)
					RevisionStaging = null;
			}
		}

		/// <summary>
		/// Attempts to move the staged update from <see cref="StagingDirectory"/> to <see cref="ByondDirectory"/>. Sets <see cref="lastError"/> on failure
		/// </summary>
		/// <returns><see langword="true"/> on success, <see langword="false"/> on failure</returns>
		bool ApplyStagedUpdate()
		{
			lock (this)
			{
				if (updateStat != ByondStatus.Staged)
					return false;
				updateStat = ByondStatus.Updating;
			}
			try
			{
				IOManager.DeleteDirectory(ByondDirectory);
				IOManager.MoveDirectory(StagingDirectoryInner, ByondDirectory);
				IOManager.DeleteDirectory(StagingDirectory);
				ChatBroadcaster.SendMessage("BYOND: Update completed!", MessageType.DeveloperInfo);
				Logger.WriteInfo(String.Format("BYOND update {0} completed!", GetVersion(ByondVersion.Installed)), EventID.BYONDUpdateComplete);
				lock (this)
					lastError = null;
				return true;
			}
			catch (Exception e)
			{
				ChatBroadcaster.SendMessage("BYOND: Update failed!", MessageType.DeveloperInfo);
				Logger.WriteError("BYOND update failed! Error: " + e.ToString(), EventID.BYONDUpdateFail);
				lock (this)
					lastError = e.ToString();
				return false;
			}
			finally
			{
				lock (this)
					updateStat = ByondStatus.Idle;
			}
		}

		/// <inheritdoc />
		public bool UpdateToVersion(int major, int minor)
		{
			lock (this)
				if (!BusyCheck())
				{
					updateStat = ByondStatus.Starting;
					RevisionStaging = new Thread(new ParameterizedThreadStart(UpdateToVersionImpl));
					RevisionStaging.Start(String.Format("{0}.{1}", major, minor));
					return true;
				}
			return false;

		}

		#region IDisposable Support
		/// <summary>
		/// To detect redundant <see cref="Dispose()"/> calls
		/// </summary>
		private bool disposedValue = false;

		/// <summary>
		/// Implements the <see cref="IDisposable"/> pattern. Calls <see cref="Cleanup"/>
		/// </summary>
		/// <param name="disposing"><see langword="true"/> if <see cref="Dispose()"/> was called manually, <see langword="false"/> if it was from the finalizer</param>
		void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Cleanup();
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~TGDiscordChatProvider() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }
		/// <summary>
		/// Implements the <see cref="IDisposable"/> pattern
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
