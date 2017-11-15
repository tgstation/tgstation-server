using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	sealed class ByondManager : IByondManager, IDisposable
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
		/// Setting to add to <see cref="ByondDDConfig"/> to suppress an invisible user prompt for running a trusted mode .dmb
		/// </summary>
		const string ByondNoPromptTrustedMode = "trusted-check 0";
		/// <summary>
		/// The executable for DreamDaemon
		/// </summary>
		const string DreamDaemonExecutable = "dreamdaemon.exe";
		/// <summary>
		/// The executable for DM
		/// </summary>
		const string DMExecutable = "dm.exe";
		/// <summary>
		/// The bin folder in a BYOND installation
		/// </summary>
		const string BinDirectory = "bin";

		/// <summary>
		/// Path to the actual BYOND installation within the <see cref="StagingDirectory"/>
		/// </summary>
		static readonly string StagingDirectoryInner = Path.Combine(StagingDirectory, "byond");
		/// <summary>
		/// The instance directory to modify the BYOND cfg before installation
		/// </summary>
		static readonly string ByondConfigDir = Path.Combine(StagingDirectoryInner, "cfg");
		/// <summary>
		/// BYOND's DreamDaemon config file in the cfg modification directory
		/// </summary>
		static readonly string ByondDDConfig = Path.Combine(ByondConfigDir, "daemon.txt");
		/// <summary>
		/// The path to <see cref="DreamDaemonExecutable"/> in a BYOND installation
		/// </summary>
		static readonly string DreamDaemonPath = Path.Combine(ByondDirectory, BinDirectory, DreamDaemonExecutable);
		/// <summary>
		/// The path to <see cref="DMExecutable"/> in a BYOND installation
		/// </summary>
		static readonly string DMPath = Path.Combine(BinDirectory, DMExecutable);

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
		/// The number of times <see cref="LockDMExecutable(bool, out string)"/> has been successfully called.
		/// </summary>
		ulong DMLockCount;

		/// <summary>
		/// The number of times <see cref="LockDDExecutable(out string)"/> has been successfully called.
		/// </summary>
		ulong DDLockCount;

		/// <summary>
		/// The last error the BYOND updater encountered
		/// </summary>
		string lastError;

		/// <summary>
		/// Thread used for staging BYOND revisions
		/// </summary>
		CancellationTokenSource RevisionStagingCanceller;

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
			if (RevisionStagingCanceller != null)
			{
				RevisionStagingCanceller.Cancel();
				RevisionStagingCanceller = null;
			}
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
		/// <param name="major">The major BYOND version to update to</param>
		/// <param name="minor">The minor BYOND version to update to</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for cancelling this function</param>
		void UpdateToVersionImpl(int major, int minor, CancellationToken cancellationToken)
		{
			lock (this)
			{
				if (updateStat != ByondStatus.Starting)
					return;
				updateStat = ByondStatus.Downloading;
			}

			try
			{
				if (cancellationToken.IsCancellationRequested)
					return;

				CleanStaging();

				if (cancellationToken.IsCancellationRequested)
					return;

				ChatBroadcaster.SendMessage(String.Format("BYOND: Updating to version {0}.{1}...", major, minor), MessageType.DeveloperInfo);

				//DOWNLOADING

				try
				{
					Exception failed = null;
					using (var client = new WebClient())
					using (var waitHandle = new ManualResetEvent(false))
					{
						client.DownloadFileCompleted += (a, b) =>
						{
							failed = b.Error;
							waitHandle.Set();
						};
						client.DownloadFileAsync(new Uri(String.Format(ByondRevisionsURL, major, minor)), RevisionDownloadPath);
						WaitHandle.WaitAny(new WaitHandle[] { waitHandle, cancellationToken.WaitHandle });
						if (cancellationToken.IsCancellationRequested)
						{
							client.CancelAsync();
							return;
						}
					}
					if (failed != null)
						throw failed;
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

				if (DreamDaemon.DaemonStatus() == DreamDaemonStatus.Offline)
				{
					var res = ApplyStagedUpdate();
					lock (this)
					{
						if (res)
							lastError = null;
						else
							lastError = "Failed to apply update!";
					}
				}
				else
				{
					DreamDaemon.RequestRestart();
					lastError = "Update staged. Awaiting server restart...";
					ChatBroadcaster.SendMessage(String.Format("BYOND: Staging complete. Awaiting server restart...", major, minor), MessageType.DeveloperInfo);
					Logger.WriteInfo(String.Format("BYOND update {0}.{1} staged", major, minor), EventID.BYONDUpdateStaged);
				}
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
					RevisionStagingCanceller = null;
			}
		}

		/// <inheritdoc />
		public bool ApplyStagedUpdate()
		{
			lock (this)
			{
				if (updateStat != ByondStatus.Staged || DMLockCount > 0 || DDLockCount > 0)
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
					RevisionStagingCanceller = new CancellationTokenSource();
					Task.Factory.StartNew(() => UpdateToVersionImpl(major, minor, RevisionStagingCanceller.Token));
					return true;
				}
			return false;

		}
		
		/// <summary>
		/// Tries to decrement <paramref name="theLock"/>. Calls <see cref="ApplyStagedUpdate"/> if it reaches zero
		/// </summary>
		/// <param name="theLock">The lock counter to decrement</param>
		/// <param name="exceptionFileName">The filename to in the posible <see cref="InvalidOperationException"/></param>
		void CheckUnlock(ref ulong theLock, string exceptionFileName)
		{
			lock (this)
				switch (theLock)
				{
					case 0:
						throw new InvalidOperationException(String.Format("{0} is already fully unlocked!", exceptionFileName));
					case 1:
						theLock = 0;
						Task.Factory.StartNew(ApplyStagedUpdate);	//async so we can leave this lock
						break;
					default:
						--theLock;
						break;
				}
		}

		/// <inheritdoc />
		public string LockDMExecutable(bool useStagedIfPossible, out string error)
		{
			lock (this)
			{
				if (BusyCheck())
				{
					error = "Error, update operation in progress!";
					return null;
				}
				//have to use the staged one if nothing is installed
				useStagedIfPossible |= GetVersion(ByondVersion.Installed) == null;
				var pathToUse = updateStat == ByondStatus.Staged && useStagedIfPossible ? StagingDirectoryInner : ByondDirectory;
				++DMLockCount;
				error = null;
				return IOManager.ResolvePath(Path.Combine(pathToUse, DMExecutable));
			}
		}

		/// <inheritdoc />
		public string LockDDExecutable(out string error)
		{
			lock (this)
			{
				if (updateStat == ByondStatus.Updating)
				{
					error = "Error, update operation in progress!";
					return null;
				}
				if(GetVersion(ByondVersion.Installed) == null)
				{
					error = String.Format("Error, {0} not installed!", DreamDaemonExecutable);
					return null;
				}
				++DDLockCount;
				error = null;
				return IOManager.ResolvePath(DreamDaemonPath);
			}
		}

		/// <inheritdoc />
		public void UnlockDMExecutable()
		{
			CheckUnlock(ref DMLockCount, DMExecutable);
		}

		/// <inheritdoc />
		public void UnlockDDExecutable()
		{
			CheckUnlock(ref DMLockCount, DreamDaemonExecutable);
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
