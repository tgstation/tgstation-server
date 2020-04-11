using System.ComponentModel;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Types of <see cref="ErrorMessage"/>s that the API may return.
	/// </summary>
	/// <remarks>Entries marked with the <see cref="System.ObsoleteAttribute"/> are no longer in use but kept for reference.</remarks>
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
		[Description("API Mismatch but no current API version provided!")]
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
		[Description("Cannot change a user's system_identifier!")]
		UserSidChange,

		/// <summary>
		/// Attempted to create a <see cref="User"/> with a <see cref="Internal.User.Name"/> and <see cref="Internal.User.SystemIdentifier"/>.
		/// </summary>
		[Description("A user cannot have both a name and system_identifier!")]
		UserMismatchNameSid,

		/// <summary>
		/// Attempted to create a <see cref="User"/> with a <see cref="UserUpdate.Password"/> and <see cref="Internal.User.SystemIdentifier"/>.
		/// </summary>
		[Description("A user cannot have both a password and system_identifier!")]
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
		/// Attempted to create a <see cref="User"/> with a whitespace <see cref="Internal.User.Name"/>.
		/// </summary>
		[Description("User names cannot be whitespace!")]
		UserWhitespaceName,

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
		[Description("Either both access_user and access_token must be set or neither!")]
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
		[Description("Cannot checkout_sha and set reference at the same time!")]
		RepoMismatchShaAndReference,

		/// <summary>
		/// Attempted to <see cref="Repository.CheckoutSha"/> and <see cref="Repository.UpdateFromOrigin"/> at the same time.
		/// </summary>
		[Description("Cannot checkout_sha and update_from_origin at the same time!")]
		RepoMismatchShaAndUpdate,

		/// <summary>
		/// Attempted to change the origin of an existing repository.
		/// </summary>
		[Description("Cannot change the origin of an existing repository, delete and recreate it instead!")]
		RepoCantChangeOrigin,

		/// <summary>
		/// <see cref="Repository.NewTestMerges"/> contained duplicate <see cref="TestMergeParameters.Number"/>s.
		/// </summary>
		[Description("The same pull request was present more than once in the test merge requests!")]
		RepoDuplicateTestMerge,

		/// <summary>
		/// Attempted to set a whitespace <see cref="Internal.RepositorySettings.CommitterName"/>.
		/// </summary>
		[Description("committer_name cannot be whitespace!")]
		RepoWhitespaceCommitterName,

		/// <summary>
		/// Attempted to set a whitespace <see cref="Internal.RepositorySettings.CommitterEmail"/>.
		/// </summary>
		[Description("committer_email cannot be whitespace!")]
		RepoWhitespaceCommitterEmail,

		/// <summary>
		/// Attempted to set <see cref="Internal.DreamDaemonLaunchParameters.PrimaryPort"/> and <see cref="Internal.DreamDaemonLaunchParameters.SecondaryPort"/> to the same value.
		/// </summary>
		[Description("Primary and secondary ports cannot be the same!")]
		DreamDaemonDuplicatePorts,

		/// <summary>
		/// <see cref="DreamDaemonSecurity.Ultrasafe"/> was used where it is not supported.
		/// </summary>
		[Description("This version of TGS does not support the ultrasafe DreamDaemon configuration!")]
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
	}
}