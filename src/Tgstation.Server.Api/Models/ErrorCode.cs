using System;
using System.ComponentModel;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Types of <see cref="ErrorMessage"/>s that the API may return.
	/// </summary>
	/// <remarks>Entries marked with the <see cref="ObsoleteAttribute"/> are no longer in use but kept for reference.</remarks>
	public enum ErrorCode : uint
	{
		/// <summary>
		/// An implementation error occurred while processing the request.
		/// </summary>
		[Description("An internal server error occurred!")]
		InternalServerError,

		/// <summary>
		/// Indicates an API upgrade was required by the server.
		/// </summary>
		[Description("API version mismatch!")]
		ApiMismatch,

		/// <summary>
		/// The request body was malformed.
		/// </summary>
		[Description("The request body was malformed!")]
		ModelValidationFailure,

		/// <summary>
		/// An IO error occurred while working with the configuration system.
		/// </summary>
		[Description("An IO error occurred during the operation!")]
		IOError,

		/// <summary>
		/// The <see cref="ApiHeaders"/> failed to validate!
		/// </summary>
		[Description("A header validation error occurred!")]
		BadHeaders,

		/// <summary>
		/// Attempted to request a <see cref="Token"/> with an existing <see cref="Token"/>.
		/// </summary>
		[Description("Cannot generate a bearer token using a bearer token!")]
		TokenWithToken,

		/// <summary>
		/// Generic database integrity failure.
		/// </summary>
		[Description("The operation could not be performed as it would violate database integrity. Please retry the request, making sure to not duplicate field names with existing entities.")]
		DatabaseIntegrityConflict,

		/// <summary>
		/// Attempted to perform a restart operation without the host watchdog component.
		/// </summary>
		[Description("Functionality is unavailable due to a missing host watchdog component! This should NOT be seen in a production environment!")]
		MissingHostWatchdog,

		/// <summary>
		/// Attempted to change to a suite other than TGS4.
		/// </summary>
		[Description("Cannot update to a different tgstation-server suite version.")]
		CannotChangeServerSuite,

		/// <summary>
		/// A required GitHub API request failed.
		/// </summary>
		[Description("A required GitHub request returned an API error!")]
		GitHubApiError,

		/// <summary>
		/// A server update was requested while another was in progress.
		/// </summary>
		[Description("A server update was requested while another was in progress")]
		ServerUpdateInProgress,

		/// <summary>
		/// Attempted to change something other than the capitalization of a <see cref="Internal.User.Name"/>.
		/// </summary>
		[Description("Can only change the capitalization of a user's name!")]
		UserNameChange,

		/// <summary>
		/// Attempted to change a <see cref="Internal.User.SystemIdentifier"/>.
		/// </summary>
		[Description("Cannot change a user's systemIdentifier!")]
		UserSidChange,

		/// <summary>
		/// Attempted to create a <see cref="User"/> with a <see cref="Internal.User.Name"/> and <see cref="Internal.User.SystemIdentifier"/>.
		/// </summary>
		[Description("A user cannot have both a name and systemIdentifier!")]
		UserMismatchNameSid,

		/// <summary>
		/// Attempted to create a <see cref="User"/> with a <see cref="UserUpdate.Password"/> and <see cref="Internal.User.SystemIdentifier"/>.
		/// </summary>
		[Description("A user cannot have both a password and systemIdentifier!")]
		UserMismatchPasswordSid,

		/// <summary>
		/// The given <see cref="UserUpdate.Password"/> length was less than the server's configured minimum.
		/// </summary>
		[Description("The given password is less than the server's configured minimum password length!")]
		UserPasswordLength,

		/// <summary>
		/// Attempted to create a <see cref="User"/> with a ':' in the <see cref="Internal.User.Name"/>.
		/// </summary>
		[Description("User names cannot contain the ':' character!")]
		UserColonInName,

		/// <summary>
		/// Attempted to create a <see cref="User"/> with a <see langword="null"/> or whitespace <see cref="Internal.User.Name"/>.
		/// </summary>
		[Description("User's name is missing or invalid whitespace!")]
		UserMissingName,

		/// <summary>
		/// Attempted to change a <see cref="Instance.Path"/> while it was <see cref="Instance.Online"/>.
		/// </summary>
		[Description("Cannot relocate an instance while it is online!")]
		InstanceRelocateOnline,

		/// <summary>
		/// Attempted to change a <see cref="Instance.Path"/> to a non-empty location.
		/// </summary>
		[Description("The instance cannot be placed at the given path because it is not empty!")]
		InstanceAtExistingPath,

		/// <summary>
		/// Attempted to detach an <see cref="Instance"/> while it was <see cref="Instance.Online"/>.
		/// </summary>
		[Description("Cannot detach an instance while it is online!")]
		InstanceDetachOnline,

		/// <summary>
		/// Attempted to change a <see cref="Instance.Path"/> to a location that conflicts with the server install or another instance.
		/// </summary>
		[Description("The instance cannot be placed at the given path because it is a child of an existing instance or the server installation directory!")]
		InstanceAtConflictingPath,

		/// <summary>
		/// Attempted to create an <see cref="Instance"/> but the configured instance limit has been reached.
		/// </summary>
		[Description("The instance cannot be created because the configured limit has been reached!")]
		InstanceLimitReached,

		/// <summary>
		/// Attempted to create an <see cref="Instance"/> with a whitespace <see cref="Instance.Name"/>.
		/// </summary>
		[Description("Instance names cannot be whitespace!")]
		InstanceWhitespaceName,

		/// <summary>
		/// The <see cref="ApiHeaders.InstanceIdHeader"/> header was required but not set.
		/// </summary>
		[Description("The request path requires the 'Instance' header to be set!")]
		InstanceHeaderRequired,

		/// <summary>
		/// The operation requires the unimplemented POSIX system identities feature.
		/// </summary>
		[Description("This operation requires POSIX system identities to be implemented. See https://github.com/tgstation/tgstation-server/issues/709 for details.")]
		RequiresPosixSystemIdentity,

		/// <summary>
		/// A <see cref="ConfigurationFile"/> was attem updated
		/// </summary>
		[Description("This existing file hash does not match, the file has beeen updated!")]
		ConfigurationFileUpdated,

		/// <summary>
		/// Attempted to delete an non-empty directory.
		/// </summary>
		[Description("The directory cannot be deleted because it is not empty!")]
		ConfigurationDirectoryNotEmpty,

		/// <summary>
		/// Tried to clone a repository with a missing <see cref="Repository.Origin"/> property.
		/// </summary>
		[Description("Cannot clone repository with missing origin field!")]
		RepoMissingOrigin,

		/// <summary>
		/// One of <see cref="Internal.RepositorySettings.AccessUser"/> and <see cref="Internal.RepositorySettings.AccessToken"/> is set while the other isn't.
		/// </summary>
		[Description("Either both accessUser and accessToken must be set or neither!")]
		RepoMismatchUserAndAccessToken,

		/// <summary>
		/// The repository is busy being cloned.
		/// </summary>
		[Description("The repository is busy being cloned!")]
		RepoCloning,

		/// <summary>
		/// The repository is busy with another operation.
		/// </summary>
		[Description("The repository is busy with another operation!")]
		RepoBusy,

		/// <summary>
		/// Attempted to clone the repository when it already exists.
		/// </summary>
		[Description("The repository already exists!")]
		RepoExists,

		/// <summary>
		/// Attempted to load a non-existant repo.
		/// </summary>
		[Description("The repository does not exist and must be cloned!")]
		RepoMissing,

		/// <summary>
		/// Attempted to <see cref="Repository.CheckoutSha"/> and set <see cref="Repository.Reference"/> at the same time.
		/// </summary>
		[Description("Cannot checkoutSha and set reference at the same time!")]
		RepoMismatchShaAndReference,

		/// <summary>
		/// Attempted to <see cref="Repository.CheckoutSha"/> and <see cref="Repository.UpdateFromOrigin"/> at the same time.
		/// </summary>
		[Description("Cannot checkoutSha and updateFromOrigin at the same time!")]
		RepoMismatchShaAndUpdate,

		/// <summary>
		/// Attempted to change the origin of an existing repository.
		/// </summary>
		[Description("Cannot change the origin of an existing repository, delete and recreate it instead!")]
		RepoCantChangeOrigin,

		/// <summary>
		/// <see cref="Repository.NewTestMerges"/> contained duplicate <see cref="TestMergeParameters.Number"/>s.
		/// </summary>
		[Description("The same pull request was present more than once in the test merge requests or is already merged!")]
		RepoDuplicateTestMerge,

		/// <summary>
		/// Attempted to set a whitespace <see cref="Internal.RepositorySettings.CommitterName"/>.
		/// </summary>
		[Description("committerName cannot be whitespace!")]
		RepoWhitespaceCommitterName,

		/// <summary>
		/// Attempted to set a whitespace <see cref="Internal.RepositorySettings.CommitterEmail"/>.
		/// </summary>
		[Description("committerEmail cannot be whitespace!")]
		RepoWhitespaceCommitterEmail,

		/// <summary>
		/// Attempted to set <see cref="Internal.DreamDaemonLaunchParameters.PrimaryPort"/> and <see cref="Internal.DreamDaemonLaunchParameters.SecondaryPort"/> to the same value.
		/// </summary>
		[Description("Primary and secondary ports cannot be the same!")]
		DreamDaemonDuplicatePorts,

		/// <summary>
		/// <see cref="DreamDaemonSecurity.Ultrasafe"/> was used where it is not supported.
		/// </summary>
		[Description("Deprecated error code.")]
		[Obsolete("With DMAPI-5.0.0, ultrasafe security is now supported.", true)]
		InvalidSecurityLevel,

		/// <summary>
		/// A requested <see cref="ChatChannel"/>'s data does not match with its <see cref="Internal.ChatBot.Provider"/>.
		/// </summary>
		[Description("One or more of the channels for one or more of the provided chat bots do not have the correct channel data for their provider!")]
		ChatBotWrongChannelType,

		/// <summary>
		/// <see cref="Internal.ChatBot.ConnectionString"/> was whitespace.
		/// </summary>
		[Description("A chat bot's connection string cannot be whitespace!")]
		ChatBotWhitespaceConnectionString,

		/// <summary>
		/// <see cref="Internal.ChatBot.Name"/> was whitespace.
		/// </summary>
		[Description("A chat bot's name cannot be whitespace!")]
		ChatBotWhitespaceName,

		/// <summary>
		/// <see cref="Internal.ChatBot.Provider"/> was <see langword="null"/> during creation.
		/// </summary>
		[Description("Missing chat bot provider!")]
		ChatBotProviderMissing,

		/// <summary>
		/// Attempted to update a <see cref="User"/> or <see cref="InstanceUser"/> without its ID.
		/// </summary>
		[Description("Missing user ID!")]
		UserMissingId,

		/// <summary>
		/// Attempted to add a <see cref="ChatBot"/> when at or above the <see cref="Instance.ChatBotLimit"/> or it was set to something lower than the existing amount of <see cref="ChatBot"/>.
		/// </summary>
		[Description("Performing this operation would violate the instance's configured chatBotLimit!")]
		ChatBotMax,

		/// <summary>
		/// Attempted to configure a <see cref="ChatBot"/> with more <see cref="ChatChannel"/>s than the configured limit
		/// </summary>
		[Description("Set amount of chatChannels exceeds the configured channelLimit!")]
		ChatBotMaxChannels,

		/// <summary>
		/// Failed to install DirectX with BYOND.
		/// </summary>
		[Description("Unable to start DirectX installer process! Is the server running with admin privileges?")]
		ByondDirectXInstallFail,

		/// <summary>
		/// Failed to download a given BYOND version.
		/// </summary>
		[Description("Error downloading specified BYOND version!")]
		ByondDownloadFail,

		/// <summary>
		/// Failed to lock BYOND executables.
		/// </summary>
		[Description("Could not acquire lock on BYOND installation as none exist!")]
		ByondNoVersionsInstalled,

		/// <summary>
		/// The DMAPI never validated itself
		/// </summary>
		[Description("DreamDaemon exited without validating the DMAPI!")]
		DreamMakerNeverValidated,

		/// <summary>
		/// The DMAPI sent an invalid validation request.
		/// </summary>
		[Description("The DMAPI sent an invalid validation request!")]
		DreamMakerInvalidValidation,

		/// <summary>
		/// DMAPI validation timeout.
		/// </summary>
		[Description("The DreamDaemon startup timeout was hit before the DMAPI validated!")]
		DreamMakerValidationTimeout,

		/// <summary>
		/// No .dme could be found for deployment.
		/// </summary>
		[Description("No .dme configured and could not automatically detect one!")]
		DreamMakerNoDme,

		/// <summary>
		/// The configured .dme could not be found.
		/// </summary>
		[Description("Could not load configured .dme!")]
		DreamMakerMissingDme,

		/// <summary>
		/// DreamMaker failed to compile.
		/// </summary>
		[Description("DreamMaker exited with a non-zero exit code!")]
		DreamMakerExitCode,

		/// <summary>
		/// Deployment already in progress
		/// </summary>
		[Description("There is already a deployment operation in progress!")]
		DreamMakerCompileJobInProgress,

		/// <summary>
		/// Missing <see cref="DreamDaemon"/> settings in database.
		/// </summary>
		[Description("Could not retrieve DreamDaemon settings from the database!")]
		InstanceMissingDreamDaemonSettings,

		/// <summary>
		/// Missing <see cref="DreamMaker"/> settings in database.
		/// </summary>
		[Description("Could not retrieve DreamMaker settings from the database!")]
		InstanceMissingDreamMakerSettings,

		/// <summary>
		/// Missing <see cref="Repository"/> settings in database.
		/// </summary>
		[Description("Could not retrieve Repository settings from the database!")]
		InstanceMissingRepositorySettings,

		/// <summary>
		/// Performing an automatic update with the <see cref="Internal.RepositorySettings.AutoUpdatesKeepTestMerges"/> flag resulted in merge conflicts.
		/// </summary>
		[Description("Performing this automatic update as a merge would result in conficts. Aborting!")]
		InstanceUpdateTestMergeConflict,

		/// <summary>
		/// <see cref="Internal.RepositorySettings.AccessUser"/> and <see cref="Internal.RepositorySettings.AccessToken"/> are required for this operation.
		/// </summary>
		[Description("Git credentials are required for this operation!")]
		RepoCredentialsRequired,

		/// <summary>
		/// The remote returned an invalid authentication request.
		/// </summary>
		[Description("The remote is requesting authentication, but is not allowing credentials to be received!")]
		RepoCannotAuthenticate,

		/// <summary>
		/// Cannot perform operation while not on a <see cref="Repository.Reference"/>.
		/// </summary>
		[Description("This git operation requires the repository HEAD to currently be on a tracked reference!")]
		RepoReferenceRequired,

		/// <summary>
		/// Attempted to start the watchdog when it was already running.
		/// </summary>
		[Description("The watchdog is already running!")]
		WatchdogRunning,

		/// <summary>
		/// Attempted to start the watchdog with a corrupted <see cref="CompileJob"/>.
		/// </summary>
		[Description("Cannot launch active compile job as it is missing or corrupted!")]
		WatchdogCompileJobCorrupted,

		/// <summary>
		/// DreamDaemon exited before it finished starting.
		/// </summary>
		[Description("DreamDaemon failed to start!")]
		WatchdogStartupFailed,

		/// <summary>
		/// DreamDaemon timed-out before it finished starting.
		/// </summary>
		[Description("DreamDaemon failed to start within the configured timeout!")]
		WatchdogStartupTimeout,

		/// <summary>
		/// Attempted to test merge with an unsupported remote.
		/// </summary>
		[Description("Test merging with the current remote is not supported!")]
		RepoUnsupportedTestMergeRemote,

		/// <summary>
		/// Either <see cref="Repository.CheckoutSha"/> or <see cref="Repository.Reference"/> was in one when it should have been the other.
		/// </summary>
		[Description("The value set for checkoutSha or reference should be in the other field!")]
		RepoSwappedShaOrReference,

		/// <summary>
		/// A merge conflict occurred during a git operation.
		/// </summary>
		[Description("A merge conflict occurred while performing the operation!")]
		RepoMergeConflict,

		/// <summary>
		/// The current <see cref="Repository.Reference"/> does not track a remote reference.
		/// </summary>
		[Description("The repository's current reference is unsuitable for this operation as it does not track a remote reference!")]
		RepoReferenceNotTracking,

		/// <summary>
		/// Encounted merge conflicts while test merging.
		/// </summary>
		[Description("Encountered merge conflicts while test merging one or more pull requests!")]
		RepoTestMergeConflict,

		/// <summary>
		/// Attempted to create an instance outside of the <see cref="Internal.ServerInformation.ValidInstancePaths"/>.
		/// </summary>
		[Description("The new instance's path is not under a white-listed path.")]
		InstanceNotAtWhitelistedPath,

		/// <summary>
		/// Attempted to make a DreamDaemon update with both <see cref="DreamDaemon.SoftRestart"/> and <see cref="DreamDaemon.SoftShutdown"/> set.
		/// </summary>
		[Description("Cannot set both softShutdown and softReboot at once!")]
		DreamDaemonDoubleSoft,

		/// <summary>
		/// Attempted to launch DreamDaemon on a user account that had the BYOND pager running.
		/// </summary>
		[Description("Cannot start DreamDaemon headless with the BYOND pager running!")]
		DeploymentPagerRunning,

		/// <summary>
		/// Could not bind to port we wanted to launch DreamDaemon on.
		/// </summary>
		[Description("Could not bind to requested DreamDaemon port! Is there another service running on that port?")]
		DreamDaemonPortInUse,

		/// <summary>
		/// Failed to post GitHub comments, send chat message, or send TGS event.
		/// </summary>
		[Description("The deployment succeeded but one or more notification events failed!")]
		PostDeployFailure,

		/// <summary>
		/// Attempted to restart a stopped watchdog.
		/// </summary>
		[Description("Cannot restart the watchdog as it is not running!")]
		WatchdogNotRunning,

		/// <summary>
		/// Attempted to access a resourse that is not (currently) present.
		/// </summary>
		[Description("The requested resource is not currently present, but may have been in the past.")]
		ResourceNotPresent,

		/// <summary>
		/// Attempted to access a resource that was never present.
		/// </summary>
		[Description("The requested resource is not present and never has been.")]
		ResourceNeverPresent,

		/// <summary>
		/// A required GitHub API call failed due to rate limiting.
		/// </summary>
		[Description("A required GitHub API call failed due to rate limiting.")]
		GitHubApiRateLimit,

		/// <summary>
		/// Attempted to cancel a stopped job.
		/// </summary>
		[Description("Cannot cancel the job as it is no longer running.")]
		JobStopped,
	}
}