using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.Administration"/>
	/// </summary>
	[Flags]
	public enum AdministrationRights : ulong
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,

		/// <summary>
		/// User has complete control over creating/editing <see cref="Models.User"/>s and <see cref="Models.UserGroup"/>s (and deleting in the case of the latter).
		/// </summary>
		WriteUsers = 1,

		/// <summary>
		/// User can gracefully restart TGS.
		/// </summary>
		RestartHost = 2,

		/// <summary>
		/// User can upgrade or downgrade TGS through the API.
		/// </summary>
		ChangeVersion = 4,

		/// <summary>
		/// User can change their password.
		/// </summary>
		EditOwnPassword = 8,

		/// <summary>
		/// User can read info and rights of other users.
		/// </summary>
		ReadUsers = 16,

		/// <summary>
		/// User can list and download <see cref="Models.LogFile"/>s.
		/// </summary>
		DownloadLogs = 32,

		/// <summary>
		/// User can modify their own <see cref="Models.User.OAuthConnections"/>.
		/// </summary>
		EditOwnOAuthConnections = 64,
	}
}
