using System.ComponentModel;

using Tgstation.Server.Common;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Types of <see cref="Response.ErrorMessageResponse"/>s that the API may return.
	/// </summary>
	/// <remarks>Entries marked Obsolete are no longer in use but kept for placeholders until they can be recycled in the next major API version.</remarks>
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
		/// The <see cref="ApiHeaders"/> failed to validate.
		/// </summary>
		[Description("A header validation error occurred!")]
		BadHeaders,

		/// <summary>
		/// Attempted to request a <see cref="Response.TokenResponse"/> with an existing <see cref="Response.TokenResponse"/>.
		/// </summary>
		[Description("Cannot generate a bearer token using a bearer token!")]
		TokenWithToken,

		/// <summary>
		/// Generic database integrity failure.
		/// </summary>
		[Description("The operation could not be performed as it would violate database integrity. Please retry the request, making sure to not duplicate field names with existing entities!")]
		DatabaseIntegrityConflict,

		/// <summary>
		/// Attempted to perform a restart operation without the host watchdog component.
		/// </summary>
		[Description("Functionality is unavailable due to a missing host watchdog component! This should NOT be seen in a production environment!")]
		MissingHostWatchdog,

		/// <summary>
		/// Attempted to change to a major version less than 4.
		/// </summary>
		[Description($"Cannot downgrade to {Constants.CanonicalPackageName} major version <4!")]
		CannotChangeServerSuite,

		/// <summary>
		/// A required remote API request failed.
		/// </summary>
		[Description("A required remote API request returned an error!")]
		RemoteApiError,

		/// <summary>
		/// A server update was requested while another was in progress.
		/// </summary>
		[Description("A server update was requested while another was in progress!")]
		ServerUpdateInProgress,

		/// <summary>
		/// Attempted to change something other than the capitalization of a <see cref="UserName.Name"/> for a user.
		/// </summary>
		[Description("Can only change the capitalization of a user's name!")]
		UserNameChange,

		/// <summary>
		/// Attempted to change a <see cref="Internal.UserModelBase.SystemIdentifier"/>.
		/// </summary>
		[Description("Cannot change a user's systemIdentifier!")]
		UserSidChange,

		/// <summary>
		/// Attempted to create a user with a <see cref="UserName.Name"/> and <see cref="Internal.UserModelBase.SystemIdentifier"/>.
		/// </summary>
		[Description("A user cannot have both a name and systemIdentifier!")]
		UserMismatchNameSid,

		/// <summary>
		/// Attempted to create a user with a <see cref="Request.UserUpdateRequest.Password"/> and <see cref="Internal.UserModelBase.SystemIdentifier"/>.
		/// </summary>
		[Description("A user cannot have both a password and systemIdentifier!")]
		UserMismatchPasswordSid,

		/// <summary>
		/// The given <see cref="Request.UserUpdateRequest.Password"/> length was less than the server's configured minimum.
		/// </summary>
		[Description("The given password is less than the server's configured minimum password length!")]
		UserPasswordLength,

		/// <summary>
		/// Attempted to create a user with a ':' character in the <see cref="UserName.Name"/>.
		/// </summary>
		[Description("User names cannot contain the ':' character!")]
		UserColonInName,

		/// <summary>
		/// Attempted to create a user with a <see langword="null"/> or whitespace <see cref="UserName.Name"/>.
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
		/// Attempted to create an <see cref="Instance"/> with a whitespace <see cref="NamedEntity.Name"/> or <see cref="Instance.Path"/>.
		/// </summary>
		[Description("Instance names and paths cannot be whitespace!")]
		InstanceWhitespaceNameOrPath,

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
		/// A <see cref="IConfigurationFile"/> was updated.
		/// </summary>
		[Description("This existing file hash does not match, the file has beeen updated!")]
		ConfigurationFileUpdated,

		/// <summary>
		/// Attempted to delete an non-empty directory.
		/// </summary>
		[Description("The directory cannot be deleted because it is not empty!")]
		ConfigurationDirectoryNotEmpty,

		/// <summary>
		/// The server swarm has less than the expected amount of nodes.
		/// </summary>
		[Description("The server swarm has less than the expected amount of nodes!")]
		SwarmIntegrityCheckFailed,

		/// <summary>
		/// One of <see cref="RepositorySettings.AccessUser"/> and <see cref="RepositorySettings.AccessToken"/> is set while the other isn't.
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
		/// Attempted to <see cref="Request.RepositoryUpdateRequest.CheckoutSha"/> and set <see cref="Internal.RepositoryApiBase.Reference"/> at the same time.
		/// </summary>
		[Description("Cannot checkoutSha and set reference at the same time!")]
		RepoMismatchShaAndReference,

		/// <summary>
		/// Attempted to <see cref="Request.RepositoryUpdateRequest.CheckoutSha"/> and <see cref="Request.RepositoryUpdateRequest.UpdateFromOrigin"/> at the same time.
		/// </summary>
		[Description("Cannot checkoutSha and updateFromOrigin at the same time!")]
		RepoMismatchShaAndUpdate,

		/// <summary>
		/// Could not delete a engine version due to it being set as the active version for the instance.
		/// </summary>
		[Description("Could not delete engine version due to it being selected as the instance's active version.")]
		EngineCannotDeleteActiveVersion,

		/// <summary>
		/// <see cref="Request.RepositoryUpdateRequest.NewTestMerges"/> contained duplicate <see cref="TestMergeParameters.Number"/>s.
		/// </summary>
		[Description("The same test merge was present more than once or is already merged!")]
		RepoDuplicateTestMerge,

		/// <summary>
		/// Attempted to set a whitespace <see cref="RepositorySettings.CommitterName"/>.
		/// </summary>
		[Description("committerName cannot be whitespace!")]
		RepoWhitespaceCommitterName,

		/// <summary>
		/// Attempted to set a whitespace <see cref="RepositorySettings.CommitterEmail"/>.
		/// </summary>
		[Description("committerEmail cannot be whitespace!")]
		RepoWhitespaceCommitterEmail,

		/// <summary>
		/// A paginated request asked for too large a page.
		/// </summary>
		[Description("Requested pageSize is too large!")]
		ApiPageTooLarge,

		/// <summary>
		/// A paginated request asked for page 0.
		/// </summary>
		[Description("Cannot request page or pageSize <= 0.")]
		ApiInvalidPageOrPageSize,

		/// <summary>
		/// A requested <see cref="ChatChannel"/>'s data does not match with its <see cref="Internal.ChatBotSettings.Provider"/>.
		/// </summary>
		[Description("One or more of the channels for one or more of the provided chat bots do not have the correct channel data for their provider!")]
		ChatBotWrongChannelType,

		/// <summary>
		/// <see cref="Internal.ChatBotSettings.ConnectionString"/> was whitespace.
		/// </summary>
		[Description("A chat bot's connection string cannot be whitespace!")]
		ChatBotWhitespaceConnectionString,

		/// <summary>
		/// Chat bot <see cref="NamedEntity.Name"/> was whitespace.
		/// </summary>
		[Description("A chat bot's name cannot be whitespace!")]
		ChatBotWhitespaceName,

		/// <summary>
		/// <see cref="Internal.ChatBotSettings.Provider"/> was <see langword="null"/> during creation.
		/// </summary>
		[Description("Missing chat bot provider!")]
		ChatBotProviderMissing,

		/// <summary>
		/// Currently unused.
		/// </summary>
		[Description("IO operation could not start contended access to the instance's configuration directory!")]
		ConfigurationContendedAccess,

		/// <summary>
		/// Attempted to add a chat bot when at or above the <see cref="Instance.ChatBotLimit"/> or it was set to something lower than the existing amount of chat bots.
		/// </summary>
		[Description("Performing this operation would violate the instance's configured chatBotLimit!")]
		ChatBotMax,

		/// <summary>
		/// Attempted to configure a chat bot with more <see cref="ChatChannel"/>s than the configured <see cref="Internal.ChatBotSettings.ChannelLimit"/>.
		/// </summary>
		[Description("Set amount of chatChannels exceeds the configured channelLimit!")]
		ChatBotMaxChannels,

		/// <summary>
		/// Failed to install DirectX with BYOND.
		/// </summary>
		[Description("Unable to start DirectX installer process! Is the server running with admin privileges?")]
		ByondDirectXInstallFail,

		/// <summary>
		/// Failed to download a given engine version.
		/// </summary>
		[Description("Error downloading specified engine version!")]
		EngineDownloadFail,

		/// <summary>
		/// Failed to lock engine executables.
		/// </summary>
		[Description("Could not acquire lock on engine installation as none exist!")]
		EngineNoVersionsInstalled,

		/// <summary>
		/// The DMAPI never validated itself.
		/// </summary>
		[Description("DMAPI validation failed! See FAQ at https://github.com/tgstation/tgstation-server/discussions/1695")]
		DeploymentNeverValidated,

		/// <summary>
		/// The DMAPI sent an invalid validation request.
		/// </summary>
		[Description("The DMAPI sent an invalid validation request!")]
		DeploymentInvalidValidation,

		/// <summary>
		/// Tried to remove the last <see cref="OAuthConnection"/> for a passwordless user.
		/// </summary>
		[Description("This user is passwordless and removing their oAuthConnections would leave them with no authentication method!")]
		CannotRemoveLastAuthenticationOption,

		/// <summary>
		/// No .dme could be found for deployment.
		/// </summary>
		[Description("No .dme configured and could not automatically detect one!")]
		DeploymentNoDme,

		/// <summary>
		/// The configured .dme could not be found.
		/// </summary>
		[Description("Could not load configured .dme!")]
		DeploymentMissingDme,

		/// <summary>
		/// Compiler failed to compile.
		/// </summary>
		[Description("Compiler exited with a non-zero exit code!")]
		DeploymentExitCode,

		/// <summary>
		/// Deployment already in progress.
		/// </summary>
		[Description("There is already a deployment operation in progress!")]
		DeploymentInProgress,

		/// <summary>
		/// Missing <see cref="Internal.DreamDaemonSettings"/> in database.
		/// </summary>
		[Description("Could not retrieve DreamDaemon settings from the database!")]
		InstanceMissingDreamDaemonSettings,

		/// <summary>
		/// Missing <see cref="Internal.DreamMakerSettings"/> in database.
		/// </summary>
		[Description("Could not retrieve DreamMaker settings from the database!")]
		InstanceMissingDreamMakerSettings,

		/// <summary>
		/// Missing <see cref="RepositorySettings"/> in database.
		/// </summary>
		[Description("Could not retrieve Repository settings from the database!")]
		InstanceMissingRepositorySettings,

		/// <summary>
		/// Performing an automatic update with the <see cref="RepositorySettings.AutoUpdatesKeepTestMerges"/> flag resulted in merge conflicts.
		/// </summary>
		[Description("Performing this automatic update as a merge would result in conficts. Aborting!")]
		InstanceUpdateTestMergeConflict,

		/// <summary>
		/// <see cref="RepositorySettings.AccessUser"/> and <see cref="RepositorySettings.AccessToken"/> are required for this operation.
		/// </summary>
		[Description("Git credentials are required for this operation!")]
		RepoCredentialsRequired,

		/// <summary>
		/// The remote returned an invalid authentication request.
		/// </summary>
		[Description("The remote is requesting authentication, but is not allowing credentials to be received!")]
		RepoCannotAuthenticate,

		/// <summary>
		/// Cannot perform operation while not on a <see cref="Internal.RepositoryApiBase.Reference"/>.
		/// </summary>
		[Description("This git operation requires the repository HEAD to currently be on a tracked reference!")]
		RepoReferenceRequired,

		/// <summary>
		/// Attempted to start the watchdog when it was already running.
		/// </summary>
		[Description("The watchdog is already running!")]
		WatchdogRunning,

		/// <summary>
		/// Attempted to start the watchdog with a corrupted <see cref="Internal.CompileJob"/>.
		/// </summary>
		[Description("Cannot launch active compile job as it is missing or corrupted!")]
		WatchdogCompileJobCorrupted,

		/// <summary>
		/// Game server exited before it finished starting.
		/// </summary>
		[Description("Game server failed to start!")]
		WatchdogStartupFailed,

		/// <summary>
		/// Game server timed-out before it finished starting.
		/// </summary>
		[Description("Game server failed to start within the configured timeout!")]
		WatchdogStartupTimeout,

		/// <summary>
		/// Attempted to test merge with an unsupported remote.
		/// </summary>
		[Description("Test merging with the current remote is not supported!")]
		RepoUnsupportedTestMergeRemote,

		/// <summary>
		/// Either <see cref="Request.RepositoryUpdateRequest.CheckoutSha"/> or <see cref="Internal.RepositoryApiBase.Reference"/> was in one when it should have been the other.
		/// </summary>
		[Description("The value set for checkoutSha or reference should be in the other field!")]
		RepoSwappedShaOrReference,

		/// <summary>
		/// A merge conflict occurred during a git operation.
		/// </summary>
		[Description("A merge conflict occurred while performing the operation!")]
		RepoMergeConflict,

		/// <summary>
		/// The current <see cref="Internal.RepositoryApiBase.Reference"/> does not track a remote reference.
		/// </summary>
		[Description("The repository's current reference is unsuitable for this operation as it does not track a remote reference!")]
		RepoReferenceNotTracking,

		/// <summary>
		/// Encounted merge conflicts while test merging.
		/// </summary>
		[Description("Encountered merge conflicts while test merging one or more sources!")]
		RepoTestMergeConflict,

		/// <summary>
		/// Attempted to create an instance outside of the <see cref="Internal.ServerInformationBase.ValidInstancePaths"/>.
		/// </summary>
		[Description("The new instance's path is not under a white-listed path.")]
		InstanceNotAtWhitelistedPath,

		/// <summary>
		/// Attempted to make a game server update with both <see cref="Internal.DreamDaemonApiBase.SoftRestart"/> and <see cref="Internal.DreamDaemonApiBase.SoftShutdown"/> set.
		/// </summary>
		[Description("Cannot set both softShutdown and softReboot at once!")]
		GameServerDoubleSoft,

		/// <summary>
		/// Attempted to launch DreamDaemon on a user account that had the BYOND pager running.
		/// </summary>
		[Description("Cannot start DreamDaemon headless with the BYOND pager running!")]
		DreamDaemonPagerRunning,

		/// <summary>
		/// Could not bind to port we wanted to launch the game server on.
		/// </summary>
		[Description("Could not bind to requested game server port! Is there another service running on that port?")]
		GameServerPortInUse,

		/// <summary>
		/// Failed to post GitHub comments, or send TGS event.
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

		/// <summary>
		/// Missing GCore executable.
		/// </summary>
		[Description("Attempted to create a process dump but /usr/bin/gcore could not be located!")]
		MissingGCore,

		/// <summary>
		/// Non-zero gcore exit code.
		/// </summary>
		[Description("Could not create dump as gcore exited with a non-zero exit code!")]
		GCoreFailure,

		/// <summary>
		/// Attempted to test merge with an invalid remote repository.
		/// </summary>
		[Description("Test merging cannot be performed with this remote!")]
		RepoTestMergeInvalidRemote,

		/// <summary>
		/// Attempted to switch to a custom engine version that does not exist.
		/// </summary>
		[Description("Cannot switch to requested custom engine version as it is not currently installed.")]
		EngineNonExistentCustomVersion,

		/// <summary>
		/// Attempted to perform an operation that requires server (not the watchdog) to be running but it wasn't.
		/// </summary>
		[Description("Cannot perform this operation as the game server is not currently running!")]
		GameServerOffline,

		/// <summary>
		/// Attempted to perform an instance operation with an offline instance.
		/// </summary>
		[Description("The instance associated with the operation is currently offline!")]
		InstanceOffline,

		/// <summary>
		/// An attempt to connect a chat bot failed.
		/// </summary>
		[Description("Failed to connect chat bot!")]
		ChatCannotConnectProvider,

		/// <summary>
		/// Attempt to add DreamDaemon to the list of firewall exempt processes failed.
		/// </summary>
		[Description("Failed to allow game server through the Windows firewall!")]
		EngineFirewallFail,

		/// <summary>
		/// Attempted to create an instance but no free ports could be found.
		/// </summary>
		[Description("TGS was unable to find a free port to allocate for the operation!")]
		NoPortsAvailable,

		/// <summary>
		/// Attempted to set a port which is either in use by another part of TGS or otherwise not available for binding.
		/// </summary>
		[Description("The requested port is either already in use by TGS or could not be allocated!")]
		PortNotAvailable,

		/// <summary>
		/// Attempted to set <see cref="Internal.UserApiBase.OAuthConnections"/> for the admin user.
		/// </summary>
		[Description("The admin user cannot use OAuth connections!")]
		AdminUserCannotHaveServiceConnection,

		/// <summary>
		/// Attempted to login with a disabled OAuth provider.
		/// </summary>
		[Description("The requested OAuth provider is disabled via configuration!")]
		OAuthProviderDisabled,

		/// <summary>
		/// A job requiring a file upload did not receive it before timing out.
		/// </summary>
		[Description("The job did not receive a required upload before timing out!")]
		FileUploadExpired,

		/// <summary>
		/// Tried to update a user to have both a <see cref="Internal.UserApiBase.Group"/> and <see cref="Internal.UserApiBase.PermissionSet"/>.
		/// </summary>
		[Description("A user may not have both a permissionSet and group!")]
		UserGroupAndPermissionSet,

		/// <summary>
		/// Tried to delete a non-empty user group.
		/// </summary>
		[Description("Cannot delete the user group as it is not empty!")]
		UserGroupNotEmpty,

		/// <summary>
		/// Attempted to create a user but the configured limit has been reached.
		/// </summary>
		[Description("The user cannot be created because the configured limit has been reached!")]
		UserLimitReached,

		/// <summary>
		/// Attempted to create a user group but the configured limit has been reached.
		/// </summary>
		[Description("The user group cannot be created because the configured limit has been reached!")]
		UserGroupLimitReached,

		/// <summary>
		/// A deployment took longer than the configured timeout.
		/// </summary>
		[Description("The deployment took longer than the configured timeout!")]
		DeploymentTimeout,

		/// <summary>
		/// Sending a broadcast message failed.
		/// </summary>
		[Description("Could not send broadcast to the DMAPI. This can happen either due to there being an insufficient DMAPI version, a communication failure, or the server being offline.")]
		BroadcastFailure,

		/// <summary>
		/// Could not compile OpenDream due to a missing dotnet executable.
		/// </summary>
		[Description("OpenDream could not be compiled due to being unable to locate the dotnet executable!")]
		OpenDreamCantFindDotnet,

		/// <summary>
		/// Could not install OpenDream due to it not meeting the minimum version requirements.
		/// </summary>
		[Description("The specified OpenDream version is too old!")]
		OpenDreamTooOld,

		/// <summary>
		/// Failed dotnet diagnostics dump.
		/// </summary>
		[Description("Could not create dump as dotnet diagnostics threw an exception!")]
		DotnetDiagnosticsFailure,

		/// <summary>
		/// The configured .dme could not be found.
		/// </summary>
		[Description("Could not load configured .dme due to it being outside the deployment directory! This should be a relative path.")]
		DeploymentWrongDme,

		/// <summary>
		/// Entered wrong <see cref="RepositorySettings.AccessUser"/> for a <see cref="RepositorySettings.AccessToken"/>.
		/// </summary>
		[Description("Provided repository username doesn't match the user of the corresponding access token!")]
		RepoTokenUsernameMismatch,

		/// <summary>
		/// Attempted to make a cross swarm server request using the GraphQL API.
		/// </summary>
		[Description("GraphQL swarm remote gateways not implemented!")]
		RemoteGatewaysNotImplemented,
	}
}
