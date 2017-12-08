using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;
using TGS.Server.IO;
using TGS.Server.Logging;

namespace TGS.Server.Components
{
	/// <inheritdoc />
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
		/// The message shown when an operation fails due to a BYOND update being in progress
		/// </summary>
		const string ErrorUpdateInProgress = "Error, BYOND update operation in progress!";

		/// <summary>
		/// Path to the actual BYOND installation within the <see cref="StagingDirectory"/>
		/// </summary>
		static readonly string StagingDirectoryInner = IOManager.ConcatPath(StagingDirectory, "byond");
		/// <summary>
		/// The instance directory to modify the BYOND cfg before installation
		/// </summary>
		static readonly string ByondConfigDir = IOManager.ConcatPath(StagingDirectoryInner, "cfg");
		/// <summary>
		/// BYOND's DreamDaemon config file in the cfg modification directory
		/// </summary>
		static readonly string ByondDDConfig = IOManager.ConcatPath(ByondConfigDir, "daemon.txt");
		/// <summary>
		/// The path to <see cref="DreamDaemonExecutable"/> in a BYOND installation
		/// </summary>
		static readonly string DreamDaemonPath = IOManager.ConcatPath(ByondDirectory, BinDirectory, DreamDaemonExecutable);
		/// <summary>
		/// The path to <see cref="DMExecutable"/> in a BYOND installation
		/// </summary>
		static readonly string DMPath = IOManager.ConcatPath(BinDirectory, DMExecutable);
		/// <summary>
		/// The path to the BYOND cache
		/// </summary>
		static readonly string ByondCacheDirectory = IOManager.ConcatPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ByondDirectory, "cache");

		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IIOManager IO;
		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IChatManager Chat;
		/// <summary>
		/// The <see cref="IInteropManager"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IInteropManager Interop;

		/// <summary>
		/// <see cref="Task"/> used for updating revision
		/// </summary>
		Task updateTask;
		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="updateTask"/>
		/// </summary>
		CancellationTokenSource updateCancellationTokenSource;

		/// <summary>
		/// The status of the BYOND updater
		/// </summary>
		ByondStatus updateStat;
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
		/// Construct a <see cref="ByondManager"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="ioManager">The value of <see cref="IO"/></param>
		/// <param name="chatBroadcaster">The value of <see cref="Chat"/></param>
		/// <param name="interop">The value of <see cref="Interop"/></param>
		public ByondManager(IInstanceLogger logger, IIOManager ioManager, IChatManager chatBroadcaster, IInteropManager interop)
		{
			Logger = logger;
			IO = ioManager;
			Chat = chatBroadcaster;
			Interop = interop;

			Chat.OnPopulateCommandInfo += (a, b) => { b.CommandInfo.Byond = this; };

			CleanStaging();
			updateStat = ByondStatus.Idle;
		}

		/// <summary>
		/// Cleans the <see cref="StagingDirectory"/> and <see cref="RevisionDownloadPath"/>
		/// </summary>
		void CleanStaging()
		{
			//linger not
			Task.WaitAll(new Task[] { IO.DeleteFile(RevisionDownloadPath), IO.DeleteDirectory(StagingDirectory) });
		}

		/// <summary>
		/// Cleans up the <see cref="ByondManager"/>
		/// </summary>
		public void Dispose()
		{
			CleanTask(true);
			CleanStaging();
		}

		/// <summary>
		/// Cancels and joins <see cref="updateTask"/> if it's running
		/// </summary>
		/// <param name="wait">If <see langword="true"/>, <see cref="Task.Wait()"/> and <see cref="Task.Dispose()"/> is called on <see cref="updateTask"/> if it's set</param>
		void CleanTask(bool wait)
		{
			Task toWait = null;
			CancellationTokenSource toCancel;
			lock (this)
			{
				toCancel = updateCancellationTokenSource;
				toWait = updateTask;
				updateTask = null;
				updateCancellationTokenSource = null;
				if (!wait)
					return;
			}
			toCancel?.Cancel();
			if (toWait != null)
			{
				toWait.Wait();
				toWait.Dispose();
			}
			toCancel?.Dispose();
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
						var results = new List<string>();
						string html = IO.GetURL(ByondLatestURL).Result;

						Regex regex = new Regex("\\\"([^\"]*)\\\"");
						MatchCollection matches = regex.Matches(html);
						foreach (Match match in matches)
							if (match.Success && match.Value.Contains("_byond.exe"))
								results.Add(match.Value.Replace("\"", "").Replace("_byond.exe", ""));

						results.Sort();
						results.Reverse();
						return results.Count > 0 ? results[0] : null;
					}
					else
					{
						var DirToUse = type == ByondVersion.Staged ? StagingDirectoryInner : ByondDirectory;
						if (IO.DirectoryExists(DirToUse).Result)
						{
							var file = IOManager.ConcatPath(DirToUse, VersionFile);
							lock (this)
								if (IO.FileExists(file).Result)
									return IO.ReadAllText(file).Result;
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
		/// Downloads and unzips a BYOND revision. Calls <see cref="ApplyStagedUpdate"/> afterwards if the <see cref="Instance"/> isn't running, otherwise, calls <see cref="IInteropManager.SendCommand(InteropCommand, IEnumerable{string})"/> with a <see cref="InteropCommand.RestartOnWorldReboot"/> parameter. Sets <see cref="lastError"/> on failure
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
				cancellationToken.ThrowIfCancellationRequested();

				CleanStaging();

				cancellationToken.ThrowIfCancellationRequested();

				Chat.SendMessage(String.Format("BYOND: Updating to version {0}.{1}...", major, minor), MessageType.DeveloperInfo);

				//DOWNLOADING

				try
				{
					IO.DownloadFile(String.Format(ByondRevisionsURL, major, minor), RevisionDownloadPath, cancellationToken).Wait();
				}
				catch
				{
					Chat.SendMessage("BYOND: Update download failed. Does the specified version exist?", MessageType.DeveloperInfo);
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

				IO.UnzipFile(RevisionDownloadPath, StagingDirectory).Wait();

				lock (this)
					IO.WriteAllText(IOManager.ConcatPath(StagingDirectoryInner, VersionFile), String.Format("{0}.{1}", major, minor)).Wait();

				//IMPORTANT: SET THE BYOND CONFIG TO NOT PROMPT FOR TRUSTED MODE REEE
				IO.CreateDirectory(ByondConfigDir).Wait();
				Task.WaitAll(new Task[] { IO.WriteAllText(ByondDDConfig, ByondNoPromptTrustedMode), IO.DeleteFile(RevisionDownloadPath) });

				lock (this)
					updateStat = ByondStatus.Staged;

				cancellationToken.ThrowIfCancellationRequested();

				if (!ApplyStagedUpdate())
				{
					Interop.SendCommand(InteropCommand.RestartOnWorldReboot);
					lastError = "Update staged. Awaiting server restart...";
					Chat.SendMessage(String.Format("BYOND: Staging complete. Awaiting server restart...", major, minor), MessageType.DeveloperInfo);
					Logger.WriteInfo(String.Format("BYOND update {0}.{1} staged", major, minor), EventID.BYONDUpdateStaged);
				}
			}
			catch (OperationCanceledException)
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
				CleanTask(false);
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
				IO.DeleteDirectory(ByondDirectory).Wait();
				IO.MoveDirectory(StagingDirectoryInner, ByondDirectory).Wait();
				IO.DeleteDirectory(StagingDirectory).Wait();
				Chat.SendMessage("BYOND: Update completed!", MessageType.DeveloperInfo);
				Logger.WriteInfo(String.Format("BYOND update {0} completed!", GetVersion(ByondVersion.Installed)), EventID.BYONDUpdateComplete);
				lock (this)
					lastError = null;
				return true;
			}
			catch (Exception e)
			{
				Chat.SendMessage("BYOND: Update failed!", MessageType.DeveloperInfo);
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
					updateCancellationTokenSource = new CancellationTokenSource();
					updateTask = Task.Factory.StartNew(() => UpdateToVersionImpl(major, minor, updateCancellationTokenSource.Token));
					return true;
				}
			return false;

		}

		/// <summary>
		/// Calls <see cref="ApplyStagedUpdate"/> if both <see cref="DDLockCount"/> and <see cref="DMLockCount"/> are zero
		/// </summary>
		/// <returns>The started <see cref="Task"/> on success, <see langword="null"/> on failure</returns>
		Task ApplyIfUnlocked()
		{
			lock (this)
				if (DDLockCount == 0 && DMLockCount == 0)
					return Task.Factory.StartNew(ApplyStagedUpdate);   //async so we can leave this lock
			return null;
		}

		/// <summary>
		/// Tries to decrement <paramref name="theLock"/>. Calls <see cref="ApplyIfUnlocked"/>
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
						ApplyIfUnlocked();
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
					error = ErrorUpdateInProgress;
					return null;
				}
				//have to use the staged one if nothing is installed
				useStagedIfPossible |= GetVersion(ByondVersion.Installed) == null;
				var pathToUse = updateStat == ByondStatus.Staged && useStagedIfPossible ? StagingDirectoryInner : ByondDirectory;
				++DMLockCount;
				error = null;
				return IO.ResolvePath(IOManager.ConcatPath(pathToUse, DMPath));
			}
		}

		/// <inheritdoc />
		public string LockDDExecutable(out string error)
		{
			lock (this)
			{
				if (updateStat == ByondStatus.Updating)
				{
					error = ErrorUpdateInProgress;
					return null;
				}
				if(GetVersion(ByondVersion.Installed) == null)
				{
					error = String.Format("Error, {0} not installed!", DreamDaemonExecutable);
					return null;
				}
				++DDLockCount;
				error = null;
				return IO.ResolvePath(DreamDaemonPath);
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
			CheckUnlock(ref DDLockCount, DreamDaemonExecutable);
		}

		/// <inheritdoc />
		public void ClearCache()
		{
			try
			{
				IO.DeleteDirectory(ByondCacheDirectory, true).Wait();
			}
			catch { }
		}
	}
}
