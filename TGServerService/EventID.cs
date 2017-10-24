using System;

namespace TGServerService
{
	/// <summary>
	/// Various events and their IDs in no particular order. Found in the Windows event log. These incremented by 100 and are guaranteed to never be reused in the future. Each event ID may be information, a warning, or error and will be documented accordingly. Warnings will occur due to user, data, or network errors. Errors will occur due to filesystem errors or hard faults
	/// </summary>
	public enum EventID : int
	{
		/// <summary>
		/// Info: When the bot recieves a (not necessarily valid) chat command
		/// </summary>
		ChatCommand = 100,
		/// <summary>
		/// Warning: When a <see cref="ChatProviders.IChatProvider"/> fails to <see cref="ChatProviders.IChatProvider.Connect"/>
		/// </summary>
		ChatConnectFail = 200,
		/// <summary>
		/// Error: When a <see cref="ChatProviders.IChatProvider"/> fails to construct
		/// </summary>
		ChatProviderStartFail = 300,
		/// <summary>
		/// Error: When a bad <see cref="TGServiceInterface.ChatProvider"/> is passed
		/// </summary>
		InvalidChatProvider = 400,
		/// <summary>
		/// Not in use anymore
		/// </summary>
		[Obsolete("Not in use anymore", true)]
		UpdateRequest = 500,
		/// <summary>
		/// Warning: When the BYOND updater cannot download a revision. Error: When the BYOND updater cannot unzip or apply a revision
		/// </summary>
		BYONDUpdateFail = 600,
		/// <summary>
		/// Info: When the BYOND updater successfully staged a revision but could not apply it due to the <see cref="ServerInstance"/> being active
		/// </summary>
		BYONDUpdateStaged = 700,
		/// <summary>
		/// Info: When the BYOND updater successfully applies an update
		/// </summary>
		BYONDUpdateComplete = 800,
		/// <summary>
		/// Error: Failed to move the <see cref="ServerInstance"/> with <see cref="TGServiceInterface.Components.ITGAdministration.MoveServer(string)"/>
		/// </summary>
		ServerMoveFailed = 900,
		/// <summary>
		/// Warning: Failed to delete the old directory during a <see cref="TGServiceInterface.Components.ITGAdministration.MoveServer(string)"/> operation
		/// </summary>
		ServerMovePartial = 1000,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGAdministration.MoveServer(string)"/> operation
		/// </summary>
		ServerMoveComplete = 1100,
		/// <summary>
		/// Error: An internal error occurred during a <see cref="TGServiceInterface.Components.ITGCompiler.Compile(bool)"/> operation
		/// </summary>
		DMCompileCrash = 1200,
		/// <summary>
		/// Error: An internal error occurred during a <see cref="TGServiceInterface.Components.ITGCompiler.Initialize"/> operation
		/// </summary>
		DMInitializeCrash = 1300,
		/// <summary>
		/// Warning: Compile failure of the target .dme in a <see cref="TGServiceInterface.Components.ITGCompiler.Compile(bool)"/> operation
		/// </summary>
		DMCompileError = 1400,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGCompiler.Compile(bool)"/> operation
		/// </summary>
		DMCompileSuccess = 1500,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGCompiler.Cancel"/> operation
		/// </summary>
		DMCompileCancel = 1600,
		/// <summary>
		/// Error: Failed to reattach the watchdog to a running DreamDaemon instance after a <see cref="Service"/> update
		/// </summary>
		DDReattachFail = 1700,
		/// <summary>
		/// Info: Successfully reattached the watchdog to a running DreamDaemon instance after a <see cref="Service"/> update
		/// </summary>
		DDReattachSuccess = 1800,
		/// <summary>
		/// Error: An internal error occurred while running the DreamDaemon watchdog
		/// </summary>
		DDWatchdogCrash = 1900,
		/// <summary>
		/// Info: The watchdog has exited, either for an <see cref="TGServiceInterface.Components.ITGSService.PrepareForUpdate"/> or <see cref="TGServiceInterface.Components.ITGDreamDaemon.Stop"/>, or operation
		/// </summary>
		DDWatchdogExit = 2000,
		/// <summary>
		/// Not in use anymore
		/// </summary>
		[Obsolete("Not in use anymore", true)]
		DDWatchdogRebootedServer = 2100,
		/// <summary>
		/// Warning: DreamDaemon has crashed and the watchdog is attempting to reboot it
		/// </summary>
		DDWatchdogRebootingServer = 2200,
		/// <summary>
		/// Info: The watchdog is performing a <see cref="TGServiceInterface.Components.ITGDreamDaemon.Restart"/> operation
		/// </summary>
		DDWatchdogRestart = 2300,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGDreamDaemon.Restart"/> operation
		/// </summary>
		DDWatchdogRestarted = 2400,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGDreamDaemon.Start"/> operation
		/// </summary>
		DDWatchdogStarted = 2500,
		/// <summary>
		/// Info: Successful completion of a <see cref="ChatProviders.IChatProvider.SendMessageDirect(string, string)"/> operation
		/// </summary>
		ChatSend = 2600,
		/// <summary>
		/// Info: Successful completion of a <see cref="ChatProviders.IChatProvider.SendMessage(string, MessageType)"/> operation
		/// </summary>
		ChatBroadcast = 2700,
		/// <summary>
		/// Not in use anymore
		/// </summary>
		[Obsolete("Not in use anymore", true)]
		ChatAdminBroadcast = 2800,
		/// <summary>
		/// Error: When an error occurs during a <see cref="ChatProviders.IChatProvider.Disconnect"/> operation
		/// </summary>
		ChatDisconnectFail = 2900,
		/// <summary>
		/// Not in use anymore
		/// </summary>
		[Obsolete("Not in use anymore", true)]
		TopicSent = 3000,
		/// <summary>
		/// Not in use anymore
		/// </summary>
		[Obsolete("Not in use anymore", true)]
		TopicFailed = 3100,
		/// <summary>
		/// Info: When the <see cref="ServerInstance.serviceCommsKey"/> has been generated
		/// </summary>
		CommsKeySet = 3200,
		/// <summary>
		/// Not in use anymore
		/// </summary>
		[Obsolete("Not in use anymore", true)]
		NudgeStartFail = 3300,
		/// <summary>
		/// Not in use anymore
		/// </summary>
		[Obsolete("Not in use anymore", true)]
		NudgeCrash = 3400,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGRepository.Setup(string, string)"/> operation
		/// </summary>
		RepoClone = 3500,
		/// <summary>
		/// Warning: An error occurred during a  <see cref="TGServiceInterface.Components.ITGRepository.Setup(string, string)"/> operation
		/// </summary>
		RepoCloneFail = 3600,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGRepository.Checkout(string)"/> operation
		/// </summary>
		RepoCheckout = 3700,
		/// <summary>
		/// Warning: An error occurred during a <see cref="TGServiceInterface.Components.ITGRepository.Setup(string, string)"/> operation
		/// </summary>
		RepoCheckoutFail = 3800,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGRepository.Update(bool)"/> operation with a <see langword="true"/> parameter
		/// </summary>
		RepoHardUpdate = 3900,
		/// <summary>
		/// Warning: An error occurred during a <see cref="TGServiceInterface.Components.ITGRepository.Update(bool)"/> operation with a <see langword="true"/> parameter
		/// </summary>
		RepoHardUpdateFail = 4000,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGRepository.Update(bool)"/> operation with a <see langword="false"/> parameter
		/// </summary>
		RepoMergeUpdate = 4100,
		/// <summary>
		/// Warning: An error occurred during a <see cref="TGServiceInterface.Components.ITGRepository.Update(bool)"/> operation with a <see langword="false"/> parameter
		/// </summary>
		RepoMergeUpdateFail = 4200,
		/// <summary>
		/// Info: A backup tag of the repository in it's current state was successfully created
		/// </summary>
		RepoBackupTag = 4300,
		/// <summary>
		/// Warning: A backup tag of the repository in it's current state failed to be created
		/// </summary>
		RepoBackupTagFail = 4400,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGRepository.Reset(bool)"/> operation with a <see langword="true"/> parameter
		/// </summary>
		RepoResetTracked = 4500,
		/// <summary>
		/// Warning: An error occurred during a <see cref="TGServiceInterface.Components.ITGRepository.Reset(bool)"/> operation with a <see langword="true"/> parameter
		/// </summary>
		RepoResetTrackedFail = 4600,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGRepository.Reset(bool)"/> operation with a <see langword="false"/> parameter
		/// </summary>
		RepoReset = 4700,
		/// <summary>
		/// Warning: An error occurred during a <see cref="TGServiceInterface.Components.ITGRepository.Reset(bool)"/> operation with a <see langword="false"/> parameter
		/// </summary>
		RepoResetFail = 4800,
		/// <summary>
		/// Error: Failed to update or delete the testmerged PR list
		/// </summary>
		RepoPRListError = 4900,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGRepository.MergePullRequest(int)"/> operation
		/// </summary>
		RepoPRMerge = 5000,
		/// <summary>
		/// Warning: An error occurred during a <see cref="TGServiceInterface.Components.ITGRepository.MergePullRequest(int)"/> operation
		/// </summary>
		RepoPRMergeFail = 5100,
		/// <summary>
		/// Info: Successfully committed the paths specified in the synchronize_directories field of the <see cref="ServerInstance"/>'s TGS3.json
		/// </summary>
		RepoCommit = 5200,
		/// <summary>
		/// Warning: An error occurred while committing the paths specified in the synchronize_directories field of the <see cref="ServerInstance"/>'s TGS3.json
		/// </summary>
		RepoCommitFail = 5300,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGRepository.SynchronizePush"/> operation
		/// </summary>
		RepoPush = 5400,
		/// <summary>
		/// Warning: An error occurred during a <see cref="TGServiceInterface.Components.ITGRepository.SynchronizePush"/> operation
		/// </summary>
		RepoPushFail = 5500,
		/// <summary>
		/// Info: Successful completion of a <see cref="TGServiceInterface.Components.ITGRepository.GenerateChangelog(out string)"/> operation
		/// </summary>
		RepoChangelog = 5600,
		/// <summary>
		/// Warning: An error occurred during a <see cref="TGServiceInterface.Components.ITGRepository.GenerateChangelog(out string)"/> operation
		/// </summary>
		RepoChangelogFail = 5700,
		/// <summary>
		/// Info: When the <see cref="TGServiceInterface"/> dll is updated for the <see cref="ServerInstance"/>
		/// </summary>
		InterfaceDLLUpdated = 5800,
		/// <summary>
		/// Error: An error occurred while updating the <see cref="TGServiceInterface"/> dll for the <see cref="ServerInstance"/>
		/// </summary>
		InterfaceDLLUpdateFail = 5900,
		/// <summary>
		/// Error: An error occurred while starting the <see cref="ServerInstance"/>
		/// </summary>
		InstanceInitializationFailure = 6000,
		/// <summary>
		/// Error: When an exception occurs while the <see cref="Service"/> is stopping
		/// </summary>
		ServiceShutdownFail = 6100,
		/// <summary>
		/// Info: When the <see cref="ServerInstance"/> reboots in BYOND
		/// </summary>
		WorldReboot = 6200,
		/// <summary>
		/// Info: When the output of <see cref="TGServiceInterface.Components.ITGCompiler.Compile(bool)"/> is applied to the live <see cref="ServerInstance"/>
		/// </summary>
		ServerUpdateApplied = 6300,
		/// <summary>
		/// Warning: When an exception occurs during a <see cref="ServerInstance.SendMessage(string, MessageType)"/> operation
		/// </summary>
		ChatBroadcastFail = 6400,
		/// <summary>
		/// Not in use anymore
		/// </summary>
		[Obsolete("Not in use anymore", true)]
		IRCLogModes = 6500,
		/// <summary>
		/// Warning: When the Repository submodule handler has to reclone a submodule entirely. This is a long operation and is due to an upstream bug
		/// </summary>
		SubmoduleReclone = 6600,
		/// <summary>
		/// This event is of type <see cref="System.Diagnostics.EventLogEntryType.SuccessAudit"/> or <see cref="System.Diagnostics.EventLogEntryType.FailureAudit"/>. It occurs when a user different from the previous one tries and either succeeds or fails to access a <see cref="ServerInstance"/>. DreamDaemon itself successfully accessing <see cref="TGServiceInterface.Components.ITGInterop"/> will not trigger this
		/// </summary>
		Authentication = 6700,
		/// <summary>
		/// Info: When a Preaction event successfully completes
		/// </summary>
		PreactionEvent = 6800,
		/// <summary>
		/// Warning: When a Preaction event fails
		/// </summary>
		PreactionFail = 6900,
		/// <summary>
		/// Warning: When a <see cref="TGServiceInterface.Components.ITGInterop"/> command from DreamDaemon fails
		/// </summary>
		InteropCallException = 7000,
		/// <summary>
		/// Warning: When the running DreamDaemon code does not have the correct API to talk to the <see cref="ServerInstance"/>
		/// </summary>
		APIVersionMismatch = 7100,
		/// <summary>
		/// Warning: If a .dll in the repository's TGS3.json could not be found. Error: If a symlink could not be established to a static .dll or directory
		/// </summary>
		RepoConfigurationFail = 7200,
		/// <summary>
		/// Info: Successfully read of a static path. Warning: An error occurred during a read of a static path
		/// </summary>
		StaticRead = 7300,
		/// <summary>
		/// Info: Successfully write of a static path. Warning: An error occurred during a write of a static path
		/// </summary>
		StaticWrite = 7400,
		/// <summary>
		/// Info: Successfully delete of a static path. Warning: An error occurred during a delete of a static path
		/// </summary>
		StaticDelete = 7500,
	}
}
