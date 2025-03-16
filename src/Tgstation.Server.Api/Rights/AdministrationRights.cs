using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Administration rights for the server.
	/// </summary>
	[Flags]
	public enum AdministrationRights : ulong
	{
		/// <summary>
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User has complete control over creating/editing <see cref="Models.Response.UserResponse"/>s and <see cref="Models.Response.UserGroupResponse"/>s (and deleting in the case of the latter).
		/// </summary>
		WriteUsers = 1 << 0,

		/// <summary>
		/// User can gracefully restart TGS.
		/// </summary>
		RestartHost = 1 << 1,

		/// <summary>
		/// User can read <see cref="Models.Response.AdministrationResponse"/> and upgrade/downgrade TGS using GitHub through the API.
		/// </summary>
		ChangeVersion = 1 << 2,

		/// <summary>
		/// User can change their password.
		/// </summary>
		EditOwnPassword = 1 << 3,

		/// <summary>
		/// User can read info and rights of other users.
		/// </summary>
		ReadUsers = 1 << 4,

		/// <summary>
		/// User can list and download <see cref="Models.Response.LogFileResponse"/>s.
		/// </summary>
		DownloadLogs = 1 << 5,

		/// <summary>
		/// User can modify their own <see cref="Models.Internal.UserApiBase.OAuthConnections"/> and <see cref="Models.Internal.UserApiBase.OidcConnections"/>.
		/// </summary>
		EditOwnServiceConnections = 1 << 6,

		/// <summary>
		/// User can upgrade/downgrade TGS using file uploads through the API.
		/// </summary>
		UploadVersion = 1 << 7,
	}
}
