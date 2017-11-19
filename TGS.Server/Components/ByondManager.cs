using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
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
		readonly IIOManager IO;
		/// <summary>
		/// The <see cref="IChatManager"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly IChatManager Chat;
		/// <summary>
		/// The <see cref="ITGDreamDaemon"/> for the <see cref="ByondManager"/>
		/// </summary>
		readonly ITGDreamDaemon DreamDaemon;

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
		/// <param name="dreamDaemon">The value of <see cref="DreamDaemon"/></param>
		public ByondManager(IInstanceLogger logger, IIOManager ioManager, IChatManager chatBroadcaster, ITGDreamDaemon dreamDaemon)
		{
			Logger = logger;
			IO = ioManager;
			Chat = chatBroadcaster;
			DreamDaemon = dreamDaemon;

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
			if (updateCancellationTokenSource != null)
			{
				updateCancellationTokenSource.Dispose();
				updateCancellationTokenSource = null;
			}
			if (updateTask != null)
			{
				updateTask.Wait();
				updateTask.Dispose();
				updateTask = null;
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
						if (IO.DirectoryExists(DirToUse))
						{
							var file = Path.Combine(DirToUse, VersionFile);
							lock (this)
								if (IO.FileExists(file))
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

				Chat.SendMessage(String.Format("BYOND: Updating to version {0}.{1}...", major, minor), MessageType.DeveloperInfo);

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

				ZipFile.ExtractToDirectory(IO.ResolvePath(RevisionDownloadPath), IO.ResolvePath(StagingDirectory));

				lock (this)
					IO.WriteAllText(Path.Combine(StagingDirectoryInner, VersionFile), String.Format("{0}.{1}", major, minor)).Wait();

				//IMPORTANT: SET THE BYOND CONFIG TO NOT PROMPT FOR TRUSTED MODE REEE
				IO.CreateDirectory(ByondConfigDir);
				Task.WaitAll(new Task[] { IO.WriteAllText(ByondDDConfig, ByondNoPromptTrustedMode), IO.DeleteFile(RevisionDownloadPath) });

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
					Chat.SendMessage(String.Format("BYOND: Staging complete. Awaiting server restart...", major, minor), MessageType.DeveloperInfo);
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
				{
					updateCancellationTokenSource.Dispose();
					updateCancellationTokenSource = null;
				}
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
				return IO.ResolvePath(Path.Combine(pathToUse, DMExecutable));
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
			CheckUnlock(ref DMLockCount, DreamDaemonExecutable);
		}
	}
}
